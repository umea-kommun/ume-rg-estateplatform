<template>
	<div class="blueprint-controls w-100">
		<v-btn
			:icon="fullScreen ? 'close' : 'open_in_full'"
			@click="fullScreen = !fullScreen"
			:title="
				fullScreen
					? t('component.map.fullScreenClose')
					: t('component.map.fullScreenOpen')
			"
			size="small"
		/>
		<v-spacer></v-spacer>
		<div class="d-flex align-end justify-space-between w-100 ga-4">
			<v-select
				:items="floors"
				item-title="name"
				item-value="id"
				color="primary"
				v-model="selectedFloorId"
				:label="t('component.blueprintMap.floor')"
				variant="solo"
				rounded="lg"
				hide-details
			/>
			<div class="d-flex flex-column ga-4">
				<v-btn icon="print" size="small" @click="emit('print')" />
				<v-btn-group direction="vertical" :elevation="2">
					<v-btn
						rounded="0"
						icon="add"
						:title="t('component.map.zoomIn')"
						@click="emit('zoom-in')"
						size="small"
						:disabled="zoomInDisabled"
					/>
					<v-btn
						rounded="0"
						icon="remove"
						:title="t('component.map.zoomOut')"
						@click="emit('zoom-out')"
						size="small"
						:disabled="zoomOutDisabled"
					/>
				</v-btn-group>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { IBuildingFloor } from '@/models/Interfaces';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	selectedFloorId: number | null;
	floors: IBuildingFloor[];
	fullScreen: boolean;
	zoomInDisabled?: boolean;
	zoomOutDisabled?: boolean;
}>();

const { t } = useI18n();

const emit = defineEmits([
	'update:selectedFloorId',
	'update:fullScreen',
	'zoom-in',
	'zoom-out',
	'print',
]);

const selectedFloorId = computed({
	get: () => props.selectedFloorId,
	set: (value: number | null) => {
		emit('update:selectedFloorId', value);
	},
});

const fullScreen = computed({
	get: () => props.fullScreen,
	set: (value: boolean) => {
		emit('update:fullScreen', value);
	},
});
</script>

<style lang="scss" scoped>
.blueprint-controls {
	touch-action: manipulation;
	pointer-events: none;
	display: flex;
	flex-direction: column;
	align-items: end;
	position: absolute;
	right: 0;
	top: 0;
	bottom: 0;
	padding: 14px;
	gap: 10px;
	.v-btn {
		pointer-events: all;
		margin: 0;

		:deep(.v-icon) {
			color: $grey-darken-4;
		}
	}

	.v-select {
		pointer-events: all;
		max-width: 160px;
		:deep(.v-input__control) {
			width: fit-content;
			min-width: 100px;
		}
	}

	.v-btn-group {
		border-radius: $border-radius;
		.v-btn {
			border-bottom: solid 1px $grey-lighten-3;
			border-radius: 0;

			&:last-child {
				border-bottom: none;
			}
		}
	}

	@media print {
		display: none;
	}
}
</style>
