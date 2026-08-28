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
		<div ref="popupEl" class="ol-popup">
			<map-building-carousel
				v-model="selectedBuildingIds"
				v-model:active-building-id="activeBuildingId"
				:selectable="selectable"
				@select="(buildingId) => emit('select-building', buildingId)"
			/>
		</div>
		<map-controls
			v-if="!hideControls"
			v-model:full-screen="fullscreen"
			v-model:base-layer="visibleBaseLayer"
			@zoom-in="smoothZoom(map, 1)"
			@zoom-out="smoothZoom(map, -1)"
		/>
	</div>
</template>

<script setup lang="ts">
import { createEmpty, extend } from 'ol/extent';
import { EstateType, MapBaseLayer } from '@/models/Enums';
import { IMapPoint, IMapState } from '@/models/Interfaces';
import {
	computed,
	onMounted,
	onUnmounted,
	ref,
	useTemplateRef,
	watch,
} from 'vue';
import Feature, { FeatureLike } from 'ol/Feature';
import Map from 'ol/Map';
import MapBrowserEvent from 'ol/MapBrowserEvent';
import View from 'ol/View';
import MouseWheelZoom from 'ol/interaction/MouseWheelZoom';
import { defaults as defaultInteractions } from 'ol/interaction/defaults';
import MapBuildingCarousel from './MapBuildingCarousel.vue';
import { useMagicKeys, useStorage, watchDebounced } from '@vueuse/core';
import { createWmsLayer, SWEREF992015, to3016 } from './layer';
import { createPointLayers } from './points';
import MapControls from './MapControls.vue';
import Point from 'ol/geom/Point';
import Overlay from 'ol/Overlay';
import { panToWithPixelOffset, smoothZoom } from './pan';
import { TileWMS } from 'ol/source';
import TileLayer from 'ol/layer/Tile';
import { appInsights } from '@/plugins/appInsights';

const props = defineProps<{
	mapId: string;
	points?: IMapPoint[];
	fitPoints?: boolean;
	initialState?: IMapState | null;
	highlightedPointId?: number | null;
	fullscreen?: boolean;
	loading?: boolean;
	hideControls?: boolean;
	selectable?: boolean;
}>();

const emit = defineEmits(['update:fullscreen', 'select-building']);

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

const popupEl = useTemplateRef('popupEl');
let popupOverlay: Overlay | null = null;

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

/** Overlay events */
function showPopupForFeature(f: Feature, buildingIds: number[]) {
	const geo = f.getGeometry();
	if (!geo || geo.getType() !== 'Point') return;

	const coord = (geo as Point).getCoordinates();
	selectedBuildingIds.value = buildingIds;
	popupOverlay?.setPosition(coord);
}

function closePopup() {
	selectedBuildingIds.value = null;
	popupOverlay?.setPosition(undefined);
}
watch(selectedBuildingIds, (ids) => {
	if (!ids) {
		closePopup();
	}
});

/** Handle click event */
const buildingsClicked = (ids: number[]) => {
	selectedBuildingIds.value = ids;

	appInsights?.trackEvent({
		name: 'EstateMapBuildingClicked',
		properties: {
			buildingIds: ids,
			url: window.location.href,
		},
	});
};

const onClusterClick = (clustered: Feature[]) => {
	if (allFeaturesAtSameLocation(clustered)) {
		const buildingIds = clustered
			.filter((f) => f.get('type') === EstateType.Building)
			.sort(
				(a, b) => (b.get('grossArea') ?? 0) - (a.get('grossArea') ?? 0)
			)
			.map((f) => f.get('id'));
		buildingsClicked(buildingIds);

		// Center clicked feature(s) (keep current zoom)
		const geo = clustered[0]?.getGeometry();
		if (geo && map) {
			panToWithPixelOffset(
				map,
				(geo as Point).getCoordinates(),
				[0, 150]
			);
		}
		showPopupForFeature(clustered[0], buildingIds);
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

		appInsights?.trackEvent({
			name: 'EstateMapZoomClusterClicked',
			properties: {
				url: window.location.href,
			},
		});
	}
};

const onMapClick = (
	evt: MapBrowserEvent<KeyboardEvent | WheelEvent | PointerEvent>
) => {
	let hit: FeatureLike | undefined;
	closePopup();

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
let panning = false;

const applyCursor = () => {
	const el = map?.getTargetElement();
	if (!el) {
		return;
	}

	el.style.cursor = panning ? 'grabbing' : lastHit ? 'pointer' : '';
};

const onMapHover = (e: MapBrowserEvent) => {
	if (!map || e.dragging) {
		return;
	}

	const hit = map.hasFeatureAtPixel(e.pixel);

	if (hit !== lastHit) {
		lastHit = hit;
		applyCursor();
	}
};

const onMapDrag = () => {
	if (panning) {
		return;
	}

	panning = true;
	applyCursor();
};

const onPointerUp = (e: PointerEvent) => {
	if (!panning) {
		return;
	}

	panning = false;
	lastHit = map ? map.hasFeatureAtPixel(map.getEventPixel(e)) : false;
	applyCursor();
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

/** Map base layers */
const mapBaseLayers: Partial<Record<MapBaseLayer, TileLayer<TileWMS>>> = {};
const visibleBaseLayer = useStorage<MapBaseLayer>(
	'map-selected-base-layer',
	MapBaseLayer.Lovisa,
	sessionStorage
);

function applyVisibleBaseLayer(selected: MapBaseLayer) {
	for (const layerName of Object.keys(mapBaseLayers) as MapBaseLayer[]) {
		mapBaseLayers[layerName]?.setVisible(layerName === selected);
	}
}
watch(visibleBaseLayer, applyVisibleBaseLayer);

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

const initBaseLayers = (): TileLayer<TileWMS>[] => {
	mapBaseLayers[MapBaseLayer.Lovisa] = createWmsLayer(
		MapBaseLayer.Lovisa,
		'#d6e1d9'
	);
	mapBaseLayers[MapBaseLayer.Ortofoto] = createWmsLayer(
		MapBaseLayer.Ortofoto,
		'#08091a'
	);

	applyVisibleBaseLayer(visibleBaseLayer.value);

	return Object.values(mapBaseLayers);
};

const initInteractions = () => {
	const mouseWheelZoom = new MouseWheelZoom();
	(
		mouseWheelZoom as unknown as {
			deltaPerZoom_: number;
		}
	).deltaPerZoom_ = 40; // Lower value makes touchpad zoom more responsive.

	return defaultInteractions({
		mouseWheelZoom: false,
	}).extend([mouseWheelZoom]);
};

const initMap = () => {
	const initialState = props.initialState
		? props.initialState
		: DEFAULT_POSITION;

	const center = to3016([initialState.lon, initialState.lat]);

	map = new Map({
		target: props.mapId,
		controls: [],
		interactions: initInteractions(),
		layers: [
			...initBaseLayers(),
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

	if (popupEl.value) {
		popupOverlay = new Overlay({
			element: popupEl.value,
			positioning: 'bottom-center',
			offset: [0, -24],
			stopEvent: true,
			autoPan: false,
		});

		map.addOverlay(popupOverlay);
	}

	map.on('singleclick', onMapClick);
	map.on('pointermove', onMapHover);
	map.on('pointerdrag', onMapDrag);
	map.on('rendercomplete', onRenderComplete);

	window.addEventListener('pointerup', onPointerUp);
	window.addEventListener('pointercancel', onPointerUp);

	updatePoints(props.points ?? [], map, props.fitPoints);

	setHighlightedPoint(props.highlightedPointId ?? null);

	if (props.fullscreen) {
		appInsights?.trackEvent({
			name: 'EstateMapFullscreen',
			properties: {
				url: window.location.href,
			},
		});
	}
};

onMounted(initMap);

onUnmounted(() => {
	window.removeEventListener('pointerup', onPointerUp);
	window.removeEventListener('pointercancel', onPointerUp);

	if (!map) return;

	map.un('singleclick', onMapClick);
	map.un('pointermove', onMapHover);
	map.un('pointerdrag', onMapDrag);
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
		cursor: grab;
	}

	:deep(.ol-overlay-container) {
		pointer-events: none !important;
		display: flex;
		max-width: 100%;
		justify-content: center;
	}
	.ol-popup {
		max-width: 80%;
		width: 440px;
		cursor: auto;
	}
}
</style>
