<template>
	<div class="map-viewer-container">
		<v-progress-linear
			v-if="loading"
			class="spinner loader-lazy"
			color="primary"
			:height="2"
			indeterminate
		/>

		<div :id="mapId" class="map-viewer"></div>
		<map-controls
			v-model:full-screen="fullscreen"
			@zoom-in="smoothZoom(1)"
			@zoom-out="smoothZoom(-1)"
		/>

		<map-building-carousel
			v-model="selectedBuildingIds"
			v-model:active-building-id="activeBuildingId"
		/>
	</div>
</template>

<script setup lang="ts">
import { createEmpty, extend } from 'ol/extent';
import { EstateType } from '@/models/estate/Enums';
import { IMapPoint, IMapState } from '@/models/estate/Interfaces';
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import Feature, { FeatureLike } from 'ol/Feature';
import Map from 'ol/Map';
import MapBrowserEvent from 'ol/MapBrowserEvent';
import View from 'ol/View';
import MapBuildingCarousel from './MapBuildingCarousel.vue';
import { useMagicKeys, watchDebounced } from '@vueuse/core';
import { createWmsLayer, SWEREF992015, to3016 } from './layer';
import { createPointLayers } from './points';
import MapControls from './MapControls.vue';

const props = defineProps<{
	mapId: string;
	points?: IMapPoint[];
	fitPoints?: boolean;
	initialState?: IMapState | null;
	highlightedPointId?: number | null;
	fullscreen?: boolean;
	loading?: boolean;
}>();

const emit = defineEmits(['update:fullscreen']);

const fullscreen = computed({
	get: () => props.fullscreen ?? false,
	set: (value: boolean) => emit('update:fullscreen', value),
});

const {
	clusterLayer,
	FIT_POINTS_PADDING,
	highlightLayer,
	setHighlightedPoint,
	updatePoints,
	allFeaturesAtSameLocation,
} = createPointLayers();

// Umeå stadshuset
const DEFAULT_POSITION = {
	lon: 20.254137,
	lat: 63.82942,
	zoom: 7,
};

let map: Map | null = null;
const selectedBuildingIds = ref<number[] | null>(null);
const activeBuildingId = ref<number | null>(null);

watchDebounced(
	() => props.points,
	() => updatePoints(props.points ?? [], map, props.fitPoints),
	{ debounce: 100 }
);
watch(
	() => [props.highlightedPointId, activeBuildingId.value],
	([highlightedId, activeId]) => {
		setHighlightedPoint(activeId ?? highlightedId ?? null);
	}
);

/** Handle click event */
const buildingsClicked = (ids: number[]) => {
	selectedBuildingIds.value = ids;
};

const onClusterClick = (clustered: Feature[]) => {
	if (allFeaturesAtSameLocation(clustered)) {
		const buildingIds = clustered
			.filter((f) => f.get('type') === EstateType.Building)
			.map((f) => f.get('id'));
		buildingsClicked(buildingIds);
		return;
	}

	// Zoom to fit cluster
	if (clustered.length > 1) {
		const innerExtent = createEmpty();
		clustered.forEach((f) => {
			const geo = f.getGeometry();
			if (geo) {
				extend(innerExtent, geo.getExtent());
			}
		});
		map?.getView().fit(innerExtent, {
			padding: [
				FIT_POINTS_PADDING,
				FIT_POINTS_PADDING,
				FIT_POINTS_PADDING,
				FIT_POINTS_PADDING,
			],
			maxZoom: 14,
			duration: 300,
		});
	}
};

const onMapClick = (
	evt: MapBrowserEvent<KeyboardEvent | WheelEvent | PointerEvent>
) => {
	let hit: FeatureLike | undefined;

	map?.forEachFeatureAtPixel(
		evt.pixel,
		(f) => {
			hit = f;
		},
		{ layerFilter: (l) => l === clusterLayer, hitTolerance: 5 }
	);

	const clustered = hit?.get('features') as Feature[] | undefined;
	if (clustered?.length) {
		onClusterClick(clustered);
	}
};

let lastHit = false;
const onMapHover = (e: MapBrowserEvent) => {
	if (!map || e.dragging) {
		return;
	}

	const hit = map.hasFeatureAtPixel(e.pixel);

	if (hit !== lastHit) {
		const el = map.getTargetElement();
		if (!el) {
			return;
		}

		el.style.cursor = hit ? 'pointer' : '';
		lastHit = hit ?? false;
	}
};

const smoothZoom = (delta: number) => {
	if (!map) return;
	const view = map.getView();
	const current = view.getZoom() ?? 0;
	const target = view.getConstrainedZoom(current + delta);
	view.animate({
		zoom: target,
		duration: 250,
		easing: (t: number) => 1 - Math.pow(1 - t, 3), // easeOutCubic
	});
};

// Close overlays on Escape key
const { escape } = useMagicKeys();
watch(escape, () => {
	if (!escape.value) return;

	if (selectedBuildingIds.value !== null) {
		selectedBuildingIds.value = null;
		return;
	}
	if (fullscreen.value) {
		fullscreen.value = false;
	}
});

/** Initialize the map */
let initRenderComplete = false;
const onRenderComplete = () => {
	if (initRenderComplete) {
		return;
	}
	initRenderComplete = true;

	if (fullscreen.value) {
		// If first render is in fullscreen, fit points after initial render
		updatePoints(props.points ?? [], map, props.fitPoints);
	}
};

const initMap = () => {
	const initialState = props.initialState
		? props.initialState
		: DEFAULT_POSITION;

	const center = to3016([initialState.lon, initialState.lat]);

	map = new Map({
		target: props.mapId,
		controls: [],
		layers: [
			createWmsLayer('Lovisa', true),
			// createWmsLayer('Ortofoto', false), // TODO: Satellite toggle later?
			clusterLayer,
			highlightLayer, // Highlighted point is rendered on top of clusters
		],
		view: new View({
			projection: SWEREF992015,
			center,
			zoom: initialState.zoom,
			minZoom: 4,
			maxZoom: 16,
			enableRotation: false,
		}),
	});

	map.on('singleclick', onMapClick);
	map.on('pointermove', onMapHover);
	map.on('rendercomplete', onRenderComplete);

	updatePoints(props.points ?? [], map, props.fitPoints);

	setHighlightedPoint(props.highlightedPointId ?? null);
};

onMounted(initMap);

onUnmounted(() => {
	if (!map) return;

	map.un('singleclick', onMapClick);
	map.un('pointermove', onMapHover);
	map.un('rendercomplete', onRenderComplete);

	map.setTarget(undefined); // detach from DOM
	map = null;
});
</script>

<style scoped lang="scss">
.map-viewer-container {
	position: relative;
	height: 100%;
	width: 100%;

	.spinner {
		position: absolute;
		top: 0;
		left: 0;
		z-index: 100;
	}

	.map-viewer {
		height: 100%;
		width: 100%;
	}
}
</style>
