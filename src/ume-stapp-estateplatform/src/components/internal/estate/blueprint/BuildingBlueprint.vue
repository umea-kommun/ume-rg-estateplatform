<template>
	<div class="building-blueprint-wrap" ref="blueprintWrap">
		<blueprint-viewer
			v-if="!fullScreen"
			:blueprint="blueprint"
			:loading="isBusyFetchingBlueprint"
			class="default-viewer"
			:floors="floors ?? []"
			v-model:full-screen="fullScreen"
			:selected-floor-id="selectedFloorId"
			@update:selected-floor-id="selectFloor"
			v-model:start-position="blueprintCameraPosition"
			:selected-room="selectedRoom"
			@room-opened="(roomId) => openRoom(roomId, true)"
			@room-selected="(room) => emit('room-selected', room)"
			:hide-controls="hideControls"
			:room-zoom-padding="roomZoomPadding"
			:selectable="selectable"
		/>
		<v-dialog
			v-model="fullScreen"
			fullscreen
			hide-overlay
			class="building-blueprint-dialog"
		>
			<blueprint-viewer
				v-if="fullScreen"
				:blueprint="blueprint"
				:loading="isBusyFetchingBlueprint"
				:floors="floors ?? []"
				v-model:full-screen="fullScreen"
				:selected-floor-id="selectedFloorId"
				@update:selected-floor-id="selectFloor"
				v-model:start-position="blueprintCameraPosition"
				:selected-room="selectedRoom"
				@room-opened="(roomId) => openRoom(roomId, true)"
				@room-selected="(room) => emit('room-selected', room)"
				:room-zoom-padding="roomZoomPadding"
				:selectable="selectable"
			/>
		</v-dialog>
	</div>
</template>

<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import {
	IBlueprintPosition,
	IBuildingFloor,
	IBuildingRoom,
} from '@/models/estate/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { computed, ref, useTemplateRef, watch } from 'vue';
import { useStore } from 'vuex';
import BlueprintViewer from './BlueprintViewer.vue';
import ErrorService from '@/utils/ErrorService';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	buildingId: number;
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
const fullScreen = ref(false);
watch(fullScreen, (newVal) => {
	if (!newVal) {
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
		if (blueprintWrap.value?.offsetParent === null && !fullScreen.value) {
			fullScreen.value = true;
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
			await selectFloor(floors.value[0].id);
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
	() => props.buildingId,
	(newId) => {
		fetchFloors(newId);
	},
	{ immediate: true }
);

watch(
	() => parentSelectedFloorId.value,
	(newFloorId) => {
		selectFloor(newFloorId);
	}
);

const openFullscreen = () => {
	fullScreen.value = true;
};

defineExpose({
	openRoom,
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
</style>
