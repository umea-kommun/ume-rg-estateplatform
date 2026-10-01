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
				<div class="portal-intro">
					<h1 class="mt-0 mb-2">
						{{ $t('component.estateSearch.title') }}
					</h1>
					<p class="my-0">
						{{ $t('component.estateSearch.description') }}
					</p>
				</div>
				<div class="mt-4">
					<!-- Search bar-->
					<v-text-field
						ref="search-field"
						v-model="search"
						class="search-field"
						:placeholder="
							$t('component.estateSearch.searchPlaceholder')
						"
						color="primary"
						prepend-inner-icon="search"
						:loading="isBusyLoading"
						clearable
						variant="outlined"
						autocomplete="off"
						enterkeyhint="search"
						@keyup.enter="submitSearch"
					>
						<template #append-inner>
							<v-btn
								variant="flat"
								color="primary"
								class="ma-2"
								@click="submitSearch"
							>
								{{ $t('component.estateSearch.searchButton') }}
							</v-btn>
						</template>
					</v-text-field>

					<v-btn
						variant="tonal"
						class="map-btn mt-2"
						color="primary"
						prepend-icon="location_pin"
						block
						@click="selectBuildingOnMap"
					>
						{{ $t('component.estateSearch.selectOnMap') }}
					</v-btn>

					<!-- Filters -->
					<div
						class="filter-bar mt-4 d-flex flex-wrap align-center ga-2"
					>
						<v-btn
							variant="text"
							@click="showSearchFilter = !showSearchFilter"
						>
							<template #prepend>
								<v-icon
									icon="expand_more"
									:size="20"
									class="chevron"
									:class="{
										'chevron-expanded': showSearchFilter,
									}"
								/>
							</template>
							{{ filterToggleLabel }}
						</v-btn>
						<estate-search-filter
							v-model="searchFilter"
							:expanded="showSearchFilter"
						/>
					</div>

					<!-- Search results -->
					<div class="mt-4">
						<h2 class="mb-4">
							{{ $t('component.estateSearch.resultsHeading') }}
						</h2>
						<v-alert v-if="resultsInfoMessage" icon="info">
							{{ resultsInfoMessage }}
						</v-alert>
						<template v-if="showResultSkeletons">
							<v-skeleton-loader
								v-for="n in RESULT_SKELETON_COUNT"
								:key="n"
								class="result-skeleton mb-4"
								type="heading, text@2, chip@3"
								elevation="1"
							/>
						</template>
						<estate-search-result-item
							v-else
							v-for="entry in searchResults"
							:key="entry.type + entry.id"
							:entry="entry"
							class="mb-4 pl-0"
							@mouseenter="hoveredSearchResultId = entry.id"
							@mouseleave="hoveredSearchResultId = null"
						/>
						<div
							v-for="group in otherTypeResults"
							:key="group.type"
							class="other-type-results"
						>
							<h3 class="mt-6 mb-2">
								{{
									$t(
										`component.estateSearch.otherTypeHeading.${group.type}`
									)
								}}
							</h3>
							<estate-search-result-item
								v-for="entry in group.entries"
								:key="entry.type + entry.id"
								:entry="entry"
								class="mb-4 pl-0"
								@mouseenter="hoveredSearchResultId = entry.id"
								@mouseleave="hoveredSearchResultId = null"
							/>
						</div>
					</div>

					<div v-if="isCleanSearchPage" class="mt-4">
						<favorite-list class="mt-4" />
					</div>
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
import { computed, nextTick, onMounted, ref, useTemplateRef, watch } from 'vue';
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
import FavoriteList from '../favorite/FavoriteList.vue';
import {
	createDefaultSearchFilter,
	hasActiveSearchCriteria,
	useEstateSearch,
} from './useEstateSearch';
import { appInsights } from '@/plugins/appInsights';
import { useEstateIsMobile } from '../useEstateIsMobile';

const route = useRoute();
const { t, locale } = useI18n();

const buildingMapRef = useTemplateRef('building-map');
const searchFieldRef = useTemplateRef('search-field');
const isMobile = useEstateIsMobile();
const hoveredSearchResultId = ref<number | null>(null);

const search = ref(route.query.search?.toString() || '');
const showSearchFilter = ref(false);
const filterFromQuery: SearchFilter = route.query.filter
	? JSON.parse(route.query.filter.toString())
	: {};

if (!filterFromQuery.types?.length) {
	filterFromQuery.types = createDefaultSearchFilter().types;
}

const searchFilter = ref<SearchFilter>(filterFromQuery);

const RESULT_SKELETON_COUNT = 6;

const filterToggleLabel = computed(() =>
	showSearchFilter.value
		? t('component.estateSearch.hideFilters')
		: t('component.estateSearch.showFilters')
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
	return (
		!!search.value?.trim() || hasActiveSearchCriteria(searchFilter.value)
	);
});

const selectBuildingOnMap = () => {
	buildingMapRef.value?.open();
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
	otherTypeResults,
} = useEstateSearch(search, searchFilter, { suggestOtherTypes: true });

const scrollToSearchField = async () => {
	if (!isMobile.value) {
		return;
	}
	await nextTick();
	searchFieldRef.value?.$el.scrollIntoView({
		behavior: 'smooth',
		block: 'start',
	});
};

const submitSearch = () => {
	const input = searchFieldRef.value?.$el.querySelector('input');
	input?.blur();
	fetchSearchResults();
};

const showResultSkeletons = computed(
	() => isBusyLoading.value && searchResults.value === null
);

const isCleanSearchPage = computed(
	() => !userHasSearched.value && searchResults.value === null
);

const resultsInfoMessage = computed(() => {
	if (isCleanSearchPage.value) {
		return t('component.estateSearch.noSearchYet');
	}
	if (searchResults.value?.length !== 0) {
		return null;
	}
	const scopedTypes = searchFilter.value.types ?? [];
	if (scopedTypes.length === 1) {
		return t(`component.estateSearch.noResultsForType.${scopedTypes[0]}`);
	}
	return t('component.estateSearch.noResults');
});

watch(userHasSearched, (searched) => {
	if (searched) {
		scrollToSearchField();
	}
});

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
	.chevron {
		transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);

		&.chevron-expanded {
			transform: rotate(-180deg);
		}
	}
	:deep(.expand-transition-enter-active),
	:deep(.expand-transition-leave-active) {
		transition-duration: 0.1s;
	}
	.result-skeleton {
		border-radius: $border-radius;
		overflow: hidden;
	}
	.v-alert {
		border-radius: $border-radius;
		:deep(.v-icon) {
			color: $grey-darken-3;
		}
	}
	.search-field {
		scroll-margin-top: calc($site-header-height + 1rem);
	}
	.content {
		min-height: 50svh;
	}
	.map-btn {
		display: none;
	}
	@media only screen and (max-width: $estate-mobile-threshold) {
		.map-btn {
			display: flex;
		}
		.portal-intro h1 {
			font-size: size(24);
		}
		// Keeps the page tall enough to hold the heading at the top of the
		// viewport once the start page content collapses into results.
		.content {
			min-height: 100svh;
		}
	}
}
</style>
