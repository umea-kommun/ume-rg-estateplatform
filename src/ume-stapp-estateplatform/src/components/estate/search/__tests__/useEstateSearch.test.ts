import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, test, vi } from 'vitest';
import { defineComponent, ref } from 'vue';
import { createMemoryHistory, createRouter, Router } from 'vue-router';
import { EstateType } from '@/models/Enums';
import { EstateRoutes } from '@/router/routes';
import { createDefaultSearchFilter, useEstateSearch } from '../useEstateSearch';

const dispatch = vi.fn();

vi.mock('@/store/store', () => ({
	default: {
		state: {},
		commit: vi.fn(),
		dispatch: (...args: unknown[]) => dispatch(...args),
	},
}));

class CanceledError extends Error {
	constructor() {
		super('canceled');
		this.name = 'CanceledError';
	}
}

const buildingEntry = {
	id: 9,
	type: EstateType.Building,
	isFavorite: false,
};

const estateEntry = {
	id: 1,
	type: EstateType.Estate,
	isFavorite: false,
};

const createTestRouter = (): Router =>
	createRouter({
		history: createMemoryHistory(),
		routes: [
			{
				path: '/',
				name: EstateRoutes.Search,
				component: { template: '<div />' },
			},
		],
	});

const mountSearch = async () => {
	const router = createTestRouter();
	await router.push('/');
	await router.isReady();

	const search = ref('');

	const Host = defineComponent({
		setup() {
			const searchFilter = ref(createDefaultSearchFilter());
			return useEstateSearch(search, searchFilter, {
				getBuildingLocations: false,
				suggestOtherTypes: true,
			});
		},
		template: '<div />',
	});

	return {
		wrapper: mount(Host, { global: { plugins: [router] } }),
		search,
	};
};

describe('useEstateSearch other-type results', () => {
	beforeEach(() => {
		dispatch.mockReset();
	});

	test('A superseded search leaves the live request cancellable', async () => {
		const controllers: AbortController[] = [];
		dispatch.mockImplementation(
			(
				_type: unknown,
				{ abortController }: { abortController: AbortController }
			) => {
				controllers.push(abortController);
				return new Promise((_resolve, reject) => {
					abortController.signal.addEventListener('abort', () =>
						reject(new CanceledError())
					);
				});
			}
		);

		const { wrapper, search } = await mountSearch();
		search.value = 'sko';

		const superseded = wrapper.vm.fetchSearchResults();
		wrapper.vm.fetchSearchResults();
		await superseded;
		await flushPromises();

		wrapper.vm.fetchSearchResults();

		expect(controllers[1].signal.aborted).toBe(true);
	});

	test('A superseded search does not clear the loading state of its replacement', async () => {
		const releases: ((value: unknown[]) => void)[] = [];
		dispatch.mockImplementation(
			(
				_type: unknown,
				{ abortController }: { abortController: AbortController }
			) =>
				new Promise((resolve, reject) => {
					releases.push(resolve);
					abortController.signal.addEventListener('abort', () =>
						reject(new CanceledError())
					);
				})
		);

		const { wrapper, search } = await mountSearch();
		search.value = 'sko';

		const superseded = wrapper.vm.fetchSearchResults();
		const latest = wrapper.vm.fetchSearchResults();
		await superseded;
		await flushPromises();

		expect(wrapper.vm.isBusyLoading).toBe(true);

		releases.at(-1)?.([buildingEntry]);
		await latest;
		await flushPromises();

		expect(wrapper.vm.isBusyLoading).toBe(false);
	});

	test('A new search drops the previous search suggestions before loading', async () => {
		dispatch.mockResolvedValueOnce([]).mockResolvedValueOnce([estateEntry]);

		const { wrapper, search } = await mountSearch();
		search.value = 'ålidhem';
		await wrapper.vm.fetchSearchResults();
		await flushPromises();

		expect(wrapper.vm.otherTypeResults).toHaveLength(1);

		dispatch.mockReturnValue(new Promise(() => undefined));
		search.value = 'stadshuset';
		wrapper.vm.fetchSearchResults();

		expect(wrapper.vm.otherTypeResults).toEqual([]);
	});
});
