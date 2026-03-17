<template>
	<building-map
		v-if="showMap"
		ref="buildingMap"
		:points="buildingPoints"
		:loading="isFetchingBuildingLocations"
		class="d-none"
		fit-points
		selectable
		@fullscreen-closed="showMap = false"
		@select-building="(buildingId) => emit('select', buildingId)"
	/>
</template>

<script setup lang="ts">
import { nextTick, ref, useTemplateRef } from 'vue';
import { useEstateSearch } from '../../search/useEstateSearch';
import BuildingMap from '../../map/BuildingMap.vue';

const emit = defineEmits(['select']);

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
};

defineExpose({
	open,
});
</script>
