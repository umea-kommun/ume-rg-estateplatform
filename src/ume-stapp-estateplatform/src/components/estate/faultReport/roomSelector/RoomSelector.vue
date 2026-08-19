<template>
	<div class="room-selector">
		<div v-if="selectedRoom || skippedRoom">
			<div v-if="skippedRoom">
				<p class="text-medium-emphasis">
					{{ $t('component.internal.roomSelector.none') }}
				</p>
			</div>
			<div
				v-else-if="selectedRoom"
				class="selected-room-wrap elevation-1 mt-1 rounded-lg"
			>
				<building-blueprint
					v-if="building.blueprintAvailable"
					ref="buildingBlueprintRef"
					class="selected-room-blueprint"
					:building="building"
					:floor="selectedRoom.floorId"
					:room-zoom-padding="0.5"
					hide-controls
				/>
				<room-card :room="selectedRoom" />
			</div>
		</div>
		<div v-else>
			<p class="text-medium-emphasis">
				{{ $t('component.internal.roomSelector.description') }}
			</p>
			<building-rooms
				:building-id="building.id"
				v-model:floor="floorId"
				:return-object="true"
				class="px-0"
				@room-selected="selectRoom"
				no-padding
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { IBuildingDetails, IBuildingRoom } from '@/models/Interfaces';
import BuildingRooms from '../../building/BuildingRooms.vue';
import { ref, useTemplateRef, watch } from 'vue';
import RoomCard from '../../building/RoomCard.vue';
import BuildingBlueprint from '../../blueprint/BuildingBlueprint.vue';

const props = defineProps<{
	building: IBuildingDetails;
	selectedRoom: IBuildingRoom | null;
	skippedRoom?: boolean;
}>();
const emit = defineEmits(['select']);

const selectRoom = (room: IBuildingRoom | null) => {
	emit('select', room);
};

const floorId = ref<number | null>(null);

const buildingBlueprintRef = useTemplateRef('buildingBlueprintRef');

watch(
	[() => props.selectedRoom, () => buildingBlueprintRef.value],
	([room, blueprint]) => {
		if (room && blueprint) {
			blueprint.openRoom(room.id);
		}
	},
	{ immediate: true, flush: 'post' }
);
</script>

<style scoped lang="scss">
.room-selector {
	:deep(.v-list) {
		overflow: visible;
	}
	:deep(.room-card) {
		margin-bottom: 8px;
		padding: 16px;

		box-shadow:
			0px 2px 1px -1px rgba(0, 0, 0, 0.2),
			0px 1px 1px 0px rgba(0, 0, 0, 0.14),
			0px 1px 3px 0px rgba(0, 0, 0, 0.12);

		border-radius: $border-radius !important;
		hr {
			display: none;
		}
	}

	.selected-room-wrap {
		overflow: hidden;

		:deep(.room-card) {
			margin-bottom: 0;
		}
		.selected-room-blueprint {
			border: solid 1px rgba(0, 0, 0, 0.1);
			border-radius: $border-radius $border-radius 0 0;
			overflow: hidden !important;
			pointer-events: none !important;
			height: 150px;

			* {
				pointer-events: none;
			}
		}
	}
}
</style>
