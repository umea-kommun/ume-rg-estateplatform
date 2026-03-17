import { DispatchType } from '@/models/Enums';
import { EstateType } from '@/models/estate/Enums';
import {
	IBuildingGeoLocation,
	IEstateSearchResultEntry,
	IMapPoint,
	SearchFilter,
} from '@/models/estate/Interfaces';
import store from '@/store/store';
import { AxiosError } from 'axios';
import { computed, Ref, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

export const useEstateSearch = (
	search?: Ref<string>,
	searchFilter?: Ref<SearchFilter>,
	options: {
		updateQueryParams?: boolean;
		getBuildingLocations?: boolean;
	} = {}
) => {
	const router = useRouter();
	const route = useRoute();

	const isBusyLoading = ref(false);
	const searchResults = ref<IEstateSearchResultEntry[] | null>(null);
	const buildings = ref<IBuildingGeoLocation[]>([]);
	const isFetchingBuildingLocations = ref(false);

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
				searchFilter && Object.keys(searchFilter.value).length
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

	let abortController: AbortController | null = null;
	const fetchSearchResults = async (params?: Record<string, unknown>) => {
		isBusyLoading.value = true;
		if (abortController) {
			abortController.abort();
		}
		abortController = new AbortController();
		try {
			if (options.updateQueryParams !== false) {
				updateQueryParams();
			}
			if (options.getBuildingLocations !== false) {
				fetchBuildingLocations(abortController);
			}

			if (
				!search?.value?.trim() &&
				(!searchFilter || Object.keys(searchFilter.value).length === 0)
			) {
				searchResults.value = [];
				return;
			}

			const result = await store.dispatch(DispatchType.GetEstateSearch, {
				params: {
					query: search?.value,
					searchFilter: searchFilter ? searchFilter.value : undefined,
					...params,
				},
				abortController,
			});

			searchResults.value = result;
		} catch (ex) {
			if ((ex as AxiosError).name === 'CanceledError') {
				return;
			}
			throw ex;
		} finally {
			abortController = null;
			isBusyLoading.value = false;
		}
	};

	return {
		fetchSearchResults,
		searchResults,
		buildingPoints,
		isBusyLoading,
		isFetchingBuildingLocations,
	};
};
