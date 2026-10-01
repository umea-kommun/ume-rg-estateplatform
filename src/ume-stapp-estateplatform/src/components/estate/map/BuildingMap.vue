<template>
	<div class="building-map">
		<map-viewer
			v-if="!dialogOpen"
			map-id="regular-map"
			:fullscreen="false"
			:points="points"
			:highlighted-point-id="highlightedPointId"
			:fit-points="fitPoints"
			:loading="loading"
			:hide-controls="hideControls"
			:selectable="selectable"
			@update:fullscreen="(value) => value && openFullscreen()"
			@select-building="
				(buildingId) => emit('select-building', buildingId)
			"
		/>

		<v-dialog
			v-model="dialogOpen"
			:fullscreen="fullscreen"
			:max-width="fullscreen ? undefined : 1200"
			:class="[
				'building-map-dialog',
				{ 'building-map-dialog--windowed': !fullscreen },
			]"
		>
			<div class="building-map-dialog__content">
				<map-viewer
					map-id="dialog-map"
					v-model:fullscreen="viewerFullscreen"
					:points="points"
					:highlighted-point-id="highlightedPointId"
					:fit-points="fitPoints"
					:loading="loading"
					:selectable="selectable"
					:closable="true"
					:can-leave-fullscreen="openedWindowed"
					@select-building="
						(buildingId) => emit('select-building', buildingId)
					"
					@close="dialogOpen = false"
				/>
			</div>
		</v-dialog>
	</div>
</template>

<script setup lang="ts">
import { IMapPoint, IMapState } from '@/models/Interfaces';
import { computed, ref, watch } from 'vue';
import MapViewer from './MapViewer.vue';
import { useEstateIsMobile } from '../useEstateIsMobile';
import { useDialogHistory } from '@/utils/useDialogHistory';

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

const isMobile = useEstateIsMobile();

// The dialog can be shown either fullscreen or as a centered window. On mobile
// a window would be too cramped, so `open()` falls back to fullscreen there.
const dialogOpen = ref(false);
const fullscreen = ref(false);
// Whether the user actually saw the windowed dialog before going fullscreen.
// Only then do we offer a "leave fullscreen" button that returns to the window;
// dialogs opened straight to fullscreen have no window to return to.
const openedWindowed = ref(false);

useDialogHistory(dialogOpen);

// The MapControls fullscreen button toggles between the window and fullscreen.
// Closing is a separate action (the close button / Escape emit `close`), so
// leaving fullscreen returns to the window instead of closing the dialog.
const viewerFullscreen = computed({
	get: () => fullscreen.value,
	set: (value: boolean) => {
		fullscreen.value = value;
	},
});

watch(dialogOpen, (open) => {
	if (!open) {
		emit('fullscreen-closed');
	}
});

const openFullscreen = () => {
	openedWindowed.value = false;
	fullscreen.value = true;
	dialogOpen.value = true;
};

const open = () => {
	// Desktop opens as a window the user can then expand; mobile needs the full
	// screen to be usable, so it skips the window (no leave-fullscreen button).
	openedWindowed.value = !isMobile.value;
	fullscreen.value = isMobile.value;
	dialogOpen.value = true;
};

defineExpose({
	open,
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
	.building-map-dialog__content {
		height: 100%;
		width: 100%;
	}

	&--windowed .building-map-dialog__content {
		// Cap the height on large screens so it stays roughly 16:9 with the
		// 1200px max-width, while still shrinking to fit shorter viewports.
		height: min(80vh, 800px);
		border: solid 4px $white;
		border-radius: $border-radius;
		overflow: hidden;
	}
}
</style>
