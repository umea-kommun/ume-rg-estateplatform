import { DispatchType } from '@/models/Enums';
import { EstateType } from '@/models/Enums';
import {
	IBuildingGeoLocation,
	IEstateSearchResultEntry,
	IMapPoint,
	SearchFilter,
} from '@/models/Interfaces';
import store from '@/store/store';
import ErrorService from '@/utils/ErrorService';
import { sortByBoolean } from '@/utils/sortByBoolean';
import { AxiosError } from 'axios';
import { computed, Ref, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

export const SEARCHABLE_TYPES: EstateType[] = [
	EstateType.Building,
	EstateType.Estate,
];

const DEFAULT_SEARCH_TYPES: EstateType[] = [EstateType.Building];

const OTHER_TYPE_RESULT_LIMIT = 50;

export const createDefaultSearchFilter = (): SearchFilter => ({
	types: [...DEFAULT_SEARCH_TYPES],
});

/**
 * True when the filter narrows the search by anything other than the type
 * scope. The type scope always has a value and never counts on its own.
 */
export const hasActiveSearchCriteria = (filter?: SearchFilter): boolean =>
	!!filter && Object.keys(filter).some((key) => key !== 'types');

/**
 * True when the type scope leaves at least one searchable type out, which is
 * the only way the scope narrows the search.
 */
export const isTypeScopeNarrowed = (filter?: SearchFilter): boolean => {
	const types = filter?.types ?? [];
	return types.length > 0 && types.length < SEARCHABLE_TYPES.length;
};

const isDefaultSearchFilter = (filter: SearchFilter): boolean => {
	if (hasActiveSearchCriteria(filter)) {
		return false;
	}
	const types = filter.types ?? [];
	return (
		types.length === DEFAULT_SEARCH_TYPES.length &&
		DEFAULT_SEARCH_TYPES.every((type) => types.includes(type))
	);
};

export const useEstateSearch = (
	search?: Ref<string>,
	searchFilter?: Ref<SearchFilter>,
	options: {
		updateQueryParams?: boolean;
		getBuildingLocations?: boolean;
		suggestOtherTypes?: boolean;
	} = {}
) => {
	const router = useRouter();
	const route = useRoute();

	const isBusyLoading = ref(false);
	const searchResults = ref<IEstateSearchResultEntry[] | null>(null);
	const buildings = ref<IBuildingGeoLocation[]>([]);
	const isFetchingBuildingLocations = ref(false);
	const otherTypeResults = ref<
		{ type: EstateType; entries: IEstateSearchResultEntry[] }[]
	>([]);

	const buildingPoints = computed<IMapPoint[]>(() => {
		return buildings.value.map((building) => {
			return {
				id: building.id,
				type: EstateType.Building,
				lon: building.geoLocation.lon,
				lat: building.geoLocation.lat,
			};
		});
	});

	const updateQueryParams = () => {
		const queryParams: Record<string, string | number | undefined> = {
			search: search?.value || undefined,
			filter:
				searchFilter && !isDefaultSearchFilter(searchFilter.value)
					? JSON.stringify(searchFilter.value)
					: undefined,
		};
		if (route.name) {
			router.replace({ name: route.name, query: queryParams });
		}
	};

	const fetchBuildingLocations = async (
		abortController?: AbortController
	) => {
		isFetchingBuildingLocations.value = true;
		try {
			buildings.value = await store.dispatch(
				DispatchType.GetEstateSearchGeoLocations,
				{
					params: {
						query: search?.value,
						searchFilter: searchFilter
							? searchFilter.value
							: undefined,
					},
					abortController,
				}
			);
		} catch (ex) {
			if ((ex as AxiosError).name === 'CanceledError') {
				return;
			}
			throw ex;
		} finally {
			isFetchingBuildingLocations.value = false;
		}
	};

	const fetchOtherTypeResults = async (controller: AbortController) => {
		const selectedTypes = searchFilter?.value.types ?? [];
		const excludedTypes = SEARCHABLE_TYPES.filter(
			(type) => !selectedTypes.includes(type)
		);
		if (
			searchResults.value?.length ||
			!search?.value?.trim() ||
			excludedTypes.length === 0
		) {
			return;
		}

		const result: IEstateSearchResultEntry[] = await store.dispatch(
			DispatchType.GetEstateSearch,
			{
				params: {
					query: search.value,
					searchFilter: {
						...searchFilter?.value,
						types: excludedTypes,
					},
					limit: OTHER_TYPE_RESULT_LIMIT,
				},
				abortController: controller,
			}
		);

		otherTypeResults.value = excludedTypes
			.map((type) => ({
				type,
				entries: result.filter((entry) => entry.type === type),
			}))
			.filter((group) => group.entries.length);
	};

	let abortController: AbortController | null = null;
	const fetchSearchResults = async (params?: Record<string, unknown>) => {
		isBusyLoading.value = true;
		otherTypeResults.value = [];
		if (abortController) {
			abortController.abort();
		}
		const controller = new AbortController();
		abortController = controller;
		try {
			if (options.updateQueryParams !== false) {
				updateQueryParams();
			}
			if (options.getBuildingLocations !== false) {
				fetchBuildingLocations(controller);
			}

			if (
				!search?.value?.trim() &&
				!hasActiveSearchCriteria(searchFilter?.value)
			) {
				searchResults.value = null;
				return;
			}

			const result = await store.dispatch(DispatchType.GetEstateSearch, {
				params: {
					query: search?.value,
					searchFilter: searchFilter ? searchFilter.value : undefined,
					...params,
				},
				abortController: controller,
			});

			searchResults.value = sortByBoolean(
				result,
				(entry) => entry.isFavorite
			);

			if (options.suggestOtherTypes) {
				try {
					await fetchOtherTypeResults(controller);
				} catch (ex) {
					if ((ex as AxiosError).name !== 'CanceledError') {
						ErrorService.onError({
							err: ex,
							hidden: true,
						});
					}
				}
			}
		} catch (ex) {
			if ((ex as AxiosError).name === 'CanceledError') {
				return;
			}
			ErrorService.onError({
				err: ex,
			});
		} finally {
			if (abortController === controller) {
				abortController = null;
				isBusyLoading.value = false;
			}
		}
	};

	return {
		fetchSearchResults,
		searchResults,
		buildingPoints,
		isBusyLoading,
		isFetchingBuildingLocations,
		otherTypeResults,
	};
};
