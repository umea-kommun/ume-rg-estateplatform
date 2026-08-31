import { mount } from '@vue/test-utils';
import { describe, expect, test, vi } from 'vitest';
import { defineComponent, ref } from 'vue';
import { createI18n } from 'vue-i18n';
import { createMemoryHistory, createRouter, Router } from 'vue-router';
import EstateSearch from '../EstateSearch.vue';
import { EstateRoutes } from '@/router/routes';
import sv from '@/locales/sv.json';

vi.mock('@/plugins/appInsights', () => ({
	appInsights: null,
}));

vi.mock('../useEstateSearch', () => ({
	useEstateSearch: () => ({
		fetchSearchResults: vi.fn(),
		searchResults: ref(null),
		buildingPoints: ref([]),
		isBusyLoading: ref(false),
		isFetchingBuildingLocations: ref(false),
	}),
}));

const Stub = defineComponent({ template: '<div />' });

const createTestRouter = (): Router =>
	createRouter({
		history: createMemoryHistory(),
		routes: [{ path: '/', name: EstateRoutes.Search, component: Stub }],
	});

const mountEstateSearch = async (query = '') => {
	const router = createTestRouter();
	await router.push(`/${query}`);
	await router.isReady();

	return mount(EstateSearch, {
		global: {
			plugins: [
				router,
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
			stubs: {
				AppContent: { template: '<div><slot /></div>' },
				NavBreadcrumbs: true,
				BuildingMap: true,
				EstateSearchFilter: true,
				FavoriteList: true,
				RateFeedback: true,
				'v-skeleton-loader': true,
			},
		},
	});
};

describe('EstateSearch portal start page', () => {
	test('Renders the portal intro on the clean start page', async () => {
		const wrapper = await mountEstateSearch();

		expect(wrapper.find('.portal-intro h1').text()).toBe(
			'Fastighetsportalen'
		);
		expect(wrapper.find('.portal-intro p').text()).not.toBe('');
	});

	test('Intro yields to search results', async () => {
		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.find('.portal-intro').exists()).toBe(false);
	});
});
