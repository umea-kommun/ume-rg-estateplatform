<template>
	<div>
		<building-map
			v-if="showMap"
			ref="buildingMap"
			:points="mapPoints"
			:loading="
				isFetchingBuildingLocations ||
				searchLoading ||
				!!isBusyFetchingBuildingId
			"
			class="d-none"
			fit-points
			selectable
			@fullscreen-closed="showMap = false"
			@select-building="selectBuilding"
		>
			<template #overlay>
				<v-text-field
					class="map-search elevation-2"
					:model-value="search"
					@update:model-value="emit('update:search', $event ?? '')"
					@keydown.enter.stop.prevent="emit('submit-search')"
					:placeholder="$t('component.estateSearch.searchButton')"
					:aria-label="$t('component.buildingSelector.searchLabel')"
					:loading="searchLoading"
					prepend-inner-icon="search"
					density="compact"
					variant="solo"
					flat
					clearable
					hide-details
					rounded="lg"
					autocomplete="off"
				/>
			</template>
		</building-map>
		<v-btn
			color="primary"
			variant="tonal"
			rounded="lg"
			prepend-icon="location_pin"
			@click="open"
		>
			{{ $t('component.buildingSelector.selectOnMap') }}
		</v-btn>
	</div>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, useTemplateRef } from 'vue';
import { useEstateSearch } from '../../search/useEstateSearch';
import BuildingMap from '../../map/BuildingMap.vue';
import { appInsights } from '@/plugins/appInsights';
import { DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { IBuildingDetails } from '@/models/Interfaces';

const props = defineProps<{
	// Null or omitted means all buildings; an empty array means no search hits.
	searchResultIds?: number[] | null;
	search?: string;
	searchLoading?: boolean;
}>();

const emit = defineEmits(['select', 'update:search', 'submit-search']);

const store = useStore<IRootState>();

const buildingMapRef = useTemplateRef('buildingMap');

const { fetchSearchResults, buildingPoints } = useEstateSearch();

const showMap = ref(false);

const mapPoints = computed(() => {
	if (props.searchResultIds == null) {
		return buildingPoints.value;
	}
	const ids = new Set(props.searchResultIds);
	return buildingPoints.value.filter((point) => ids.has(point.id as number));
});

const waitForMapToRender = async () => {
	for (let i = 0; i < 3; i++) {
		if (buildingMapRef.value) {
			return;
		}
		await nextTick();
	}
};

const isFetchingBuildingLocations = ref(false);
const fetchBuildingLocations = async () => {
	if (buildingPoints.value.length > 0) {
		return; // Already have building locations, no need to fetch again
	}

	isFetchingBuildingLocations.value = true;
	try {
		await fetchSearchResults();
	} finally {
		isFetchingBuildingLocations.value = false;
	}
};

const open = async () => {
	fetchBuildingLocations();

	showMap.value = true;
	await waitForMapToRender();

	buildingMapRef.value?.open();
	appInsights?.trackEvent({
		name: 'EstateSelectOnMapClicked',
		properties: {
			url: window.location.href,
		},
	});
};
const isBusyFetchingBuildingId = ref<number | null>(null);
const selectBuilding = async (buildingId: number) => {
	isBusyFetchingBuildingId.value = buildingId;
	try {
		const building: IBuildingDetails = await store.dispatch(
			DispatchType.GetBuildingById,
			{
				buildingId: buildingId,
			}
		);
		emit('select', building);
		showMap.value = false;
		appInsights?.trackEvent({
			name: 'EstateBuildingSelectedOnMap',
			properties: {
				url: window.location.href,
				buildingId,
				buildingName: building.name,
			},
		});
	} finally {
		isBusyFetchingBuildingId.value = null;
	}
};
</script>

<style scoped lang="scss">
.map-search {
	position: absolute;
	top: 14px;
	left: 14px;
	width: 280px;
	max-width: calc(100% - 124px);
	border-radius: $border-radius;

	:deep(.v-field) {
		color: $grey-darken-4;
		--v-high-emphasis-opacity: 1;
	}

	:deep(.v-icon) {
		color: $grey-darken-4;
		opacity: 1;
	}
}
</style>
