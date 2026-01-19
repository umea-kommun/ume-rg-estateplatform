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
			@room-selected="(roomId) => selectRoom(roomId, true)"
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
				@room-selected="(roomId) => selectRoom(roomId, true)"
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

const props = defineProps<{
	buildingId: number;
	floor: number | null;
}>();
const emit = defineEmits(['room-selected', 'update:floor']);

const store = useStore<IRootState>();

const selectedFloorId = ref<number | null>(null);
const floors = ref<IBuildingFloor[] | null>(null);
const blueprint = ref<string | null>(null);

const blueprintWrap = useTemplateRef('blueprintWrap');
const blueprintCameraPosition = ref<IBlueprintPosition | null>(null);
const fullScreen = ref(false);

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

const selectRoom = async (roomId: number | null, emitEvent = false) => {
	if (selectedRoomId.value === roomId) {
		return;
	}

	if (emitEvent) {
		emit('room-selected', roomId);
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
		const room = await store.dispatch(DispatchType.GetRoomById, {
			roomId,
		});

		if (room && room.floorId !== selectedFloorId.value) {
			await selectFloor(room.floorId);
		}
		selectedRoom.value = room;
		isBusyFetchingRoom.value = false;
	}
};

/* Fetch blueprint and floors */
const isBusyFetchingBlueprint = ref(false);
const fetchBlueprint = async (floorId: number) => {
	isBusyFetchingBlueprint.value = true;
	blueprintCameraPosition.value = null;
	try {
		blueprint.value = await store.dispatch(DispatchType.GetFloorBlueprint, {
			floorId,
		});
	} finally {
		isBusyFetchingBlueprint.value = false;
	}
};

const isBusyFetchingFloors = ref(false);
const fetchFloors = async (buildingId: number) => {
	isBusyFetchingFloors.value = true;
	floors.value = await store.dispatch(DispatchType.GetBuildingFloors, {
		buildingId,
		includeRooms: false,
	});
	if (floors.value?.length && selectedFloorId.value === null) {
		await selectFloor(floors.value[0].id);
	}
	isBusyFetchingFloors.value = false;
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
	selectRoom,
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
