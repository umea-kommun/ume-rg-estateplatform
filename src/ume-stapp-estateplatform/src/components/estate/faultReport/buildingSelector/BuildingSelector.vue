<template>
	<div class="building-selector">
		<div v-if="selectedBuilding" class="selected-building mt-2">
			<selection-card
				:image-url="buildingThumbUrl"
				icon="apartment"
				:title="selectedBuilding.popularName || selectedBuilding.name"
				:description="buildingAddress"
			>
				<template #actions>
					<v-btn
						v-if="selectedBuilding.geoLocation"
						variant="text"
						size="small"
						rounded="lg"
						color="primary"
						:prepend-icon="showMap ? 'expand_less' : 'map'"
						@click="showMap = !showMap"
					>
						{{
							showMap
								? $t('component.faultReport.building.hideMap')
								: $t('component.faultReport.building.showMap')
						}}
					</v-btn>
					<v-btn
						rounded="lg"
						variant="outlined"
						color="grey-darken-2"
						@click="emit('select', null)"
					>
						{{ $t('component.faultReport.changeAnswer') }}
					</v-btn>
				</template>
			</selection-card>

			<building-map
				v-if="showMap && selectedBuilding.geoLocation"
				ref="building-map"
				class="selected-building-map mt-2"
				:points="selectedBuildingMapPoints"
				hide-controls
				fit-points
			/>

			<building-notice-board
				v-if="selectedBuilding.noticeBoard"
				class="mt-2"
				:notice-board="selectedBuilding.noticeBoard"
			/>
		</div>
		<div v-else>
			<!-- Search and the "pick on map" action share a row: two equal ways to
			find a building, kept out of the step title. The button wraps below the
			field on narrow screens. -->
			<div class="search-row mt-4">
				<v-text-field
					v-model="search"
					:label="$t('component.buildingSelector.searchLabel')"
					color="primary"
					item-title="popularName"
					item-value="id"
					prepend-inner-icon="search"
					clearable
					rounded="lg"
					variant="outlined"
					class="search-field"
					autocomplete="off"
					hide-details
					:loading="isBusyLoading"
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
				<div v-if="$slots['search-action']" class="search-action">
					<slot name="search-action" />
				</div>
			</div>

			<v-alert
				v-if="!isBusyLoading && searchResults?.length === 0 && search"
				class="mt-4"
				icon="info"
			>
				{{ $t('component.buildingSelector.noResults') }}
			</v-alert>
			<v-alert
				v-if="!search && !searchResults?.length"
				class="mt-4"
				icon="info"
			>
				{{ $t('component.buildingSelector.searchHelp') }}
			</v-alert>
			<v-list class="mt-2" v-if="searchResults?.length">
				<estate-search-result-item
					v-for="entry in visibleSearchResults"
					:key="entry.type + entry.id"
					:entry="entry"
					@click="selectBuilding(entry.id)"
					:to="undefined"
					class="mb-2"
					:loading="isBusyFetchingBuildingId === entry.id"
					@mouseenter="hoveredSearchResultId = entry.id"
					@mouseleave="hoveredSearchResultId = null"
				/>
				<div
					v-if="searchResults.length > visibleCount"
					class="d-flex justify-center mt-2"
				>
					<v-btn
						variant="text"
						rounded="lg"
						color="primary"
						append-icon="expand_more"
						@click="visibleCount += SEARCH_RESULT_PAGE_SIZE"
					>
						{{ $t('component.buildingSelector.showMore') }}
					</v-btn>
				</div>
			</v-list>
			<div v-if="!searchResults?.length && !search">
				<favorite-list
					class="mt-4"
					@select-building="emit('select', $event)"
					@select-room="emit('select-room', $event)"
					:types="[EstateType.Building, EstateType.Room]"
					selectable
					compact
				>
					<template #header="{ count }">
						<h3 class="mt-4">
							{{
								$t(
									'component.buildingSelector.favoritesTitle',
									{ count }
								)
							}}
						</h3>
						<p class="text-medium-emphasis mb-4" v-if="count">
							{{
								$t(
									'component.buildingSelector.favoritesDescription'
								)
							}}
						</p>
					</template>
				</favorite-list>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { IBuildingDetails } from '@/models/Interfaces';
import { computed, onMounted, ref } from 'vue';
import { useEstateSearch } from '@/components/estate/search/useEstateSearch';
import { watchDebounced } from '@vueuse/core';
import { DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import BuildingNoticeBoard from '@/components/estate/building/BuildingNoticeBoard.vue';
import BuildingMap from '@/components/estate/map/BuildingMap.vue';
import SelectionCard from '../../order/SelectionCard.vue';
import EstateSearchResultItem from '@/components/estate/search/EstateSearchResultItem.vue';
import FavoriteList from '../../favorite/FavoriteList.vue';
import { EstateType } from '@/models/Enums';

const props = defineProps<{
	selectedBuilding: IBuildingDetails | null;
}>();

const emit = defineEmits(['select', 'select-room']);

const store = useStore<IRootState>();

const hoveredSearchResultId = ref<number | null>(null);

// The map is hidden by default to keep the selected-building card compact; the
// "Visa karta" action expands it on demand.
const showMap = ref(false);

const SEARCH_RESULT_LIMIT = 25;
// Show a short list by default so the step stays compact; "show more" reveals the
// rest of the (already fetched) results a page at a time.
const SEARCH_RESULT_PAGE_SIZE = 5;
const search = ref('');
const visibleCount = ref(SEARCH_RESULT_PAGE_SIZE);

// Address data comes in mixed/upper case (e.g. "UMEÅ"); title-case each word so
// it reads like the rest of the UI, matching the old building list item.
const titleCase = (value: string) =>
	value
		.toLocaleLowerCase()
		.replace(
			/\p{L}[\p{L}\p{M}]*/gu,
			(word) => word[0].toLocaleUpperCase() + word.slice(1)
		);

const buildingAddress = computed(() => {
	const address = props.selectedBuilding?.address;
	if (!address) return '';
	const street = address.street?.trim() || '';
	let result = street;
	if (street && address.zipCode && address.city) {
		result = `${street}, ${address.zipCode} ${address.city}`;
	} else if (street && address.city) {
		result = `${street}, ${address.city}`;
	}
	return titleCase(result);
});

// Request a small, retina-friendly thumbnail rather than the full-size image
// for the 44px card media.
const buildingThumbUrl = computed(() =>
	props.selectedBuilding?.imageUrl
		? `${props.selectedBuilding.imageUrl}?w=88`
		: null
);

const { fetchSearchResults, searchResults, isBusyLoading } = useEstateSearch(
	search,
	undefined,
	{
		updateQueryParams: false,
		getBuildingLocations: false,
	}
);

const visibleSearchResults = computed(
	() => searchResults.value?.slice(0, visibleCount.value)
);

const isBusyFetchingBuildingId = ref<number | null>(null);
const selectBuilding = async (buildingId: number) => {
	isBusyFetchingBuildingId.value = buildingId;
	try {
		const building = await store.dispatch(DispatchType.GetBuildingById, {
			buildingId: buildingId,
		});
		emit('select', building);
	} finally {
		isBusyFetchingBuildingId.value = null;
	}
};

const selectedBuildingMapPoints = computed(() => {
	if (!props.selectedBuilding?.geoLocation) {
		return [];
	}
	return [
		{
			id: props.selectedBuilding.id,
			type: props.selectedBuilding.type,
			lon: props.selectedBuilding.geoLocation.lon,
			lat: props.selectedBuilding.geoLocation.lat,
		},
	];
});

const submitSearch = () => {
	fetchSearchResults({ type: ['building'], limit: SEARCH_RESULT_LIMIT });
};

watchDebounced(
	() => search.value,
	() => {
		visibleCount.value = SEARCH_RESULT_PAGE_SIZE;
		submitSearch();
	},
	{ debounce: 200, maxWait: 500 }
);
onMounted(submitSearch);
</script>

<style lang="scss" scoped>
.building-selector {
	// Search field + "Välj på karta" on one row; the button aligns with the input
	// control box and drops full-width below the field on narrow screens.
	.search-row {
		display: flex;
		align-items: stretch;
		gap: 12px;

		.search-field {
			flex: 1 1 auto;
			min-width: 0;
		}
		.search-action {
			flex: 0 0 auto;
			display: flex;
			:deep(.v-btn) {
				height: 100%;
				min-height: 36px;
			}
		}

		@media only screen and (max-width: 600px) {
			flex-direction: column;
			.search-action,
			.search-action :deep(.v-btn) {
				width: 100%;
			}
		}
	}

	.selected-building {
		width: 100%;

		.selected-building-map {
			pointer-events: none;
			height: 150px;
			border: solid 1px rgba(0, 0, 0, 0.08);
			border-radius: $border-radius;
			overflow: hidden;
		}
	}
	.v-list {
		overflow: visible;
	}
	.v-alert {
		border-radius: $border-radius;
		:deep(.v-icon) {
			color: $grey-darken-3;
		}
	}
	.favorite-list {
		border-top: solid 1px #f2f2f2;
	}
}
</style>
