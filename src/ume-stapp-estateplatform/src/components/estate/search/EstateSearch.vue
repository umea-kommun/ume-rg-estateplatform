<template>
	<app-content
		class="estate-search estate-details estate-default"
		:pageTitle="$t('component.appHeader.title.default')"
	>
		<div class="container">
			<div class="content px-6 pb-4">
				<nav-breadcrumbs class="mt-4 mb-2" :breadcrumbs="breadcrumbs" />
				<!--
					Portal identity. Kept search-neutral so the page still
					reads as complete when ErrorReport hides the actions.
				-->
				<div class="portal-intro mt-4" v-if="!userHasSearched">
					<h1>{{ $t('component.estatePortal.title') }}</h1>
					<p>{{ $t('component.estatePortal.description') }}</p>
				</div>
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

					<!-- Search results -->
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
				<!-- Desktop counterpart to the mobile map-btn: makes the
				     map pane an entry point instead of a backdrop. -->
				<v-btn
					class="map-cta"
					rounded="pill"
					prepend-icon="location_pin"
					@click="selectBuildingOnMap"
				>
					{{ $t('component.estatePortal.selectOnMap') }}
				</v-btn>
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

const route = useRoute();
const { t, locale } = useI18n();

const buildingMapRef = useTemplateRef('building-map');
const hoveredSearchResultId = ref<number | null>(null);

const search = ref(route.query.search?.toString() || '');
const showSearchFilter = ref(false);
const searchFilter = ref<SearchFilter>(
	route.query.filter ? JSON.parse(route.query.filter.toString()) : {}
);

const breadcrumbs = computed(() => {
	if (!locale.value) return [];
	return [
		{
			title: t('component.estateSearch.breadcrumb'),
			to: { name: EstateRoutes.Search },
		},
	];
});

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
.portal-intro {
	h1 {
		font-size: size(29);
		line-height: 1.2;
		margin: 0 0 4px;
	}

	p {
		margin: 0;
		font-size: size(17);
		color: $grey-darken-3;
		max-width: 46ch;
	}
}

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
	.map .map-cta {
		position: absolute;
		left: 20px;
		bottom: 20px;
		z-index: 2;
		background: #fff;
		color: $primary;
		border-radius: 24px;
		box-shadow: 0 2px 10px rgba(0, 0, 0, 0.18);
	}
	@media only screen and (max-width: $estate-mobile-threshold) {
		.map-btn {
			display: inline-flex;
		}
		.portal-intro h1 {
			font-size: size(24);
		}
	}
}
</style>
