import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, test, vi } from 'vitest';
import { defineComponent, ref } from 'vue';
import { createI18n } from 'vue-i18n';
import { createMemoryHistory, createRouter, Router } from 'vue-router';
import EstateSearch from '../EstateSearch.vue';
import EstateSearchResultItem from '../EstateSearchResultItem.vue';
import { EstateRoutes } from '@/router/routes';
import { EstateType } from '@/models/Enums';
import { SEARCHABLE_TYPES } from '../useEstateSearch';
import sv from '@/locales/sv.json';

vi.mock('@/plugins/appInsights', () => ({
	appInsights: null,
}));

const searchState = {
	fetchSearchResults: vi.fn(),
	isBusyLoading: ref(false),
	searchResults: ref<{ id: number }[] | null>(null),
	otherTypeResults: ref<
		{ type: EstateType; entries: { id: number; type: EstateType }[] }[]
	>([]),
};

vi.mock('../useEstateSearch', async () => {
	const actual =
		await vi.importActual<typeof import('../useEstateSearch')>(
			'../useEstateSearch'
		);
	return {
		...actual,
		useEstateSearch: () => ({
			fetchSearchResults: searchState.fetchSearchResults,
			searchResults: searchState.searchResults,
			buildingPoints: ref([]),
			isBusyLoading: searchState.isBusyLoading,
			isFetchingBuildingLocations: ref(false),
			otherTypeResults: searchState.otherTypeResults,
		}),
	};
});

const Stub = defineComponent({ template: '<div />' });

const FilterStub = defineComponent({
	props: {
		modelValue: { type: Object, default: () => ({}) },
		expanded: { type: Boolean, default: false },
	},
	template: '<div />',
});

const TextFieldStub = defineComponent({
	props: { modelValue: { type: String, default: null } },
	emits: ['update:modelValue'],
	template: '<div />',
});

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
				EstateSearchFilter: FilterStub,
				FavoriteList: true,
				RateFeedback: true,
				'v-skeleton-loader': true,
				'v-expand-transition': { template: '<div><slot /></div>' },
				'v-text-field': TextFieldStub,
			},
		},
	});
};

const findFilterToggle = (
	wrapper: Awaited<ReturnType<typeof mountEstateSearch>>
) =>
	wrapper
		.findAll('button')
		.find((button) => button.text().includes('sökfilter'));

describe('EstateSearch portal start page', () => {
	beforeEach(() => {
		searchState.fetchSearchResults.mockClear();
		searchState.isBusyLoading.value = false;
		searchState.searchResults.value = null;
		searchState.otherTypeResults.value = [];
	});

	test('Renders the portal intro on the clean start page', async () => {
		const wrapper = await mountEstateSearch();

		expect(wrapper.find('.portal-intro h1').text()).toBe('Sök');
		expect(wrapper.find('.portal-intro p').text()).not.toBe('');
	});

	test('Intro stays put once a search is under way', async () => {
		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.find('.portal-intro').exists()).toBe(true);
	});

	test('Default building scope does not count as a user search', async () => {
		const wrapper = await mountEstateSearch();

		expect(wrapper.find('.portal-intro').exists()).toBe(true);
	});

	test('Enter in the search field runs the search', async () => {
		const wrapper = await mountEstateSearch('?search=skola');
		searchState.fetchSearchResults.mockClear();

		await wrapper.findComponent(TextFieldStub).trigger('keyup.enter');

		expect(searchState.fetchSearchResults).toHaveBeenCalled();
	});

	test('Building scope is preselected in the filter', async () => {
		const wrapper = await mountEstateSearch();

		expect(
			wrapper.findComponent(FilterStub).props('modelValue')
		).toMatchObject({ types: [EstateType.Building] });
	});

	test('Filter panel starts collapsed', async () => {
		const wrapper = await mountEstateSearch();

		expect(wrapper.findComponent(FilterStub).props('expanded')).toBe(false);
	});

	test('Filter toggle expands the panel and flips its label', async () => {
		const wrapper = await mountEstateSearch();

		const toggle = findFilterToggle(wrapper);
		expect(toggle?.text()).toBe(sv.component.estateSearch.showFilters);

		await toggle?.trigger('click');

		expect(wrapper.findComponent(FilterStub).props('expanded')).toBe(true);
		expect(toggle?.text()).toBe(sv.component.estateSearch.hideFilters);
	});

	test('Results are introduced by a heading', async () => {
		searchState.searchResults.value = [{ id: 1 }];

		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.find('h2').text()).toBe('Sökresultat');
	});

	test('Heading stays put when the search returned nothing', async () => {
		searchState.searchResults.value = [];

		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.find('h2').text()).toBe('Sökresultat');
	});

	test('Heading and an empty-result notice greet the clean start page', async () => {
		searchState.searchResults.value = null;

		const wrapper = await mountEstateSearch();

		expect(wrapper.find('h2').text()).toBe('Sökresultat');
		expect(wrapper.text()).toContain(sv.component.estateSearch.noSearchYet);
	});

	test('Empty-result notice gives way once a search is under way', async () => {
		searchState.searchResults.value = [{ id: 1 }];

		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.text()).not.toContain(
			sv.component.estateSearch.noSearchYet
		);
	});

	test('Favorites are listed on the clean start page', async () => {
		searchState.searchResults.value = null;

		const wrapper = await mountEstateSearch();

		expect(wrapper.find('favorite-list-stub').exists()).toBe(true);
	});

	test('Favorites step aside once a search is under way', async () => {
		searchState.searchResults.value = [{ id: 1 }];

		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.find('favorite-list-stub').exists()).toBe(false);
	});

	test('No-results message stays away until a search has actually run', async () => {
		searchState.searchResults.value = null;

		const wrapper = await mountEstateSearch('?search=s');

		expect(wrapper.text()).not.toContain(
			sv.component.estateSearch.noResults
		);
	});

	test('Heading and card skeletons show while the search is loading', async () => {
		searchState.isBusyLoading.value = true;
		searchState.searchResults.value = null;

		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.find('h2').text()).toBe('Sökresultat');
		expect(wrapper.findAll('.result-skeleton').length).toBeGreaterThan(1);
	});

	test('Results stay on screen while the next search runs, instead of flashing skeletons', async () => {
		searchState.searchResults.value = [{ id: 1 }];
		searchState.isBusyLoading.value = true;

		const wrapper = await mountEstateSearch('?search=skola');

		expect(wrapper.findAll('.result-skeleton')).toHaveLength(0);
		expect(wrapper.findAllComponents(EstateSearchResultItem)).toHaveLength(
			1
		);
	});

	test('A link with no type scope falls back to buildings instead of every type', async () => {
		const wrapper = await mountEstateSearch(
			`?filter=${encodeURIComponent('{"businessTypes":[5]}')}`
		);

		expect(
			wrapper.findComponent(FilterStub).props('modelValue')
		).toMatchObject({
			types: [EstateType.Building],
			businessTypes: [5],
		});
	});

	test('Rooms are never offered as a search type', () => {
		expect(SEARCHABLE_TYPES).not.toContain(EstateType.Room);
	});

	test('Zero hits lists the real results of the excluded type', async () => {
		searchState.searchResults.value = [];
		searchState.otherTypeResults.value = [
			{
				type: EstateType.Estate,
				entries: [
					{ id: 1, type: EstateType.Estate },
					{ id: 2, type: EstateType.Estate },
				],
			},
		];

		const wrapper = await mountEstateSearch('?search=alidhem');

		const section = wrapper.find('.other-type-results');
		expect(section.find('h3').text()).toBe('Träffar bland fastigheter');
		expect(section.findAllComponents(EstateSearchResultItem)).toHaveLength(
			2
		);
	});

	test('Zero hits scopes the message to the searched type even when the other type has nothing either', async () => {
		searchState.searchResults.value = [];
		searchState.otherTypeResults.value = [];

		const wrapper = await mountEstateSearch('?search=alidhem');

		expect(wrapper.text()).toContain(
			'Inga byggnader matchade din sökning.'
		);
	});

	test('Clearing the search holds the results until the next fetch clears them', async () => {
		searchState.searchResults.value = [{ id: 1 }];

		const wrapper = await mountEstateSearch();

		expect(wrapper.text()).not.toContain(
			sv.component.estateSearch.noSearchYet
		);
		expect(wrapper.find('favorite-list-stub').exists()).toBe(false);
	});

	test('Empty-result message stays put while the next search runs, instead of flashing', async () => {
		searchState.searchResults.value = [];
		searchState.isBusyLoading.value = true;

		const wrapper = await mountEstateSearch('?search=alidhem');

		expect(wrapper.text()).toContain(
			'Inga byggnader matchade din sökning.'
		);
	});

	test('Zero hits keeps the message generic when no type scope is set', async () => {
		searchState.searchResults.value = [];

		const wrapper = await mountEstateSearch(
			`?search=alidhem&filter=${encodeURIComponent(
				'{"types":["building","estate"]}'
			)}`
		);

		expect(wrapper.text()).toContain(sv.component.estateSearch.noResults);
	});

	test('Zero hits scopes the message to the searched type', async () => {
		searchState.searchResults.value = [];
		searchState.otherTypeResults.value = [
			{
				type: EstateType.Estate,
				entries: [{ id: 1, type: EstateType.Estate }],
			},
		];

		const wrapper = await mountEstateSearch('?search=alidhem');

		expect(wrapper.text()).toContain(
			'Inga byggnader matchade din sökning.'
		);
	});
});
