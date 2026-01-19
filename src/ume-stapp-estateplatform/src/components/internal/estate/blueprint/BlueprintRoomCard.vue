<template>
	<div
		class="blueprint-room-card map-card estate-default d-flex justify-center"
	>
		<v-card class="room-card ma-4" :elevation="4">
			<div class="card-header">
				<v-card-title>
					{{
						room.popularName
							? room.popularName + '  ' + room.name
							: room.name
					}}
				</v-card-title>
				<div class="d-flex">
					<v-btn
						icon="close"
						rounded="xl"
						size="small"
						flat
						@click="emit('close')"
					/>
				</div>
			</div>
			<v-card-text class="pt-2">
				<div class="properties">
					<div
						class="prop"
						v-for="prop in properties"
						:key="prop.label"
						v-show="prop.value"
					>
						<div class="label">{{ prop.label }}</div>
						<div class="value" :class="prop.class">
							{{ prop.value }}
						</div>
					</div>
				</div>
				<div class="d-flex justify-end">
					<!-- TODO: ENABLE REPORT ROOM BUTTON (and add functionality)-->
					<v-btn
						class="regular-text ma-0 mt-4"
						flat
						color="info"
						rounded="lg"
						disabled
					>
						{{ $t('component.blueprintMap.reportRoom') }}
					</v-btn>
				</div>
			</v-card-text>
		</v-card>
	</div>
</template>

<script setup lang="ts">
import { IBuildingRoom } from '@/models/estate/Interfaces';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	room: IBuildingRoom;
}>();

const emit = defineEmits(['close']);
const { t } = useI18n();

const properties = computed(() => {
	return [
		{
			label: t('estateCommon.floorLabel'),
			value: props.room.floorName,
			class: 'text-capitalize',
		},
		{
			label: t('estateCommon.areaLabel'),
			value: props.room.grossArea,
		},
	];
});
</script>

<style scoped lang="scss">
.blueprint-room-card {
	position: absolute;
	left: 0;
	bottom: 0;
	right: 0;
	pointer-events: none;

	.room-card {
		pointer-events: all;
		width: clamp(200px, 90%, 400px);
	}

	.card-header {
		display: flex;
		flex-wrap: wrap-reverse;
		align-items: start;
		justify-content: end;

		.navigation {
			display: flex;
			align-items: center;
			font-size: size(14);
			.v-btn.v-btn--disabled {
				opacity: 0.8;
				:deep(.v-btn__overlay) {
					display: none;
				}
			}
		}

		.v-btn {
			margin: 4px;
		}
		.v-card-title,
		:deep(.v-skeleton-loader) {
			flex: 1;
			min-width: 200px;
		}
	}
}
</style>
