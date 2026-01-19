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
			/>
		</v-dialog>
	</div>
</template>

<script setup lang="ts">
import { IMapPoint, IMapState } from '@/models/estate/Interfaces';
import { ref } from 'vue';
import MapViewer from './MapViewer.vue';

defineProps<{
	points?: IMapPoint[];
	fitPoints?: boolean;
	initialState?: IMapState | null;
	highlightedPointId?: number | null;
	loading?: boolean;
}>();
const fullscreen = ref(false);

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
