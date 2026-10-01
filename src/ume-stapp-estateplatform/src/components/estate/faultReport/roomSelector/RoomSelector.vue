<template>
	<div class="room-selector">
		<div v-if="selectedRoom || skippedRoom">
			<selection-card
				v-if="skippedRoom"
				class="mt-1"
				muted
				icon="no_meeting_room"
				:title="$t('component.roomSelector.noneTitle')"
				:description="$t('component.roomSelector.none')"
			>
				<template #actions>
					<v-btn
						rounded="lg"
						variant="outlined"
						color="grey-darken-2"
						@click="selectRoom(null)"
					>
						{{ $t('component.faultReport.changeAnswer') }}
					</v-btn>
				</template>
			</selection-card>
			<div v-else-if="selectedRoom" class="selected-room mt-1">
				<selection-card
					:title="selectedRoomTitle"
					:description="
						$t('component.roomSelector.floorArea', {
							floor: selectedRoom.floorName,
							area: selectedRoom.grossArea,
						})
					"
				>
					<template #actions>
						<v-btn
							v-if="building.blueprintAvailable"
							variant="text"
							size="small"
							rounded="lg"
							color="primary"
							:prepend-icon="
								showBlueprint ? 'expand_less' : 'map'
							"
							@click="showBlueprint = !showBlueprint"
						>
							{{
								showBlueprint
									? $t('component.roomSelector.hideBlueprint')
									: $t('component.roomSelector.showBlueprint')
							}}
						</v-btn>
						<v-btn
							rounded="lg"
							variant="outlined"
							color="grey-darken-2"
							@click="selectRoom(null)"
						>
							{{ $t('component.faultReport.changeAnswer') }}
						</v-btn>
					</template>
				</selection-card>

				<building-blueprint
					v-if="building.blueprintAvailable && showBlueprint"
					ref="buildingBlueprintRef"
					class="selected-room-blueprint mt-2"
					:building="building"
					:floor="selectedRoom.floorId"
					:room-zoom-padding="0.5"
					hide-controls
				/>
			</div>
		</div>
		<fault-room-picker
			v-else
			:building="building"
			:skippable="skippable"
			@select="selectRoom"
			@skip="emit('skip')"
		/>
	</div>
</template>

<script setup lang="ts">
import { IBuildingDetails, IBuildingRoom } from '@/models/Interfaces';
import FaultRoomPicker from './FaultRoomPicker.vue';
import { computed, ref, useTemplateRef, watch } from 'vue';
import BuildingBlueprint from '../../blueprint/BuildingBlueprint.vue';
import SelectionCard from '../../order/SelectionCard.vue';

const props = withDefaults(
	defineProps<{
		building: IBuildingDetails;
		selectedRoom: IBuildingRoom | null;
		skippedRoom?: boolean;
		skippable?: boolean;
	}>(),
	{ skippable: true }
);
const emit = defineEmits(['select', 'skip']);

const selectRoom = (room: IBuildingRoom | null) => {
	emit('select', room);
};

// The popular name alone is the friendly label, but the raw name (usually a room
// number) disambiguates it - so show both when a popular name exists, and fall
// back to just the number when it doesn't.
const selectedRoomTitle = computed(() => {
	const room = props.selectedRoom;
	if (!room) return '';
	return room.popularName ? `${room.popularName} - ${room.name}` : room.name;
});

// The blueprint is hidden by default to keep the selected-room card compact; the
// "Visa planritning" action expands it on demand.
const showBlueprint = ref(false);

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
	.selected-room {
		.selected-room-blueprint {
			border: solid 1px rgba(0, 0, 0, 0.08);
			border-radius: $border-radius;
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
