import { flushPromises, shallowMount } from '@vue/test-utils';
import { expect, test, vi } from 'vitest';
import { createI18n } from 'vue-i18n';
import { createMemoryHistory, createRouter } from 'vue-router';
import { createStore } from 'vuex';
import { DispatchType } from '@/models/Enums';
import store from '@/store/store';
import sv from '@/locales/sv.json';
import BuildingSelector from './BuildingSelector.vue';

vi.mock('@/plugins/appInsights', () => ({ appInsights: null }));
vi.mock('@/store/store', () => ({ default: { dispatch: vi.fn() } }));

test('keeps all map buildings while the first search is pending, then applies an empty result', async () => {
	vi.useFakeTimers();
	let resolveSearch!: (results: []) => void;
	vi.mocked(store.dispatch).mockImplementation((type) => {
		const action = typeof type === 'string' ? type : type.type;
		if (action === DispatchType.GetEstateSearch) {
			return new Promise<[]>((resolve) => {
				resolveSearch = resolve;
			});
		}
		return Promise.resolve([]);
	});
	const router = createRouter({
		history: createMemoryHistory(),
		routes: [{ path: '/', component: { template: '<div />' } }],
	});
	await router.push('/');
	await router.isReady();
	const wrapper = shallowMount(BuildingSelector, {
		props: { selectedBuilding: null },
		slots: {
			'search-action':
				'<button @click="params.updateSearch(\'Skola\')">' +
				'<span data-testid="map-filter">{{ JSON.stringify(params.searchResultIds) }}</span></button>',
		},
		global: {
			plugins: [
				router,
				createStore({}),
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
		},
	});
	try {
		expect(wrapper.get('[data-testid="map-filter"]').text()).toBe('null');
		await wrapper.get('button').trigger('click');
		expect(wrapper.get('[data-testid="map-filter"]').text()).toBe('null');

		await vi.advanceTimersByTimeAsync(200);
		expect(wrapper.get('[data-testid="map-filter"]').text()).toBe('null');
		resolveSearch([]);
		await flushPromises();
		expect(wrapper.get('[data-testid="map-filter"]').text()).toBe('[]');
	} finally {
		wrapper.unmount();
		vi.useRealTimers();
	}
});
