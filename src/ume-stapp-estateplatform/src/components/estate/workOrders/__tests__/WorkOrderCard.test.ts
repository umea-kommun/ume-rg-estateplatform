import { mount } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import WorkOrderCard from '../WorkOrderCard.vue';
import type { WorkOrderListItem } from '@/utils/useWorkOrders';
import { workOrder } from './workOrderFixture';
import { formatFullDate, formatRecentDate } from '@/utils/formatRecentDate';
import sv from '@/locales/sv.json';

const mountCard = (order: WorkOrderListItem) =>
	mount(WorkOrderCard, {
		props: { order },
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
		},
	});

const status = (order: WorkOrderListItem) => {
	const label = mountCard(order).find('.errand-status-label');
	return { text: label.text(), classes: label.classes() };
};

describe('WorkOrderCard', () => {
	test('shows type and building', () => {
		const wrapper = mountCard(workOrder());

		expect(wrapper.find('.errand-meta').text()).toBe(
			'Felanmälan · Stadshuset'
		);
		expect(wrapper.find('.errand-icon').classes()).toContain('faultReport');
	});

	test('shows the date of the latest change, not when it was sent', () => {
		const wrapper = mountCard(
			workOrder({
				createdAt: '2026-09-16T10:00:00Z',
				submittedAt: '2026-09-16T10:01:00Z',
				lastChangedAt: '2026-09-28T12:00:00Z',
			})
		);

		const changed = new Date('2026-09-28T12:00:00Z');
		const date = wrapper.find('.errand-date');
		expect(date.text()).toBe(formatRecentDate(changed, 'sv'));
		expect(date.attributes('title')).toBe(formatFullDate(changed, 'sv'));
		expect(date.attributes('datetime')).toBe('2026-09-28T12:00:00Z');
	});

	test('prefers the popular building name', () => {
		const wrapper = mountCard(
			workOrder({
				buildingName: '983-01',
				buildingPopularName: 'Stadshuset',
			})
		);

		expect(wrapper.find('.errand-meta').text()).toBe(
			'Felanmälan · Stadshuset'
		);
	});

	test.each([
		['click', {}],
		['keydown', { key: 'Enter' }],
		['keydown', { key: ' ' }],
	] as const)('emits select on %s %o', async (event, options) => {
		const wrapper = mountCard(workOrder());

		await wrapper.trigger(event, options);

		expect(wrapper.emitted('select')).toHaveLength(1);
	});

	// Vuetify only emits a tabindex for link cards, so the key handlers are
	// dead without an explicit one.
	test('is keyboard focusable', () => {
		expect(mountCard(workOrder()).attributes('tabindex')).toBe('0');
	});

	test('omits the building when there is none', () => {
		const wrapper = mountCard(
			workOrder({ workOrderType: 'spaceRequirement', buildingName: null })
		);

		expect(wrapper.find('.errand-meta').text()).toBe(
			'Förändrade lokalbehov'
		);
	});

	test.each([
		['received', 'Mottaget'],
		['inProgress', 'Pågående'],
		['closed', 'Avslutat'],
		['onHold', 'Vilande'],
		['materialOrdered', 'Beställt material'],
		['sentToContractor', 'Skickat till entreprenör'],
	] as const)('shows status %s as %s', (displayStatus, text) => {
		expect(
			status(workOrder({ status: 'SEND_TO_CONTRACTOR', displayStatus }))
		).toEqual({ text, classes: expect.arrayContaining([displayStatus]) });
	});

	test.each(['Pending', 'Processing', 'Submitted', 'Failed'])(
		'shows %s as sent until Pythagoras answers',
		(syncStatus) => {
			expect(status(workOrder({ syncStatus })).text).toBe('Skickat');
		}
	);

	test('shows a dismissed order as closed', () => {
		expect(status(workOrder({ syncStatus: 'Dismissed' })).text).toBe(
			'Avslutat'
		);
	});
});
