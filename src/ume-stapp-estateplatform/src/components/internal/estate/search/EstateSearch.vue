<template>
	<app-content
		class="estate-search estate-details estate-default"
		:pageTitle="$t('component.appHeader.title.internalEstate')"
	>
		<div class="container">
			<div class="content px-6 pb-4">
				<nav-breadcrumbs class="mt-4 mb-2" :breadcrumbs="breadcrumbs" />
				<div class="mt-4">
					<!-- Search bar-->
					<v-text-field
						v-model="search"
						:placeholder="
							$t(
								'component.internal.estateSearch.searchPlaceholder'
							)
						"
						color="primary"
						prepend-inner-icon="search"
						clearable
						variant="outlined"
						autocomplete="off"
					>
						<template #append-inner>
							<v-btn
								variant="flat"
								class="regular-text"
								color="primary"
								@click="fetchSearchResults"
							>
								{{
									$t(
										'component.internal.estateSearch.searchButton'
									)
								}}
							</v-btn>
						</template>
					</v-text-field>

					<!-- Search filter -->
					<div class="mt-2 d-flex justify-start">
						<v-btn
							variant="text"
							color="primary"
							class="regular-text ma-0"
							@click="showSearchFilter = !showSearchFilter"
							:active="showSearchFilter"
						>
							<template #prepend>
								<div
									class="indicator-icon"
									:class="{
										'indicator-active':
											Object.keys(searchFilter).length,
									}"
								>
									<v-icon icon="filter_list" :size="20" />
								</div>
							</template>
							{{ $t('component.internal.estateSearch.filter') }}
						</v-btn>
						<v-btn
							variant="tonal"
							class="map-btn regular-text ma-0 ml-2"
							color="primary"
							prepend-icon="location_pin"
							@click="buildingMapRef?.openFullscreen()"
						>
							{{
								$t(
									'component.internal.estateSearch.selectOnMap'
								)
							}}
						</v-btn>
					</div>

					<estate-search-filter
						v-show="showSearchFilter"
						v-model="searchFilter"
						@close="showSearchFilter = false"
					/>

					<!-- Search results -->
					<v-alert
						v-if="
							!isBusyLoading &&
							searchResults?.length === 0 &&
							(search || Object.keys(searchFilter).length)
						"
						class="mt-4"
						icon="info"
					>
						{{ $t('component.internal.estateSearch.noResults') }}
					</v-alert>
					<div class="mt-4" v-if="searchResults?.length">
						<estate-search-result-item
							v-for="entry in searchResults"
							:key="entry.type + entry.id"
							:entry="entry"
							class="mb-4 pl-0"
							@mouseenter="hoveredSearchResultId = entry.id"
							@mouseleave="hoveredSearchResultId = null"
						/>
					</div>
					<v-skeleton-loader
						v-if="isBusyLoading"
						class="my-4"
						type="article"
						:loading="isBusyLoading"
					/>
				</div>
			</div>

			<div class="map">
				<building-map
					ref="building-map"
					:points="buildingPoints"
					:highlighted-point-id="hoveredSearchResultId"
					:loading="isFetchingBuildingLocations"
					fit-points
				/>
			</div>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import { IRootState } from '@/models/Interfaces';
import { AxiosError } from 'axios';
import { computed, onMounted, ref, useTemplateRef } from 'vue';
import { useStore } from 'vuex';
import EstateSearchResultItem from './EstateSearchResultItem.vue';
import {
	IBuildingGeoLocation,
	IEstateSearchResultEntry,
	IMapPoint,
	SearchFilter,
} from '@/models/estate/Interfaces';
import { watchDebounced } from '@vueuse/core';
import { useRoute, useRouter } from 'vue-router';
import AppContent from '@/components/app/AppContent.vue';
import '@/themes/estate.scss';
import { EstateRoutes, MyPagesRoutes } from '@/router/routes';
import { useI18n } from 'vue-i18n';
import { EstateType } from '@/models/estate/Enums';
import BuildingMap from '@/components/internal/estate/map/BuildingMap.vue';
import EstateSearchFilter from './EstateSearchFilter.vue';
import NavBreadcrumbs from '../../shared/NavBreadcrumbs.vue';

const router = useRouter();
const route = useRoute();
const store = useStore<IRootState>();
const { t } = useI18n();

const isBusyLoading = ref(false);
const buildingMapRef = useTemplateRef('building-map');
const hoveredSearchResultId = ref<number | null>(null);

const search = ref(route.query.search?.toString() || '');
const showSearchFilter = ref(false);
const searchFilter = ref<SearchFilter>(
	route.query.filter ? JSON.parse(route.query.filter.toString()) : {}
);

const searchResults = ref<IEstateSearchResultEntry[] | null>(null);
const buildings = ref<IBuildingGeoLocation[]>([]);

const breadcrumbs = computed(() => {
	return [
		{
			title: t('app.nav.home'),
			to: { name: MyPagesRoutes.InternalStart },
		},
		{
			title: t('component.internal.estateSearch.breadcrumb'),
			to: { name: EstateRoutes.Search },
		},
	];
});

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
		search: search.value || undefined,
		filter: Object.keys(searchFilter.value).length
			? JSON.stringify(searchFilter.value)
			: undefined,
	};
	if (route.name) {
		router.replace({ name: route.name, query: queryParams });
	}
};

const isFetchingBuildingLocations = ref(false);
const fetchBuildingLocations = async (abortController?: AbortController) => {
	isFetchingBuildingLocations.value = true;
	try {
		buildings.value = await store.dispatch(
			DispatchType.GetEstateSearchGeoLocations,
			{
				params: {
					query: search.value,
					searchFilter: searchFilter.value,
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
const fetchSearchResults = async () => {
	isBusyLoading.value = true;
	if (abortController) {
		abortController.abort();
	}
	abortController = new AbortController();
	try {
		updateQueryParams();
		fetchBuildingLocations(abortController);

		if (
			!search.value?.trim() &&
			Object.keys(searchFilter.value).length === 0
		) {
			searchResults.value = [];
			return;
		}

		const result = await store.dispatch(DispatchType.GetEstateSearch, {
			params: {
				query: search.value,
				searchFilter: searchFilter.value,
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

watchDebounced(
	() => [search.value, searchFilter.value],
	() => {
		fetchSearchResults();
	},
	{ debounce: 200, maxWait: 400, deep: true }
);
onMounted(() => {
	fetchSearchResults();
});
</script>

<style lang="scss" scoped>
.estate-search {
	:deep(.v-btn),
	:deep(.v-field) {
		border-radius: $border-radius;

		&.v-field--appended {
			padding-right: 0;
		}
	}
	.indicator-icon.indicator-active {
		::before {
			content: ' ';
			position: absolute;
			top: 0;
			right: 0;
			width: 10px;
			height: 10px;
			background: $primary;
			border-radius: 20px;
			border: 2px solid #fff;
		}
	}
	.v-alert {
		border-radius: $border-radius;
		:deep(.v-icon) {
			color: $grey-darken-3;
		}
	}
	.content {
		min-height: 50svh;
	}
	.map-btn {
		display: none;
	}
	@media only screen and (max-width: $estate-mobile-threshold) {
		.map-btn {
			display: inline-flex;
		}
	}
}
</style>
