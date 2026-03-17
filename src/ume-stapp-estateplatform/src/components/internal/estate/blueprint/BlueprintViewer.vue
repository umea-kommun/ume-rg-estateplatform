<template>
	<div class="blueprint-viewer">
		<div v-if="loading" class="loader-lazy d-flex align-center h-100">
			<app-loading-spinner :is-visible="true" />
		</div>
		<blueprint-map
			v-if="blueprint && !loading"
			ref="blueprint-map"
			:blueprintSvg="blueprint"
			:selectedRoomId="selectedRoom?.id"
			:start-position="startPosition"
			:room-zoom-padding="roomZoomPadding"
			@room-clicked="(roomId) => emit('room-opened', roomId)"
			@camera-moved="(a) => (startPosition = a)"
		/>
		<blueprint-controls
			v-if="!hideControls"
			v-model:fullScreen="fullScreen"
			v-model:selectedFloorId="selectedFloorId"
			:floors="floors"
			@zoom-in="blueprintMap?.zoomIn()"
			@zoom-out="blueprintMap?.zoomOut()"
			:zoom-in-disabled="blueprintMap?.zoomInDisabled"
			:zoom-out-disabled="blueprintMap?.zoomOutDisabled"
		/>
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
} from '@/models/estate/Interfaces';
import BlueprintControls from '@/components/internal/estate/blueprint/BlueprintControls.vue';
import BlueprintRoomCard from '@/components/internal/estate/blueprint/BlueprintRoomCard.vue';
import BlueprintMap from '@/components/internal/estate/blueprint/BlueprintMap.vue';
import { computed, onBeforeUnmount, useTemplateRef, watch } from 'vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';

const props = defineProps<{
	blueprint: string | null;
	loading: boolean;
	floors: IBuildingFloor[];
	startPosition?: IBlueprintPosition | null;
	selectedRoom: IBuildingRoom | null;
	selectedFloorId: number | null;
	fullScreen: boolean;
	hideControls?: boolean;
	roomZoomPadding?: number;
	selectable?: boolean;
}>();

const emit = defineEmits<{
	(e: 'room-opened', id: number | null): void;
	(e: 'room-selected', room: IBuildingRoom): void;
	(e: 'update:full-screen', value: boolean): void;
	(e: 'update:selected-floor-id', value: number | null): void;
	(e: 'update:start-position', value: IBlueprintPosition | null): void;
}>();

const blueprintMap = useTemplateRef('blueprint-map');

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

	background-color: $estate-blueprint-background;
}
</style>
