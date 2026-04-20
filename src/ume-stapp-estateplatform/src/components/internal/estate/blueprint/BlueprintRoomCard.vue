<template>
	<div
		class="blueprint-room-card map-card estate-default d-flex justify-center"
		:key="room.id"
	>
		<v-card class="room-card ma-4" :elevation="4">
			<div class="card-header">
				<v-card-title>
					<div class="title">
						{{
							room.popularName
								? room.popularName + '  ' + room.name
								: room.name
						}}
					</div>
					<favorite-button
						:id="room.id"
						:type="EstateType.Room"
						:is-favorite="room.isFavorite"
					/>
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
			<v-card-text
				class="pt-2 d-flex align-start flex-wrap justify-space-between ga-4"
			>
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
				<div class="d-flex flex-grow-1 justify-end mt-2">
					<v-btn
						v-if="selectable"
						flat
						color="primary"
						@click="$emit('select', room)"
					>
						{{ $t('component.blueprintMap.selectRoom') }}
					</v-btn>
					<v-btn
						v-else-if="isEnabled('ErrorReport')"
						class="report-btn"
						variant="tonal"
						color="error"
						prepend-icon="warning"
						:to="{
							name: EstateRoutes.FaultReport,
							query: {
								buildingId: room.buildingId,
								roomId: room.id,
							},
						}"
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
import { EstateRoutes } from '@/router/routes';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import FavoriteButton from '../favorite/FavoriteButton.vue';
import { EstateType } from '@/models/estate/Enums';
import { useFeatureFlags } from '@/utils/useFeatureFlags';

const { isEnabled } = useFeatureFlags();

const props = defineProps<{
	room: IBuildingRoom;
	selectable?: boolean;
}>();

const emit = defineEmits(['close', 'select']);
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

		a.report-btn {
			color: $error !important; /* Override default link color */
		}
	}

	.card-header {
		display: flex;
		flex-wrap: wrap-reverse;
		align-items: start;
		justify-content: end;

		.v-card-title {
			display: flex;
			align-items: center;
			flex: 1;
			min-width: 200px;

			.title {
				overflow: hidden;
				text-overflow: ellipsis;
				white-space: nowrap;
			}
		}

		.v-btn {
			margin: 4px;
		}
	}
}
</style>
