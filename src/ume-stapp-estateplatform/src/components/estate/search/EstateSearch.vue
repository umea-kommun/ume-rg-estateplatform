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

					<!--
						Portal actions. Navigation cards into the three work
						order flows - same visual language as OptionCardGrid
						but with link semantics (arrow hint, no selection
						chrome). Gated on ErrorReport, the same flag as the
						routes, and they yield to search results.
					-->
					<section
						class="mt-6 portal-actions"
						v-if="!userHasSearched && isErrorReportEnabled"
						aria-labelledby="portal-actions-title"
					>
						<h2 id="portal-actions-title">
							{{ $t('component.estatePortal.actionsTitle') }}
						</h2>
						<div class="action-grid mt-3">
							<v-card
								v-for="action in portalActions"
								:key="action.key"
								class="action-card pa-4"
								rounded="lg"
								:to="{ name: action.route }"
								@click="trackPortalAction(action.key)"
							>
								<div class="icon-wrap mb-3">
									<v-icon :icon="action.icon" :size="26" />
								</div>
								<div class="text-h6 font-weight-bold mb-1">
									{{ action.title }}
								</div>
								<div
									class="text-body-2 text-medium-emphasis mb-3"
								>
									{{ action.description }}
								</div>
								<div class="action-hint mt-auto">
									{{ action.hint }}
									<v-icon icon="arrow_forward" :size="18" />
								</div>
							</v-card>
						</div>
					</section>

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

const portalActions = computed(() =>
	[
		{
			key: 'faultReport',
			icon: 'warning',
			route: EstateRoutes.FaultReport,
		},
		{ key: 'order', icon: 'handyman', route: EstateRoutes.Order },
		{
			key: 'spaceRequirement',
			icon: 'space_dashboard',
			route: EstateRoutes.SpaceRequirement,
		},
	].map((action) => ({
		...action,
		title: t(`component.estatePortal.actions.${action.key}.title`),
		description: t(
			`component.estatePortal.actions.${action.key}.description`
		),
		hint: t(`component.estatePortal.actions.${action.key}.hint`),
	}))
);

const trackPortalAction = (type: string) => {
	appInsights?.trackEvent({
		name: 'EstatePortalActionClicked',
		properties: {
			type,
			url: window.location.href,
		},
	});
};

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

.portal-actions {
	// auto-fit tolerates the three cards becoming one (the planned
	// ärendeguide) without a layout change.
	.action-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
		gap: 14px;
	}

	.action-card {
		display: flex;
		flex-direction: column;
		border: 1px solid rgba(0, 0, 0, 0.08);
		transition: border-color 0.15s;

		&:hover,
		&:focus-visible {
			border-color: $primary;
		}

		.icon-wrap {
			width: 48px;
			height: 48px;
			border-radius: 50%;
			display: grid;
			place-items: center;
			background: rgba($primary, 0.08);
			color: $primary;
		}

		.action-hint {
			display: flex;
			align-items: center;
			gap: 4px;
			font-size: size(15);
			font-weight: 600;
			color: $primary;
		}
	}

	// Same compact rows as OptionCardGrid's dense mobile variant.
	@media only screen and (max-width: 600px) {
		.action-grid {
			grid-template-columns: 1fr;
			gap: 8px;
		}

		.action-card {
			flex-direction: row;
			align-items: center;
			gap: 12px;
			padding: 12px !important;

			.icon-wrap {
				width: 40px;
				height: 40px;
				margin-bottom: 0 !important;
				flex: 0 0 auto;
			}

			.text-h6 {
				flex: 1 1 auto;
				margin-bottom: 0 !important;
				font-size: 1rem !important;
				line-height: 1.3;
			}

			.text-body-2,
			.action-hint {
				display: none;
			}
		}
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
