<template>
	<div class="building-blueprint-wrap" ref="blueprintWrap">
		<blueprint-viewer
			v-if="!dialogOpen"
			:blueprint="blueprint"
			:loading="isBusyFetchingBlueprint"
			class="default-viewer"
			:floors="floors ?? []"
			:floors-loading="isBusyFetchingFloors"
			:full-screen="false"
			:selected-floor-id="selectedFloorId"
			@update:selected-floor-id="selectFloor"
			@update:full-screen="(value) => value && openFullscreen()"
			v-model:start-position="blueprintCameraPosition"
			:selected-room="selectedRoom"
			@room-opened="(roomId) => openRoom(roomId, true)"
			@room-selected="(room) => emit('room-selected', room)"
			@print="print"
			:hide-controls="hideControls"
			:room-zoom-padding="roomZoomPadding"
			:selectable="selectable"
			:print-title="printTitle"
		/>
		<v-dialog
			v-model="dialogOpen"
			:fullscreen="fullscreen"
			:max-width="fullscreen ? undefined : 1200"
			:class="[
				'building-blueprint-dialog',
				{ 'building-blueprint-dialog--windowed': !fullscreen },
			]"
		>
			<div class="building-blueprint-dialog__content">
				<blueprint-viewer
					:blueprint="blueprint"
					:loading="isBusyFetchingBlueprint"
					:floors="floors ?? []"
					:floors-loading="isBusyFetchingFloors"
					v-model:full-screen="viewerFullscreen"
					:selected-floor-id="selectedFloorId"
					@update:selected-floor-id="selectFloor"
					v-model:start-position="blueprintCameraPosition"
					:selected-room="selectedRoom"
					@room-opened="(roomId) => openRoom(roomId, true)"
					@room-selected="(room) => emit('room-selected', room)"
					@print="print"
					:room-zoom-padding="roomZoomPadding"
					:selectable="selectable"
					:print-title="printTitle"
					:closable="true"
					:can-leave-fullscreen="openedWindowed"
					@close="dialogOpen = false"
				/>
			</div>
		</v-dialog>
	</div>
</template>

<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import {
	IBlueprintPosition,
	IBuildingDetails,
	IBuildingFloor,
	IBuildingRoom,
} from '@/models/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { computed, ref, useTemplateRef, watch } from 'vue';
import { useStore } from 'vuex';
import BlueprintViewer from './BlueprintViewer.vue';
import ErrorService from '@/utils/ErrorService';
import { pickDefaultFloor } from '../defaultFloor';
import { useI18n } from 'vue-i18n';
import { useEstateIsMobile } from '../useEstateIsMobile';
import { useDialogHistory } from '@/utils/useDialogHistory';

const props = defineProps<{
	building: IBuildingDetails;
	floor: number | null;
	hideControls?: boolean;
	roomZoomPadding?: number;
	selectable?: boolean;
}>();
const emit = defineEmits([
	'room-opened',
	'room-selected',
	'update:floor',
	'fullscreen-closed',
]);

const store = useStore<IRootState>();
const { t } = useI18n();

const selectedFloorId = ref<number | null>(null);
const floors = ref<IBuildingFloor[] | null>(null);
const blueprint = ref<string | null>(null);

const blueprintWrap = useTemplateRef('blueprintWrap');
const blueprintCameraPosition = ref<IBlueprintPosition | null>(null);

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

// The BlueprintControls fullscreen button toggles between the window and
// fullscreen. Closing is a separate action (the close button / Escape), so
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

const selectedRoomId = ref<number | null>(null);
const selectedRoom = ref<IBuildingRoom | null>(null);
const isBusyFetchingRoom = ref(false);

const parentSelectedFloorId = computed<number | null>({
	get: () => props.floor,
	set: (value: number | null) => {
		emit('update:floor', value);
	},
});

const selectFloor = async (floorId: number | null) => {
	if (floorId === null || floorId === selectedFloorId.value) {
		return;
	}

	selectedFloorId.value = floorId;
	parentSelectedFloorId.value = floorId;
	selectedRoom.value = null;

	// eslint-disable-next-line @typescript-eslint/no-use-before-define
	await fetchBlueprint(floorId);
};

const openRoom = async (roomId: number | null, emitEvent = false) => {
	if (selectedRoomId.value === roomId) {
		return;
	}

	if (emitEvent) {
		emit('room-opened', roomId);
	}
	selectedRoomId.value = roomId;

	if (roomId === null) {
		selectedRoom.value = null;
	} else {
		// If in blueprint is not visible, open fullscreen
		if (blueprintWrap.value?.offsetParent === null && !dialogOpen.value) {
			// eslint-disable-next-line @typescript-eslint/no-use-before-define
			openFullscreen();
		}

		isBusyFetchingRoom.value = true;
		try {
			const room = await store.dispatch(DispatchType.GetRoomById, {
				roomId,
			});

			if (room && room.floorId !== selectedFloorId.value) {
				await selectFloor(room.floorId);
			}
			selectedRoom.value = room;
		} catch (err) {
			ErrorService.onError({
				err,
				message: t('app.error.estate.unableToFetchRoom'),
			});
		} finally {
			isBusyFetchingRoom.value = false;
		}
	}
};

/* Fetch blueprint and floors */
let abortController: AbortController | null = null;
const isBusyFetchingBlueprint = ref(false);
const fetchBlueprint = async (floorId: number) => {
	if (abortController) {
		abortController.abort();
		await new Promise((r) => requestAnimationFrame(r)); // Wait for the abort to propagate and avoid race conditions
	}
	abortController = new AbortController();
	isBusyFetchingBlueprint.value = true;
	blueprintCameraPosition.value = null;
	try {
		blueprint.value = await store.dispatch(DispatchType.GetFloorBlueprint, {
			floorId,
			abortController,
		});
	} catch (err) {
		if (abortController.signal.aborted) {
			// Fetch was aborted, do not show an error
			return;
		}

		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchBlueprint'),
		});
	} finally {
		isBusyFetchingBlueprint.value = false;
	}
};

const isBusyFetchingFloors = ref(false);
const fetchFloors = async (buildingId: number) => {
	isBusyFetchingFloors.value = true;
	try {
		floors.value = await store.dispatch(DispatchType.GetBuildingFloors, {
			buildingId,
			includeRooms: false,
		});
		if (floors.value?.length && selectedFloorId.value === null) {
			const defaultFloor = pickDefaultFloor(floors.value);

			if (defaultFloor) {
				await selectFloor(defaultFloor.id);
			}
		}
	} catch (err) {
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchFloors'),
		});
	} finally {
		isBusyFetchingFloors.value = false;
	}
};

watch(
	() => props.building,
	(newBuilding) => {
		if (newBuilding) {
			fetchFloors(newBuilding.id);
		}
	},
	{ immediate: true }
);

watch(
	() => parentSelectedFloorId.value,
	(newFloorId) => {
		selectFloor(newFloorId);
	}
);

const print = async () => {
	document.body.classList.add('printing-blueprint');

	const cleanup = () => {
		document.body.classList.remove('printing-blueprint');
		window.removeEventListener('afterprint', cleanup);
	};

	window.addEventListener('afterprint', cleanup, { once: true });
	window.print();
};

const printTitle = computed(() => {
	const buildingName = props.building.popularName || props.building.name;
	const floorName = floors.value?.find((f) => f.id === selectedFloorId.value)
		?.name;

	return `${buildingName} - ${t(
		'component.blueprintMap.floor'
	)} ${floorName}`;
});

const openFullscreen = () => {
	openedWindowed.value = false;
	fullscreen.value = true;
	dialogOpen.value = true;
};

const open = () => {
	openedWindowed.value = !isMobile.value;
	fullscreen.value = isMobile.value;
	dialogOpen.value = true;
};

defineExpose({
	openRoom,
	open,
	openFullscreen,
});
</script>

<style lang="scss" scoped>
.building-blueprint-wrap {
	height: 100%;
	position: relative;
	overscroll-behavior: contain;
	touch-action: none;

	.default-viewer {
		position: relative;
		z-index: 2;
		box-shadow: inset 3px 3px 5px -2px rgba(0, 0, 0, 0.1);

		:deep(svg) {
			z-index: 1;
		}
	}
}

.building-blueprint-dialog {
	.building-blueprint-dialog__content {
		height: 100%;
		width: 100%;
	}

	&--windowed .building-blueprint-dialog__content {
		// Cap the height on large screens so it stays roughly 16:9 with the
		// 1200px max-width, while still shrinking to fit shorter viewports.
		height: min(80vh, 720px);
		border: solid 4px $white;
		border-radius: $border-radius;
		overflow: hidden;
	}
}
</style>

<style lang="scss">
@media print {
	@page {
		margin: 0;
		size: auto;
	}
	body.printing-blueprint {
		margin: 0 !important;
		padding: 0 !important;
		height: 100vh;
		overflow: hidden !important;

		* {
			visibility: hidden;
		}

		.estate-default .container .map,
		.building-blueprint-dialog {
			visibility: visible;
			position: fixed !important;
			inset: 0 !important;
			overflow: hidden;

			.blueprint-viewer {
				padding: 36px;
			}

			* {
				visibility: visible;
			}
		}

		// A windowed dialog centres its content in a capped, bordered box. For
		// printing, force it to fill the sheet from the top-left like the
		// fullscreen dialog does, so the floor plan (and its title) aren't
		// shrunk or pushed down the page.
		.building-blueprint-dialog .v-overlay__content {
			position: fixed !important;
			inset: 0 !important;
			width: 100% !important;
			height: 100% !important;
			max-width: none !important;
			max-height: none !important;
			margin: 0 !important;
			transform: none !important;
		}

		.building-blueprint-dialog__content {
			height: 100% !important;
			border: none !important;
		}
	}
}
</style>
