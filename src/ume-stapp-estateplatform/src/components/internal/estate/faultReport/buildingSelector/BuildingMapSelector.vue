<template>
	<div>
		<building-map
			v-if="showMap"
			ref="buildingMap"
			:points="buildingPoints"
			:loading="isFetchingBuildingLocations || !!isBusyFetchingBuildingId"
			class="d-none"
			fit-points
			selectable
			@fullscreen-closed="showMap = false"
			@select-building="selectBuilding"
		/>
		<v-btn
			class="regular-text ma-0"
			color="primary"
			variant="flat"
			rounded="xl"
			prepend-icon="location_pin"
			@click="open"
		>
			{{ $t('component.internal.buildingSelector.selectOnMap') }}
		</v-btn>
	</div>
</template>

<script setup lang="ts">
import { nextTick, ref, useTemplateRef } from 'vue';
import { useEstateSearch } from '../../search/useEstateSearch';
import BuildingMap from '../../map/BuildingMap.vue';
import { appInsights } from '@/plugins/appInsights';
import { DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { IBuildingDetails } from '@/models/estate/Interfaces';

const emit = defineEmits(['select']);

const store = useStore<IRootState>();

const buildingMapRef = useTemplateRef('buildingMap');

const { fetchSearchResults, buildingPoints } = useEstateSearch();

const showMap = ref(false);

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

	buildingMapRef.value?.openFullscreen();
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
