<template>
	<div>
		<building-blueprint
			v-if="showBlueprint"
			ref="building-blueprint"
			:building="building"
			class="d-none"
			:floor="null"
			@fullscreen-closed="showBlueprint = false"
			@room-selected="selectRoom"
			:selectable="true"
		/>
		<v-btn
			color="primary"
			variant="tonal"
			rounded="lg"
			prepend-icon="map"
			@click="open"
		>
			{{ $t('component.internal.roomSelector.selectOnBlueprint') }}
		</v-btn>
	</div>
</template>

<script setup lang="ts">
import { nextTick, ref, useTemplateRef } from 'vue';
import BuildingBlueprint from '../../blueprint/BuildingBlueprint.vue';
import { appInsights } from '@/plugins/appInsights';
import { IBuildingDetails, IBuildingRoom } from '@/models/Interfaces';

defineProps<{
	building: IBuildingDetails;
}>();

const emit = defineEmits(['room-selected']);

const buildingBlueprintRef = useTemplateRef('building-blueprint');
const showBlueprint = ref(false);

const waitForBlueprintToRender = async () => {
	for (let i = 0; i < 3; i++) {
		if (buildingBlueprintRef.value) {
			return;
		}
		await nextTick();
	}
};

const open = async () => {
	showBlueprint.value = true;
	await waitForBlueprintToRender();

	buildingBlueprintRef.value?.openFullscreen();

	appInsights?.trackEvent({
		name: 'EstateSelectRoomOnBlueprintClicked',
		properties: {
			url: window.location.href,
		},
	});
};

const selectRoom = (room: IBuildingRoom) => {
	emit('room-selected', room);
	showBlueprint.value = false;
	appInsights?.trackEvent({
		name: 'EstateRoomSelectedOnBlueprint',
		properties: {
			url: window.location.href,
			roomId: room.id,
			roomName: room.name,
			roomPopularName: room.popularName,
			floorName: room.floorName,
			buildingId: room.buildingId,
		},
	});
};
</script>
