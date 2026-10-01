import { flushPromises, mount } from '@vue/test-utils';
import { afterEach, describe, expect, test, vi } from 'vitest';
import { createI18n } from 'vue-i18n';
import { createStore } from 'vuex';
import BuildingDocumentPanel from '../BuildingDocumentPanel.vue';
import { IBuildingDetails, IBuildingDocument } from '@/models/Interfaces';
import sv from '@/locales/sv.json';

vi.mock('@/utils/ErrorService', () => ({
	default: { onError: vi.fn() },
}));

const building = { id: 1, name: 'Hus A' } as IBuildingDetails;
const otherBuilding = { id: 2, name: 'Hus B' } as IBuildingDetails;

const documentNamed = (id: number, name: string): IBuildingDocument => ({
	id,
	name,
	directoryId: null,
	sizeInBytes: null,
	categoryId: null,
	categoryName: null,
});

const mountPanel = (getBuildingDocuments: ReturnType<typeof vi.fn>) =>
	mount(BuildingDocumentPanel, {
		props: { building, active: false },
		global: {
			plugins: [
				createStore({ actions: { getBuildingDocuments } }),
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
			stubs: {
				'v-alert': { template: '<div><slot /></div>' },
				'v-text-field': true,
				'v-select': true,
				'v-skeleton-loader': true,
				'v-list': { template: '<div><slot /></div>' },
				'document-file': {
					props: ['document'],
					template: '<div>{{ document.name }}</div>',
				},
				'document-preview-modal': true,
			},
		},
	});

describe('BuildingDocumentPanel', () => {
	afterEach(() => {
		vi.useRealTimers();
	});

	test('does not fetch documents while the page is still loading', async () => {
		const getBuildingDocuments = vi.fn().mockResolvedValue([]);
		mountPanel(getBuildingDocuments);
		await flushPromises();

		expect(getBuildingDocuments).not.toHaveBeenCalled();
	});

	test('prefetches the documents once the page has gone quiet', () => {
		vi.useFakeTimers();
		const getBuildingDocuments = vi.fn().mockResolvedValue([]);
		mountPanel(getBuildingDocuments);

		expect(getBuildingDocuments).not.toHaveBeenCalled();

		vi.advanceTimersByTime(2000);

		expect(getBuildingDocuments).toHaveBeenCalledTimes(1);
	});

	test('shows the skeleton, never the empty message, before the first response', async () => {
		let resolveDocuments: (docs: unknown[]) => void = () => undefined;
		const getBuildingDocuments = vi.fn().mockReturnValue(
			new Promise((resolve) => {
				resolveDocuments = resolve;
			})
		);
		const wrapper = mountPanel(getBuildingDocuments);

		await wrapper.setProps({ active: true });
		await flushPromises();

		expect(wrapper.find('v-skeleton-loader-stub').exists()).toBe(true);
		expect(wrapper.text()).not.toContain(
			sv.component.buildingDocument.noDocuments
		);

		resolveDocuments([]);
		await flushPromises();

		expect(wrapper.text()).toContain(
			sv.component.buildingDocument.noDocuments
		);
	});

	test('opening the tab fetches immediately, and only once', async () => {
		const getBuildingDocuments = vi.fn().mockResolvedValue([]);
		const wrapper = mountPanel(getBuildingDocuments);

		await wrapper.setProps({ active: true });
		await flushPromises();
		await wrapper.setProps({ active: false });
		await wrapper.setProps({ active: true });
		await flushPromises();

		expect(getBuildingDocuments).toHaveBeenCalledTimes(1);
	});

	test('switching building drops the previous building documents at once', async () => {
		vi.useFakeTimers();
		const getBuildingDocuments = vi
			.fn()
			.mockResolvedValue([documentNamed(10, 'Hus A.pdf')]);
		const wrapper = mountPanel(getBuildingDocuments);

		await wrapper.setProps({ active: true });
		await flushPromises();

		expect(wrapper.text()).toContain('Hus A.pdf');

		await wrapper.setProps({ building: otherBuilding });

		expect(wrapper.text()).not.toContain('Hus A.pdf');
	});

	test('a failed fetch is retried the next time the tab is opened', async () => {
		const getBuildingDocuments = vi
			.fn()
			.mockRejectedValueOnce(new Error('boom'))
			.mockResolvedValue([documentNamed(10, 'Hus A.pdf')]);
		const wrapper = mountPanel(getBuildingDocuments);

		await wrapper.setProps({ active: true });
		await flushPromises();

		expect(getBuildingDocuments).toHaveBeenCalledTimes(1);

		await wrapper.setProps({ active: false });
		await wrapper.setProps({ active: true });
		await flushPromises();

		expect(getBuildingDocuments).toHaveBeenCalledTimes(2);
		expect(wrapper.text()).toContain('Hus A.pdf');
	});

	test('returning to a building refetches it, instead of leaving the skeleton up', async () => {
		vi.useFakeTimers();
		const getBuildingDocuments = vi
			.fn()
			.mockResolvedValue([documentNamed(10, 'Hus A.pdf')]);
		const wrapper = mountPanel(getBuildingDocuments);

		await wrapper.setProps({ active: true });
		await flushPromises();
		expect(wrapper.text()).toContain('Hus A.pdf');

		// Away and back before the new building's idle prefetch runs
		await wrapper.setProps({ building: otherBuilding });
		await wrapper.setProps({ building });
		vi.advanceTimersByTime(2000);
		await flushPromises();

		expect(wrapper.text()).toContain('Hus A.pdf');
	});

	test('a late response for the previous building does not overwrite the current one', async () => {
		vi.useFakeTimers();
		const resolvers: ((docs: IBuildingDocument[]) => void)[] = [];
		const getBuildingDocuments = vi.fn().mockImplementation(
			() =>
				new Promise<IBuildingDocument[]>((resolve) => {
					resolvers.push(resolve);
				})
		);
		const wrapper = mountPanel(getBuildingDocuments);

		await wrapper.setProps({ active: true });
		await wrapper.setProps({ building: otherBuilding });
		vi.advanceTimersByTime(2000);
		await flushPromises();

		expect(getBuildingDocuments).toHaveBeenCalledTimes(2);

		resolvers[1]([documentNamed(20, 'Hus B.pdf')]);
		await flushPromises();
		resolvers[0]([documentNamed(10, 'Hus A.pdf')]);
		await flushPromises();

		expect(wrapper.text()).toContain('Hus B.pdf');
		expect(wrapper.text()).not.toContain('Hus A.pdf');
	});
});
