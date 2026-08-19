<template>
	<app-content
		class="estate-search estate-details estate-default"
		:pageTitle="$t('component.appHeader.title.default')"
	>
		<div class="container">
			<div class="content px-6 pb-4">
				<nav-breadcrumbs class="mt-4 mb-2" :breadcrumbs="breadcrumbs" />
				<div class="mt-4">
					<!-- Search bar-->
					<v-text-field
						v-model="search"
						:placeholder="
							$t('component.estateSearch.searchPlaceholder')
						"
						color="primary"
						prepend-inner-icon="search"
						:loading="isBusyLoading"
						clearable
						variant="outlined"
						autocomplete="off"
					>
						<template #append-inner>
							<v-btn
								variant="flat"
								color="primary"
								class="ma-2"
								@click="fetchSearchResults"
							>
								{{ $t('component.estateSearch.searchButton') }}
							</v-btn>
						</template>
					</v-text-field>

					<!-- Search filter -->
					<div class="mt-2 d-flex justify-start">
						<v-btn
							variant="text"
							color="primary"
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
							{{ $t('component.estateSearch.filter') }}
						</v-btn>
						<v-btn
							variant="tonal"
							class="map-btn ma-0 ml-2"
							color="primary"
							prepend-icon="location_pin"
							@click="selectBuildingOnMap"
						>
							{{ $t('component.estateSearch.selectOnMap') }}
						</v-btn>
					</div>

					<estate-search-filter
						v-show="showSearchFilter"
						v-model="searchFilter"
						@close="showSearchFilter = false"
					/>

					<!--
						Portal services. These were the estate card's buttons on
						Mina sidor's InternalStart, which is the only place they
						ever linked from - see migrate_plan/handover.md.
						Gated on ErrorReport, the same flag as the routes.
					-->
					<div
						class="mt-6 portal-services"
						v-if="!userHasSearched && isErrorReportEnabled"
					>
						<h2 class="services-title">
							{{ $t('app.nav.services.title') }}
						</h2>
						<div class="services-actions mt-2">
							<v-btn
								variant="outlined"
								size="x-large"
								prependIcon="warning"
								:to="{ name: EstateRoutes.FaultReport }"
							>
								{{ $t('app.nav.services.faultReport') }}
							</v-btn>
							<v-btn
								variant="outlined"
								size="x-large"
								prependIcon="handyman"
								:to="{ name: EstateRoutes.Order }"
							>
								{{ $t('app.nav.services.order') }}
							</v-btn>
							<v-btn
								variant="outlined"
								size="x-large"
								prependIcon="space_dashboard"
								:to="{ name: EstateRoutes.SpaceRequirement }"
							>
								{{ $t('app.nav.services.spaceRequirement') }}
							</v-btn>
						</div>
					</div>

					<!-- Search results -->
					<div class="mt-4 pb-4 search-help" v-if="!userHasSearched">
						<v-alert color="primary" variant="tonal">
							{{ $t('component.estateSearch.searchHelp') }}
						</v-alert>
					</div>
					<v-alert
						v-if="
							!isBusyLoading &&
							searchResults?.length === 0 &&
							userHasSearched
						"
						class="mt-4"
						icon="info"
					>
						{{ $t('component.estateSearch.noResults') }}
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

				<div v-if="!isBusyLoading && !userHasSearched" class="mt-4">
					<favorite-list class="mt-4" />
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
import { computed, onMounted, ref, useTemplateRef } from 'vue';
import EstateSearchResultItem from './EstateSearchResultItem.vue';
import { SearchFilter } from '@/models/Interfaces';
import { watchDebounced } from '@vueuse/core';
import { useRoute } from 'vue-router';
import AppContent from '@/components/app/AppContent.vue';
import '@/themes/estate.scss';
import { EstateRoutes } from '@/router/routes';
import { useI18n } from 'vue-i18n';
import BuildingMap from '@/components/estate/map/BuildingMap.vue';
import EstateSearchFilter from './EstateSearchFilter.vue';
import NavBreadcrumbs from '../../shared/NavBreadcrumbs.vue';
import { useEstateSearch } from './useEstateSearch';
import FavoriteList from '../favorite/FavoriteList.vue';
import { appInsights } from '@/plugins/appInsights';
import { useFeatureFlags } from '@/utils/useFeatureFlags';

const route = useRoute();
const { t } = useI18n();
const { isEnabled } = useFeatureFlags();

const isErrorReportEnabled = computed(() => isEnabled('ErrorReport'));

const buildingMapRef = useTemplateRef('building-map');
const hoveredSearchResultId = ref<number | null>(null);

const search = ref(route.query.search?.toString() || '');
const showSearchFilter = ref(false);
const searchFilter = ref<SearchFilter>(
	route.query.filter ? JSON.parse(route.query.filter.toString()) : {}
);

const breadcrumbs = [
	{
		title: t('component.estateSearch.breadcrumb'),
		to: { name: EstateRoutes.Search },
	},
];

const userHasSearched = computed(() => {
	return !!search.value || Object.keys(searchFilter.value).length > 0;
});

const selectBuildingOnMap = () => {
	buildingMapRef.value?.openFullscreen();
	appInsights?.trackEvent({
		name: 'EstateSelectOnMapClicked',
		properties: {
			url: window.location.href,
		},
	});
};

const {
	fetchSearchResults,
	searchResults,
	buildingPoints,
	isBusyLoading,
	isFetchingBuildingLocations,
} = useEstateSearch(search, searchFilter);

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
.portal-services {
	.services-title {
		font-size: 1.1rem;
		margin: 0;
	}

	.services-actions {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
		gap: 14px;

		.v-btn {
			width: 100%;
			margin: 0;
		}
	}
}

.estate-search {
	.search-help {
		border-bottom: solid 1px $grey-lighten-4;
	}
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
