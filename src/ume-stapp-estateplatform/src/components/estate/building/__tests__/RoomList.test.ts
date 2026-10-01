import { flushPromises, mount } from '@vue/test-utils';
import { afterEach, describe, expect, test, vi } from 'vitest';
import { createI18n } from 'vue-i18n';
import RoomList from '../RoomList.vue';
import { IBuildingRoom } from '@/models/Interfaces';
import sv from '@/locales/sv.json';

const rooms = Array.from({ length: 20 }, (_, i) => ({
	id: i + 1,
	name: `Rum ${i + 1}`,
})) as IBuildingRoom[];

const mountList = (initialRooms: IBuildingRoom[] = rooms) =>
	mount(RoomList, {
		props: { rooms: initialRooms },
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
			stubs: {
				'room-card': { template: '<div class="room-card" />' },
				'v-list': { template: '<div><slot /></div>' },
				'v-btn': {
					template: '<button v-bind="$attrs"><slot /></button>',
				},
			},
		},
	});

// jsdom reports every height as 0, which skips the animation the list runs
// between the old and the new height
const ROOM_CARD_HEIGHT = 80;
// The list settles this long after starting, when no transitionend arrives
const EXPAND_TIMEOUT_MS = 300;
const withMeasurableRooms = () => {
	const descriptor = Object.getOwnPropertyDescriptor(
		HTMLElement.prototype,
		'offsetHeight'
	);
	Object.defineProperty(HTMLElement.prototype, 'offsetHeight', {
		configurable: true,
		get(this: HTMLElement) {
			return (
				this.querySelectorAll('.room-card').length * ROOM_CARD_HEIGHT
			);
		},
	});
	return () => {
		if (descriptor) {
			Object.defineProperty(
				HTMLElement.prototype,
				'offsetHeight',
				descriptor
			);
		} else {
			delete (HTMLElement.prototype as { offsetHeight?: number })
				.offsetHeight;
		}
	};
};

const withScrollPosition = (top: number, scrollTo: () => void) => {
	const scrollY = Object.getOwnPropertyDescriptor(window, 'scrollY');
	Object.defineProperty(window, 'scrollY', {
		configurable: true,
		get: () => top,
	});
	const original = window.scrollTo;
	window.scrollTo = scrollTo as typeof window.scrollTo;

	return () => {
		if (scrollY) {
			Object.defineProperty(window, 'scrollY', scrollY);
		}
		window.scrollTo = original;
	};
};

describe('RoomList', () => {
	afterEach(() => {
		vi.useRealTimers();
	});

	test('clicking the faded band shows more rooms, not just the button', async () => {
		const wrapper = mountList();
		expect(wrapper.findAll('.room-card')).toHaveLength(5);

		await wrapper.find('.show-more-wrap').trigger('click');

		expect(wrapper.findAll('.room-card')).toHaveLength(15);
	});

	test('collapses back to the initial rooms with show fewer', async () => {
		const wrapper = mountList();

		await wrapper.find('.show-more-wrap').trigger('click');
		await flushPromises();
		expect(wrapper.find('.fewer-btn').exists()).toBe(true);

		await wrapper.find('.fewer-btn').trigger('click');

		expect(wrapper.findAll('.room-card')).toHaveLength(5);
		expect(wrapper.find('.fewer-btn').exists()).toBe(false);
	});

	test('shows every room and hides the band once nothing is left', async () => {
		const wrapper = mountList();

		await wrapper.find('.show-more-wrap').trigger('click');
		await wrapper.find('.show-more-wrap').trigger('click');

		expect(wrapper.findAll('.room-card')).toHaveLength(20);
		expect(wrapper.find('.show-more-wrap').exists()).toBe(false);
	});

	test('a room change during the expand animation does not hide the show more band', async () => {
		const restoreHeights = withMeasurableRooms();
		vi.useFakeTimers();

		try {
			const wrapper = mountList();

			await wrapper.find('.show-more-wrap').trigger('click');
			await flushPromises();
			expect(wrapper.findAll('.room-card')).toHaveLength(15);

			await wrapper.setProps({ rooms: rooms.slice(0, 12) });
			await flushPromises();
			vi.advanceTimersByTime(EXPAND_TIMEOUT_MS);
			await flushPromises();

			expect(wrapper.findAll('.room-card')).toHaveLength(5);
			expect(wrapper.find('.show-more-wrap').exists()).toBe(true);
		} finally {
			restoreHeights();
		}
	});

	test('unmounting mid-collapse stops the scrolling, so the next view is left alone', async () => {
		const restoreHeights = withMeasurableRooms();
		const scrollTo = vi.fn();
		const restoreScroll = withScrollPosition(1000, scrollTo);
		vi.useFakeTimers();

		try {
			const wrapper = mountList();

			await wrapper.find('.show-more-wrap').trigger('click');
			vi.advanceTimersByTime(EXPAND_TIMEOUT_MS);
			await flushPromises();

			await wrapper.find('.fewer-btn').trigger('click');
			await flushPromises();

			wrapper.unmount();
			scrollTo.mockClear();
			vi.advanceTimersByTime(EXPAND_TIMEOUT_MS);
			await flushPromises();

			expect(scrollTo).not.toHaveBeenCalled();
		} finally {
			restoreScroll();
			restoreHeights();
		}
	});

	test('collapsing scrolls up by the height the list loses', async () => {
		const restoreHeights = withMeasurableRooms();
		const scrollTo = vi.fn();
		const restoreScroll = withScrollPosition(1000, scrollTo);
		vi.useFakeTimers();

		try {
			const wrapper = mountList();

			await wrapper.find('.show-more-wrap').trigger('click');
			vi.advanceTimersByTime(EXPAND_TIMEOUT_MS);
			await flushPromises();
			expect(wrapper.findAll('.room-card')).toHaveLength(15);

			scrollTo.mockClear();
			await wrapper.find('.fewer-btn').trigger('click');
			await flushPromises();
			vi.advanceTimersByTime(EXPAND_TIMEOUT_MS);

			// The list drops from 15 to 5 cards, so the page owes back 10 of them
			expect(scrollTo).toHaveBeenCalledWith({
				top: 1000 - 10 * ROOM_CARD_HEIGHT,
			});
		} finally {
			restoreScroll();
			restoreHeights();
		}
	});
});
