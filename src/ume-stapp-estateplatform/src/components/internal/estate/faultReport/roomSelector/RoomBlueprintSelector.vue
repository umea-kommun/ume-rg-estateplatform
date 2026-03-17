<template>
	<building-blueprint
		v-if="showBlueprint"
		ref="building-blueprint"
		:building-id="buildingId"
		class="d-none"
		:floor="null"
		@fullscreen-closed="showBlueprint = false"
		@room-selected="(room) => emit('room-selected', room)"
		:selectable="true"
	/>
</template>

<script setup lang="ts">
import { nextTick, ref, useTemplateRef } from 'vue';
import BuildingBlueprint from '../../blueprint/BuildingBlueprint.vue';

defineProps<{
	buildingId: number;
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
};

defineExpose({
	open,
});
</script>
