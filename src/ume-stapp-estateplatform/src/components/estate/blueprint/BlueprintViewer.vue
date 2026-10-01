<template>
	<div class="blueprint-viewer">
		<h1 class="print-title">{{ printTitle }}</h1>
		<div v-if="loading" class="loader-lazy d-flex align-center h-100">
			<app-loading-spinner :is-visible="true" />
		</div>
		<v-fade-transition hide-on-leave>
			<blueprint-map
				v-if="blueprint && !loading"
				ref="blueprint-map"
				:blueprintSvg="blueprint"
				:selectedRoomId="selectedRoom?.id"
				:start-position="startPosition"
				:room-zoom-padding="roomZoomPadding"
				@room-clicked="(roomId) => emit('room-opened', roomId)"
				@camera-moved="(a) => (startPosition = a)"
				@user-interacted="logUserInteractionOnce"
			/>
		</v-fade-transition>
		<v-fade-transition>
			<blueprint-controls
				v-if="!hideControls && !floorsLoading"
				v-model:fullScreen="fullScreen"
				v-model:selectedFloorId="selectedFloorId"
				:floors="floors"
				:closable="closable"
				:can-leave-fullscreen="canLeaveFullscreen"
				@print="emit('print')"
				@zoom-in="blueprintMapRef?.zoomIn()"
				@zoom-out="blueprintMapRef?.zoomOut()"
				@close="emit('close')"
				:zoom-in-disabled="blueprintMapRef?.zoomInDisabled"
				:zoom-out-disabled="blueprintMapRef?.zoomOutDisabled"
			/>
		</v-fade-transition>
		<blueprint-room-card
			v-if="selectedRoom && !hideControls"
			:room="selectedRoom"
			:selectable="selectable"
			@close="emit('room-opened', null)"
			@select="(room) => emit('room-selected', room)"
		/>
	</div>
</template>

<script setup lang="ts">
import {
	IBlueprintPosition,
	IBuildingFloor,
	IBuildingRoom,
} from '@/models/Interfaces';
import BlueprintControls from '@/components/estate/blueprint/BlueprintControls.vue';
import BlueprintRoomCard from '@/components/estate/blueprint/BlueprintRoomCard.vue';
import BlueprintMap from '@/components/estate/blueprint/BlueprintMap.vue';
import {
	computed,
	onBeforeUnmount,
	onMounted,
	useTemplateRef,
	watch,
} from 'vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { appInsights } from '@/plugins/appInsights';

const props = defineProps<{
	blueprint: string | null;
	loading: boolean;
	floors: IBuildingFloor[];
	floorsLoading?: boolean;
	startPosition?: IBlueprintPosition | null;
	selectedRoom: IBuildingRoom | null;
	selectedFloorId: number | null;
	fullScreen: boolean;
	hideControls?: boolean;
	roomZoomPadding?: number;
	selectable?: boolean;
	printTitle?: string;
	closable?: boolean;
	canLeaveFullscreen?: boolean;
}>();

const emit = defineEmits<{
	(e: 'room-opened', id: number | null): void;
	(e: 'room-selected', room: IBuildingRoom): void;
	(e: 'update:full-screen', value: boolean): void;
	(e: 'update:selected-floor-id', value: number | null): void;
	(e: 'update:start-position', value: IBlueprintPosition | null): void;
	(e: 'print'): void;
	(e: 'close'): void;
}>();

const blueprintMapRef = useTemplateRef('blueprint-map');

const fullScreen = computed({
	get: () => props.fullScreen,
	set: (value) => emit('update:full-screen', value),
});

const selectedFloorId = computed({
	get: () => props.selectedFloorId,
	set: (value) => emit('update:selected-floor-id', value),
});

const startPosition = computed({
	get: () => props.startPosition || null,
	set: (value) => emit('update:start-position', value),
});

// Disable default page zoom while in fullscreen mode
function lockViewportZoom() {
	let tag = document.querySelector<HTMLMetaElement>('meta[name="viewport"]');
	const original = tag?.getAttribute('content') ?? '';
	if (!tag) {
		tag = document.createElement('meta');
		tag.name = 'viewport';
		document.head.appendChild(tag);
	}

	tag.setAttribute(
		'content',
		'width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover'
	);
	return () => {
		tag?.setAttribute(
			'content',
			original || 'width=device-width, initial-scale=1.0'
		);
	};
}

let unlockViewportZoom: (() => void) | null = null;

watch(
	() => fullScreen.value,
	(isFull) => {
		if (isFull) {
			unlockViewportZoom = lockViewportZoom();
		} else {
			unlockViewportZoom?.();
			unlockViewportZoom = null;
		}
	},
	{
		immediate: true,
	}
);

let hasLoggedInteraction = false;
const logUserInteractionOnce = () => {
	if (hasLoggedInteraction) {
		return;
	}

	appInsights?.trackEvent({
		name: 'EstateBlueprintInteraction',
		properties: {
			url: window.location.href,
		},
	});
	hasLoggedInteraction = true;
};

const trackFullscreenOpened = () => {
	appInsights?.trackEvent({
		name: 'EstateBlueprintFullscreen',
		properties: {
			url: window.location.href,
		},
	});
};

// Track entering fullscreen after mount too, e.g. when a windowed dialog is
// expanded (a viewer that mounts already fullscreen is tracked in onMounted).
watch(
	() => props.fullScreen,
	(value) => {
		if (value) {
			trackFullscreenOpened();
		}
	}
);

onMounted(() => {
	if (props.fullScreen) {
		trackFullscreenOpened();
	}
});

onBeforeUnmount(() => {
	unlockViewportZoom?.();
});
</script>

<style lang="scss" scoped>
.blueprint-viewer {
	height: 100%;
	width: 100%;
	position: relative;
	overflow: hidden;
	// Queried by the controls and the room card, which size against the pane
	container: blueprint / inline-size;

	background-color: $estate-blueprint-background;

	.print-title {
		display: none;

		@media print {
			display: block;
		}
	}
}
</style>
