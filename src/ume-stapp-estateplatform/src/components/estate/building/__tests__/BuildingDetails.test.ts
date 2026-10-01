import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, test, vi } from 'vitest';
import { defineComponent, ref } from 'vue';
import { createI18n } from 'vue-i18n';
import { createMemoryHistory, createRouter } from 'vue-router';
import { createStore } from 'vuex';
import BuildingDetails from '../BuildingDetails.vue';
import BuildingMapToggle from '../BuildingMapToggle.vue';
import { ActiveMapType } from '@/models/Enums';
import { EstateRoutes } from '@/router/routes';
import sv from '@/locales/sv.json';

vi.mock('@/plugins/appInsights', () => ({
	appInsights: null,
}));

vi.mock('@/utils/useFeatureFlags', () => ({
	useFeatureFlags: () => ({ isEnabled: () => true }),
}));

const viewport = vi.hoisted(() => ({ isMobile: { value: false } }));

vi.mock('@/components/estate/useEstateIsMobile', () => ({
	useEstateIsMobile: () => viewport.isMobile,
}));

const openRoom = vi.fn();
const displayAtOpenRoom = ref<string | null>(null);

const BlueprintStub = defineComponent({
	emits: ['room-opened'],
	setup(_, { expose }) {
		const root = ref<HTMLElement | null>(null);
		expose({
			openRoom: (roomId: number) => {
				displayAtOpenRoom.value = root.value?.style.display ?? null;
				openRoom(roomId);
			},
			openFullscreen: vi.fn(),
		});
		return { root };
	},
	template: '<div ref="root" class="blueprint-stub" />',
});

const TabsStub = defineComponent({
	props: { modelValue: { type: String, default: null } },
	emits: ['update:modelValue'],
	template: '<div class="tabs"><slot /></div>',
});

const focusRoom = vi.fn();

const RoomsStub = defineComponent({
	emits: ['room-selected', 'update:floor'],
	setup(_, { expose }) {
		expose({ focusRoom });
	},
	template:
		'<button class="room" @click="$emit(\'room-selected\', 42)">Rum</button>',
});

const building = {
	id: 1,
	name: 'Hus A',
	blueprintAvailable: true,
	estate: { id: 2, name: 'Fastighet' },
	contactPersons: {},
	workOrderTypeAccess: {},
};

const mountDetails = () => {
	const router = createRouter({
		history: createMemoryHistory(),
		routes: [
			{
				path: '/',
				name: EstateRoutes.Search,
				component: { template: '<div />' },
			},
			{
				path: '/estate/:estateId',
				name: EstateRoutes.EstateDetails,
				component: { template: '<div />' },
			},
			{
				path: '/building/:buildingId',
				name: EstateRoutes.BuildingDetails,
				component: { template: '<div />' },
			},
		],
	});

	return mount(BuildingDetails, {
		props: { buildingId: '1' },
		global: {
			plugins: [
				router,
				createStore({
					actions: {
						getBuildingById: vi.fn().mockResolvedValue(building),
					},
				}),
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
			stubs: {
				'app-content': { template: '<div><slot /></div>' },
				'nav-breadcrumbs': true,
				'favorite-button': true,
				'building-notice-board': true,
				'building-properties': true,
				'external-owner-info': true,
				'building-details-actions': true,
				'building-map': true,
				'v-skeleton-loader': true,
				'v-tooltip': {
					template: '<div><slot name="activator" /></div>',
				},
				'v-btn': {
					template: '<button v-bind="$attrs"><slot /></button>',
				},
				'building-blueprint': BlueprintStub,
				'building-rooms': RoomsStub,
				'building-contact-panel': true,
				'building-document-panel': true,
				'v-tabs': TabsStub,
				'v-tab': { template: '<button><slot /></button>' },
			},
		},
	});
};

// The room list is wrapped in the element v-show toggles
const roomsPanelDisplay = (wrapper: ReturnType<typeof mountDetails>) =>
	(wrapper.get('.room').element.parentElement as HTMLElement).style.display;

const selectMap = (
	wrapper: ReturnType<typeof mountDetails>,
	type: ActiveMapType
) =>
	wrapper
		.findAllComponents(BuildingMapToggle)
		.find((toggle) => toggle.props('variant') === 'overlay')
		?.vm.$emit('select', type);

const contactTab = (wrapper: ReturnType<typeof mountDetails>) =>
	wrapper
		.findAll('.tabs button')
		.filter(
			(tab) =>
				tab.text() === sv.component.buildingDetails.contactPersonsButton
		)[0];

// jsdom has no layout engine, so the tab strip's position is supplied by hand
const stubTabsBar = (wrapper: ReturnType<typeof mountDetails>, top: number) => {
	const bar = wrapper.get('.details-tabs-bar').element as HTMLElement;
	const scrollIntoView = vi.fn();

	bar.getBoundingClientRect = () => ({ top }) as DOMRect;
	bar.scrollIntoView = scrollIntoView;

	return scrollIntoView;
};

describe('BuildingDetails', () => {
	beforeEach(() => {
		openRoom.mockClear();
		focusRoom.mockClear();
		displayAtOpenRoom.value = null;
		viewport.isMobile.value = false;
	});

	test('opens a clicked room only once the blueprint pane is visible, so it does not open fullscreen', async () => {
		const wrapper = mountDetails();
		await flushPromises();

		// Switch the pane to the map, which hides the blueprint
		selectMap(wrapper, ActiveMapType.Map);
		await flushPromises();
		expect(wrapper.find('.blueprint-stub').attributes('style')).toContain(
			'display: none'
		);

		await wrapper.find('.room').trigger('click');
		await flushPromises();

		expect(openRoom).toHaveBeenCalledWith(42);
		expect(displayAtOpenRoom.value).not.toBe('none');
	});

	test('keeps the room list mounted while another tab is active', async () => {
		const wrapper = mountDetails();
		await flushPromises();

		wrapper
			.findComponent(TabsStub)
			.vm.$emit('update:modelValue', 'documents');
		await flushPromises();

		expect(wrapper.find('.room').exists()).toBe(true);
		expect(roomsPanelDisplay(wrapper)).toBe('none');
	});

	test('returns to the room tab when the blueprint opens a room', async () => {
		const wrapper = mountDetails();
		await flushPromises();

		wrapper
			.findComponent(TabsStub)
			.vm.$emit('update:modelValue', 'documents');
		await flushPromises();

		wrapper.findComponent(BlueprintStub).vm.$emit('room-opened', 42);
		await flushPromises();

		expect(focusRoom).toHaveBeenCalledWith(42);
		expect(roomsPanelDisplay(wrapper)).not.toBe('none');
	});

	test('keeps the contact tab usable when the building has no contacts', async () => {
		const wrapper = mountDetails();
		await flushPromises();

		expect(contactTab(wrapper).attributes('disabled')).toBeUndefined();
	});

	test('scrolls the tab strip into view when a shorter panel hides it on mobile', async () => {
		viewport.isMobile.value = true;
		const wrapper = mountDetails();
		await flushPromises();

		const scrollIntoView = stubTabsBar(wrapper, -200);

		wrapper
			.findComponent(TabsStub)
			.vm.$emit('update:modelValue', 'contactPersons');
		await flushPromises();

		expect(scrollIntoView).toHaveBeenCalledWith({ block: 'start' });
	});

	test('leaves the scroll position alone on mobile while the tab strip is still in view', async () => {
		viewport.isMobile.value = true;
		const wrapper = mountDetails();
		await flushPromises();

		const scrollIntoView = stubTabsBar(wrapper, 120);

		wrapper
			.findComponent(TabsStub)
			.vm.$emit('update:modelValue', 'contactPersons');
		await flushPromises();

		expect(scrollIntoView).not.toHaveBeenCalled();
	});
});
