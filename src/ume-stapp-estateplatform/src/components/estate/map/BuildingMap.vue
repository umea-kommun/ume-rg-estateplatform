<template>
	<div class="building-map">
		<map-viewer
			v-if="!fullscreen"
			map-id="regular-map"
			v-model:fullscreen="fullscreen"
			:points="points"
			:highlighted-point-id="highlightedPointId"
			:fit-points="fitPoints"
			:loading="loading"
			:hide-controls="hideControls"
			:selectable="selectable"
			@select-building="
				(buildingId) => emit('select-building', buildingId)
			"
		/>

		<v-dialog
			v-model="fullscreen"
			fullscreen
			hide-overlay
			class="building-map-dialog"
		>
			<map-viewer
				v-if="fullscreen"
				map-id="fullscreen-map"
				v-model:fullscreen="fullscreen"
				:points="points"
				:highlighted-point-id="highlightedPointId"
				:fit-points="fitPoints"
				:loading="loading"
				:selectable="selectable"
				@select-building="
					(buildingId) => emit('select-building', buildingId)
				"
			/>
		</v-dialog>
	</div>
</template>

<script setup lang="ts">
import { IMapPoint, IMapState } from '@/models/Interfaces';
import { ref, watch } from 'vue';
import MapViewer from './MapViewer.vue';

defineProps<{
	points?: IMapPoint[];
	fitPoints?: boolean;
	initialState?: IMapState | null;
	highlightedPointId?: number | null;
	loading?: boolean;
	hideControls?: boolean;
	selectable?: boolean;
}>();

const emit = defineEmits(['fullscreen-closed', 'select-building']);

const fullscreen = ref(false);

watch(fullscreen, (newVal) => {
	if (!newVal) {
		emit('fullscreen-closed');
	}
});

const openFullscreen = () => {
	fullscreen.value = true;
};

defineExpose({
	openFullscreen,
});
</script>

<style scoped lang="scss">
.building-map {
	position: relative;
	height: 100%;
	width: 100%;
}
.building-map-dialog {
	background-color: #fff;
}
</style>
