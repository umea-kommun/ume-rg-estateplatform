import { mount } from '@vue/test-utils';
import { describe, expect, test, vi } from 'vitest';
import { createI18n } from 'vue-i18n';
import EstateOngoingErrands from '../EstateOngoingErrands.vue';
import { workOrder } from '@/components/estate/workOrders/__tests__/workOrderFixture';
import type { WorkOrderListItem } from '@/utils/useWorkOrders';
import sv from '@/locales/sv.json';

const state = vi.hoisted(() => ({
	orders: [] as unknown[],
	loading: false,
	loadFailed: false,
}));
vi.mock('@/utils/useWorkOrders', async () => {
	const { ref } = await import('vue');
	return {
		useWorkOrders: () => ({
			orders: ref(state.orders),
			loading: ref(state.loading),
			loadFailed: ref(state.loadFailed),
		}),
	};
});

function mountErrands(
	orders: WorkOrderListItem[],
	{ loading = false, loadFailed = false } = {}
) {
	state.orders = orders;
	state.loading = loading;
	state.loadFailed = loadFailed;
	return mount(EstateOngoingErrands, {
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
		},
	});
}

const cardCount = (wrapper: ReturnType<typeof mountErrands>) =>
	wrapper.findAllComponents({ name: 'WorkOrderCard' }).length;

const toggleClosed = (wrapper: ReturnType<typeof mountErrands>) =>
	wrapper.find('.toggle-closed').trigger('click');

describe('EstateOngoingErrands', () => {
	test('hides closed and dismissed orders and shows five until expanded', async () => {
		const wrapper = mountErrands([
			workOrder({ id: 'closed', displayStatus: 'closed' }),
			workOrder({ id: 'dismissed', syncStatus: 'Dismissed' }),
			...['a', 'b', 'c', 'd', 'e', 'f'].map((id) =>
				workOrder({ id, displayStatus: 'inProgress' })
			),
		]);

		expect(wrapper.find('h2').text()).toBe('Mina ärenden (6)');
		expect(cardCount(wrapper)).toBe(5);
		expect(wrapper.find('.show-more-wrap .toggle-expanded').text()).toBe(
			'Visa alla'
		);

		await wrapper.find('.toggle-expanded').trigger('click');
		expect(cardCount(wrapper)).toBe(6);
		expect(wrapper.find('.show-more-wrap').exists()).toBe(false);
		expect(wrapper.find('.show-fewer-wrap .toggle-expanded').text()).toBe(
			'Visa färre'
		);

		await wrapper.find('.toggle-expanded').trigger('click');
		expect(cardCount(wrapper)).toBe(5);
		expect(wrapper.find('.show-fewer-wrap').exists()).toBe(false);
	});

	test('expands when the band around the button is clicked', async () => {
		const wrapper = mountErrands(
			['a', 'b', 'c', 'd', 'e', 'f'].map((id) =>
				workOrder({ id, displayStatus: 'inProgress' })
			)
		);

		await wrapper.find('.show-more-wrap').trigger('click');

		expect(cardCount(wrapper)).toBe(6);
	});

	test('shows neither button when everything fits', () => {
		const wrapper = mountErrands([
			workOrder({ id: 'a', displayStatus: 'received' }),
		]);

		expect(wrapper.find('.toggle-expanded').exists()).toBe(false);
	});

	test('mixes closed orders in by the API order and counts them', async () => {
		const wrapper = mountErrands([
			workOrder({ id: 'closed', displayStatus: 'closed' }),
			workOrder({ id: 'open', displayStatus: 'received' }),
			workOrder({ id: 'dismissed', syncStatus: 'Dismissed' }),
		]);
		const ids = () =>
			wrapper
				.findAllComponents({ name: 'WorkOrderCard' })
				.map((card) => card.props('order').id);

		expect(ids()).toEqual(['open']);
		expect(wrapper.find('.toggle-closed').text()).toBe('Visa avslutade');

		await toggleClosed(wrapper);

		expect(ids()).toEqual(['closed', 'open', 'dismissed']);
		expect(wrapper.find('h2').text()).toBe('Mina ärenden (3)');
		expect(wrapper.find('.toggle-closed').text()).toBe('Dölj avslutade');

		await toggleClosed(wrapper);
		expect(ids()).toEqual(['open']);
	});

	test('keeps showing five when closed orders are revealed', async () => {
		const wrapper = mountErrands([
			workOrder({ id: 'a', displayStatus: 'inProgress' }),
			workOrder({ id: 'closed', displayStatus: 'closed' }),
			...['b', 'c', 'd', 'e', 'f'].map((id) =>
				workOrder({ id, displayStatus: 'inProgress' })
			),
		]);
		const ids = () =>
			wrapper
				.findAllComponents({ name: 'WorkOrderCard' })
				.map((card) => card.props('order').id);

		expect(ids()).toEqual(['a', 'b', 'c', 'd', 'e']);

		await toggleClosed(wrapper);

		expect(ids()).toEqual(['a', 'closed', 'b', 'c', 'd']);
		expect(wrapper.find('.toggle-expanded').text()).toBe('Visa alla');

		await wrapper.find('.toggle-expanded').trigger('click');
		expect(ids()).toEqual(['a', 'closed', 'b', 'c', 'd', 'e', 'f']);
	});

	test('shows skeletons and no count while loading', () => {
		const wrapper = mountErrands([], { loading: true });

		expect(wrapper.find('h2').text()).toBe('Mina ärenden');
		expect(wrapper.findAll('.errand-skeleton')).toHaveLength(2);
		expect(wrapper.text()).not.toContain('Här syns dina pågående ärenden');
	});

	test('shows no count or skeletons when loading failed', () => {
		const wrapper = mountErrands([], { loadFailed: true });

		expect(wrapper.find('h2').text()).toBe('Mina ärenden');
		expect(wrapper.find('.errand-skeleton').exists()).toBe(false);
		expect(wrapper.text()).toContain('Kunde inte hämta dina ärenden.');
	});

	test('shows the count once loaded', () => {
		const wrapper = mountErrands([
			workOrder({ id: 'a', displayStatus: 'received' }),
			workOrder({ id: 'b', displayStatus: 'inProgress' }),
		]);

		expect(wrapper.find('h2').text()).toBe('Mina ärenden (2)');
		expect(wrapper.find('.errand-skeleton').exists()).toBe(false);
	});

	test('shows the empty text when nothing is ongoing', () => {
		const wrapper = mountErrands([workOrder({ displayStatus: 'closed' })]);

		expect(wrapper.text()).toContain('Här syns dina pågående ärenden');
		expect(wrapper.find('.toggle-expanded').exists()).toBe(false);
		expect(wrapper.find('.toggle-closed').exists()).toBe(true);
	});

	test('hides the closed toggle when there are no closed orders', () => {
		const wrapper = mountErrands([
			workOrder({ displayStatus: 'received' }),
		]);

		expect(wrapper.find('.toggle-closed').exists()).toBe(false);
	});

	test('opens the modal with the selected order', async () => {
		const wrapper = mountErrands([
			workOrder({ id: 'open', description: 'Läcker kran' }),
		]);
		const modal = wrapper.findComponent({ name: 'WorkOrderModal' });
		expect(modal.props('modelValue')).toBe(false);

		await wrapper.findComponent({ name: 'WorkOrderCard' }).trigger('click');

		expect(modal.props('modelValue')).toBe(true);
		expect(modal.props('order')?.id).toBe('open');
	});
});
