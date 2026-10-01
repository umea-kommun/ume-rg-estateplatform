<template>
	<div class="blueprint-controls w-100">
		<div class="blueprint-controls__top">
			<v-btn
				v-if="!fullScreen || canLeaveFullscreen"
				:icon="fullScreen ? 'close_fullscreen' : 'open_in_full'"
				@click="fullScreen = !fullScreen"
				:title="
					fullScreen
						? t('component.map.fullScreenClose')
						: t('component.map.fullScreenOpen')
				"
				size="small"
			/>
			<v-btn
				v-if="closable"
				icon="close"
				@click="emit('close')"
				:title="t('component.blueprintMap.close')"
				size="small"
			/>
		</div>
		<v-spacer></v-spacer>
		<div class="d-flex align-end w-100 ga-4">
			<v-sheet
				v-if="stackedFloors.length"
				class="blueprint-controls__floors"
				:class="{
					'blueprint-controls__floors--collapsed': !floorsExpanded,
				}"
				:title="t('component.blueprintMap.floor')"
				rounded="lg"
				:elevation="2"
			>
				<v-btn
					class="blueprint-controls__floors-label"
					variant="text"
					rounded="0"
					:title="
						floorsExpanded
							? t('component.blueprintMap.collapseFloors')
							: t('component.blueprintMap.expandFloors')
					"
					:aria-expanded="floorsExpanded"
					@click="floorsExpanded = !floorsExpanded"
				>
					<svg
						width="24"
						height="24"
						viewBox="0 -960 960 960"
						fill="currentColor"
						aria-hidden="true"
					>
						<path
							d="M80-200v-80h240v-240h240v-240h320v80H640v240H400v240H80Z"
						/>
					</svg>
				</v-btn>
				<v-btn-toggle
					v-model="selectedFloorId"
					direction="vertical"
					mandatory
					:aria-label="t('component.blueprintMap.floor')"
				>
					<v-expand-transition
						v-for="floor in stackedFloors"
						:key="floor.id"
					>
						<div
							v-show="
								floorsExpanded || floor.id === selectedFloorId
							"
							class="blueprint-controls__floor"
						>
							<v-btn
								:value="floor.id"
								:title="
									floor.popularName ||
									t('component.blueprintMap.floor')
								"
								variant="flat"
								size="small"
								@click="floorsExpanded = true"
							>
								{{ floor.name }}
							</v-btn>
						</div>
					</v-expand-transition>
				</v-btn-toggle>
			</v-sheet>
			<div class="d-flex flex-column ga-4 ms-auto">
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
import { stackFloors } from '@/components/estate/defaultFloor';
import { IBuildingFloor } from '@/models/Interfaces';
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	selectedFloorId: number | null;
	floors: IBuildingFloor[];
	fullScreen: boolean;
	zoomInDisabled?: boolean;
	zoomOutDisabled?: boolean;
	closable?: boolean;
	canLeaveFullscreen?: boolean;
}>();

const { t } = useI18n();

const emit = defineEmits([
	'update:selectedFloorId',
	'update:fullScreen',
	'zoom-in',
	'zoom-out',
	'print',
	'close',
]);

const stackedFloors = computed(() => stackFloors(props.floors));
const floorsExpanded = ref(true);

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

	.blueprint-controls__top {
		display: flex;
		gap: 10px;
	}

	.v-btn {
		pointer-events: all;
		margin: 0;

		:deep(.v-icon) {
			color: $grey-darken-4;
		}
	}

	.blueprint-controls__floors {
		pointer-events: all;
		display: flex;
		flex-direction: column;
		overflow: hidden;
		min-width: 40px;

		.v-btn-toggle {
			border-top: solid 1px $grey-lighten-3;
			border-radius: 0;
		}

		.v-btn-toggle .v-btn {
			width: 100%;
			min-width: 0;
			height: 40px;
			padding: 0;

			&.v-btn--active {
				background: rgb(var(--v-theme-primary));
				color: rgb(var(--v-theme-on-primary));
			}
		}

		.blueprint-controls__floor:not(:last-child) {
			border-bottom: solid 1px $grey-lighten-3;
		}

		&.blueprint-controls__floors--collapsed .blueprint-controls__floor {
			border-bottom: none;
		}

		.blueprint-controls__floors-label {
			width: 100%;
			min-width: 0;
			height: 40px;
			padding: 0;
			color: $grey-darken-4;
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
