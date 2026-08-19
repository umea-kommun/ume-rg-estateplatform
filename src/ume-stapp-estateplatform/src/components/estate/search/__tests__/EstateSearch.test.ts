import { mount } from '@vue/test-utils';
import { describe, expect, test, vi, beforeEach } from 'vitest';
import { defineComponent, ref } from 'vue';
import { createI18n } from 'vue-i18n';
import { createMemoryHistory, createRouter, Router } from 'vue-router';
import EstateSearch from '../EstateSearch.vue';
import { EstateRoutes } from '@/router/routes';
import sv from '@/locales/sv.json';

const flags: Record<string, boolean> = {};
vi.mock('@/utils/useFeatureFlags', () => ({
	useFeatureFlags: () => ({
		isEnabled: (flag: string) => flags[flag] ?? false,
	}),
}));

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
		routes: [
			{ path: '/', name: EstateRoutes.Search, component: Stub },
			{
				path: '/felanmalan',
				name: EstateRoutes.FaultReport,
				component: Stub,
			},
			{ path: '/bestallning', name: EstateRoutes.Order, component: Stub },
			{
				path: '/lokalbehov',
				name: EstateRoutes.SpaceRequirement,
				component: Stub,
			},
		],
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
				'v-skeleton-loader': true,
				// The global v-card stub drops props; expose the route
				// target so the navigation assertions can see it.
				'v-card': {
					props: ['to'],
					template:
						'<a class="v-card" :data-route="to?.name"><slot /></a>',
				},
			},
		},
	});
};

describe('EstateSearch portal start page', () => {
	beforeEach(() => {
		flags.ErrorReport = true;
	});

	test('Renders portal intro and one action card per service', async () => {
		const wrapper = await mountEstateSearch();

		expect(wrapper.find('.portal-intro h1').text()).toBe(
			'Fastighetsportalen'
		);

		const cards = wrapper.findAll('.portal-actions .action-card');
		expect(cards).toHaveLength(3);
		expect(cards.map((card) => card.attributes('data-route'))).toEqual([
			EstateRoutes.FaultReport,
			EstateRoutes.Order,
			EstateRoutes.SpaceRequirement,
		]);
	});

	test('Hides the action section when ErrorReport is disabled', async () => {
		flags.ErrorReport = false;
		const wrapper = await mountEstateSearch();

		expect(wrapper.find('.portal-actions').exists()).toBe(false);
		// The page must still carry its identity without the services.
		expect(wrapper.find('.portal-intro').exists()).toBe(true);
	});

	test('Intro and actions yield to search results', async () => {
		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.find('.portal-intro').exists()).toBe(false);
		expect(wrapper.find('.portal-actions').exists()).toBe(false);
	});
});
