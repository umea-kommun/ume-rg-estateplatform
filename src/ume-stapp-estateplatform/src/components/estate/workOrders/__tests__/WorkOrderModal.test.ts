import { mount } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import WorkOrderModal from '../WorkOrderModal.vue';
import type { WorkOrderListItem } from '@/utils/useWorkOrders';
import { workOrder } from './workOrderFixture';
import { formatDateTime } from '@/utils/formatRecentDate';
import sv from '@/locales/sv.json';

const mountModal = (order: WorkOrderListItem) =>
	mount(WorkOrderModal, {
		props: { modelValue: true, order },
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
		},
	});

describe('WorkOrderModal', () => {
	test('shows type, status, place, full description and number', () => {
		const wrapper = mountModal(
			workOrder({
				buildingId: 12,
				buildingName: '983-01',
				buildingPopularName: 'Stadshuset',
				buildingImageUrl: '/api/buildings/12/image',
				roomName: 'Rum 101',
				location: 'Indoor',
				description: 'Rad ett\nRad två',
				displayStatus: 'inProgress',
				workOrderNumber: 'UK-2026-2121',
				submittedAt: '2026-09-16T10:00:00Z',
			})
		);

		expect(wrapper.find('h2').text()).toBe('Felanmälan');
		expect(wrapper.find('.errand-status-label').text()).toBe('Pågående');
		expect(wrapper.find('.building').text()).toContain('Stadshuset');
		expect(wrapper.find('.show-building').exists()).toBe(true);
		expect(wrapper.find('.room').text()).toBe('Rum: Rum 101');
		expect(wrapper.find('.location').text()).toBe('Invändigt');
		expect(wrapper.find('.description p').text()).toBe('Rad ett\nRad två');
		expect(wrapper.find('.meta').text()).toMatch(
			/^Ärendenummer UK-2026-2121 · Skickat /
		);
		expect(wrapper.find('.image-wrap img').attributes('src')).toMatch(
			/\/buildings\/12\/image\?w=820$/
		);
	});

	test.each([
		['a later minute', '2026-09-16T10:05:00Z', true],
		['a later day', '2026-09-28T12:00:00Z', true],
		['the same minute', '2026-09-16T10:00:30Z', false],
		// Unchanged: the order dates from its creation, just before sending.
		['just before sending', '2026-09-16T09:59:00Z', false],
	])('shows the last change when it is %s: %s', (_, lastChangedAt, shown) => {
		const sent = '2026-09-16T10:00:00Z';
		const at = (iso: string) => formatDateTime(new Date(iso), 'sv');
		const wrapper = mountModal(
			workOrder({ createdAt: sent, submittedAt: sent, lastChangedAt })
		);

		expect(wrapper.find('.meta').text()).toBe(
			shown
				? `Skickat ${at(sent)} · Uppdaterad ${at(lastChangedAt)}`
				: `Skickat ${at(sent)}`
		);
	});

	test('falls back to the stored building name and skips a missing image', () => {
		const wrapper = mountModal(
			workOrder({ buildingId: 12, buildingName: '983-01' })
		);

		expect(wrapper.find('.building').text()).toContain('983-01');
		expect(wrapper.find('.image-wrap').exists()).toBe(false);
	});

	test('omits place and number when there are none', () => {
		const wrapper = mountModal(
			workOrder({
				workOrderType: 'spaceRequirement',
				buildingName: null,
			})
		);

		expect(wrapper.find('h2').text()).toBe('Förändrade lokalbehov');
		expect(wrapper.find('.where').exists()).toBe(false);
		expect(wrapper.find('.meta').text()).toMatch(/^Skickat /);
		expect(wrapper.find('.performed').exists()).toBe(false);
	});

	// Its date is covered by "Uppdaterad" in the meta line.
	test('shows the performed description without a date of its own', () => {
		const wrapper = mountModal(
			workOrder({
				performedDescription: 'Gallret är bytt.',
				performedDescriptionAt: '2026-09-02T08:57:13Z',
			})
		);

		expect(wrapper.find('.performed h3').text()).toBe('Utfört arbete');
		expect(wrapper.find('.performed p').text()).toBe('Gallret är bytt.');
		expect(wrapper.find('.performed').text()).not.toContain('2026');
	});
});
