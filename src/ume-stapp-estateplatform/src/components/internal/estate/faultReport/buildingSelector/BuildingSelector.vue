<template>
	<div class="building-selector">
		<div v-if="selectedBuilding" class="selected-building mt-2 elevation-1">
			<building-map
				ref="building-map"
				:points="selectedBuildingMapPoints"
				hide-controls
				fit-points
			/>
			<building-selector-item
				:entry="selectedBuilding"
				class="px-4 py-4"
			/>
			<building-notice-board
				v-if="selectedBuilding.noticeBoard"
				class="mx-4"
				:notice-board="selectedBuilding.noticeBoard"
			/>
		</div>
		<div v-else>
			<div>
				<v-text-field
					v-model="search"
					:label="
						$t('component.internal.buildingSelector.searchLabel')
					"
					color="primary"
					item-title="popularName"
					item-value="id"
					prepend-inner-icon="search"
					clearable
					rounded="lg"
					variant="outlined"
					class="mt-4"
					autocomplete="off"
					:loading="isBusyLoading"
				>
				</v-text-field>
			</div>

			<v-alert
				v-if="!isBusyLoading && searchResults?.length === 0 && search"
				class="mt-4"
				icon="info"
			>
				{{ $t('component.internal.buildingSelector.noResults') }}
			</v-alert>
			<v-alert
				v-if="!search && !searchResults?.length"
				class="mt-4"
				icon="info"
			>
				{{ $t('component.internal.buildingSelector.searchHelp') }}
			</v-alert>
			<v-list class="mt-2" v-if="searchResults?.length">
				<estate-search-result-item
					v-for="entry in searchResults"
					:key="entry.type + entry.id"
					:entry="entry"
					@click="selectBuilding(entry.id)"
					:to="undefined"
					class="mb-2"
					:loading="isBusyFetchingBuildingId === entry.id"
					@mouseenter="hoveredSearchResultId = entry.id"
					@mouseleave="hoveredSearchResultId = null"
				/>
			</v-list>
			<div v-if="!searchResults?.length && !search">
				<favorite-list
					class="mt-4"
					@select-building="emit('select', $event)"
					@select-room="emit('select-room', $event)"
					:types="[EstateType.Building, EstateType.Room]"
					selectable
				>
					<template #header="{ count }">
						<h3 class="mt-4">
							{{
								$t(
									'component.internal.buildingSelector.favoritesTitle',
									{ count }
								)
							}}
						</h3>
						<p class="text-medium-emphasis mb-4" v-if="count">
							{{
								$t(
									'component.internal.buildingSelector.favoritesDescription'
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
import { IBuildingDetails } from '@/models/estate/Interfaces';
import { computed, onMounted, ref } from 'vue';
import { useEstateSearch } from '@/components/internal/estate/search/useEstateSearch';
import { watchDebounced } from '@vueuse/core';
import { DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import BuildingNoticeBoard from '@/components/internal/estate/building/BuildingNoticeBoard.vue';
import BuildingSelectorItem from './BuildingSelectorItem.vue';
import BuildingMap from '@/components/internal/estate/map/BuildingMap.vue';
import EstateSearchResultItem from '@/components/internal/estate/search/EstateSearchResultItem.vue';
import FavoriteList from '../../favorite/FavoriteList.vue';
import { EstateType } from '@/models/estate/Enums';

const props = defineProps<{
	selectedBuilding: IBuildingDetails | null;
}>();

const emit = defineEmits(['select', 'select-room']);

const store = useStore<IRootState>();

const hoveredSearchResultId = ref<number | null>(null);

const SEARCH_RESULT_LIMIT = 10;
const search = ref('');

const { fetchSearchResults, searchResults, isBusyLoading } = useEstateSearch(
	search,
	undefined,
	{ updateQueryParams: false, getBuildingLocations: false }
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

watchDebounced(
	() => search.value,
	() => {
		fetchSearchResults({ type: ['building'], limit: SEARCH_RESULT_LIMIT });
	},
	{ debounce: 200, maxWait: 500 }
);
onMounted(() => {
	fetchSearchResults({ type: ['building'], limit: SEARCH_RESULT_LIMIT });
});
</script>

<style lang="scss" scoped>
.building-selector {
	.selected-building {
		width: 100%;
		border: solid 1px $grey-lighten-2;
		border-radius: $border-radius;
		overflow: hidden;

		.building-selector-item {
			flex: 1;
			border: none;
		}
		.building-map {
			pointer-events: none;
			flex: 1;
			height: 150px;
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
