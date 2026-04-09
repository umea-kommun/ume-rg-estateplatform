<template>
	<div class="map-building-carousel estate-default">
		<v-card class="building-card" rounded="lg" :key="building?.id">
			<router-link
				v-if="building?.imageUrl"
				:to="{
					name: EstateRoutes.BuildingDetails,
					params: { buildingId: building.id },
				}"
			>
				<building-image
					:src="building.imageUrl"
					:image-width="300"
					class="cursor-pointer building-image"
				/>
			</router-link>
			<div class="card-header">
				<v-skeleton-loader
					v-if="isBusyLoadingBuildings && !building"
					type="heading"
					width="100%"
					height="48px"
					flex-grow="1"
				></v-skeleton-loader>
				<v-card-title
					v-else-if="building"
					class="d-flex align-center pr-0"
				>
					<div class="title">
						<router-link
							:to="{
								name: EstateRoutes.BuildingDetails,
								params: { buildingId: building.id },
							}"
							:title="building?.popularName ?? building?.name"
						>
							{{ building?.popularName ?? building?.name }}
						</router-link>
					</div>
					<favorite-button
						:id="building.id"
						:type="EstateType.Building"
						:isFavorite="building.isFavorite"
						size="small"
					/>
				</v-card-title>

				<v-btn
					icon="close"
					rounded="xl"
					size="small"
					flat
					@click="close"
				/>
			</div>
			<v-skeleton-loader
				v-if="isBusyLoadingBuildings && !building"
				type="list-item-three-line"
				width="100%"
				height="114px"
			></v-skeleton-loader>
			<v-card-text v-else-if="building" class="pt-2 d-flex align-start">
				<div>
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
					<div class="chip-properties d-flex flex-wrap ga-2 mt-4">
						<v-chip v-if="building?.metrics?.floorCount"
							>{{
								$t('estateCommon.floorCount', {
									count: building.metrics?.floorCount,
								})
							}}
						</v-chip>
						<v-chip v-if="building.metrics?.roomCount">
							{{
								$t('estateCommon.roomCount', {
									count: building.metrics?.roomCount,
								})
							}}
						</v-chip>

						<v-chip v-if="building.metrics?.areaSqm">
							{{ building.metrics?.areaSqm?.toLocaleString() }}
							m²
						</v-chip>
					</div>
				</div>
			</v-card-text>
			<div v-if="selectable" class="d-flex justify-center pb-2">
				<v-btn
					class="regular-text"
					flat
					color="primary"
					@click="$emit('select', building?.id)"
				>
					{{ $t('component.map.selectBuilding') }}
				</v-btn>
			</div>
		</v-card>
		<v-card v-if="(buildingIds?.length ?? 0) > 1" class="navigation mt-2">
			<v-btn
				icon="chevron_left"
				rounded="0"
				size="small"
				variant="text"
				:disabled="activeBuildingIndex <= 0"
				@click="previousBuilding"
			/>
			<div class="label">
				{{
					$t('component.map.buildingXofY', {
						buildingNumber: activeBuildingIndex + 1,
						totalBuildings: buildingIds?.length,
					})
				}}
			</div>
			<v-btn
				icon="chevron_right"
				rounded="0"
				size="small"
				variant="text"
				:disabled="
					activeBuildingIndex >= (buildingIds?.length ?? 0) - 1
				"
				@click="nextBuilding"
			/>
		</v-card>
	</div>
</template>

<script lang="ts" setup>
import { DispatchType } from '@/models/Enums';
import { IBuildingDetails } from '@/models/estate/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { computed, ref, watch } from 'vue';
import { useStore } from 'vuex';
import { EstateRoutes } from '@/router/routes';
import { useI18n } from 'vue-i18n';
import BuildingImage from '../building/BuildingImage.vue';
import FavoriteButton from '../favorite/FavoriteButton.vue';
import { EstateType } from '@/models/estate/Enums';
import ErrorService from '@/utils/ErrorService';

const props = defineProps<{
	modelValue: number[] | null;
	activeBuildingId: number | null;
	selectable?: boolean;
}>();

const emit = defineEmits([
	'update:modelValue',
	'update:activeBuildingId',
	'select',
]);

const store = useStore<IRootState>();
const { t } = useI18n();

const buildingIds = computed({
	get: () => props.modelValue,
	set: (val: number[] | null) => {
		emit('update:modelValue', val);
	},
});

const activeBuildingId = computed({
	get: () => props.activeBuildingId,
	set: (val: number | null) => {
		emit('update:activeBuildingId', val);
	},
});

const isBusyLoadingBuildings = ref(false);
const buildings = ref<IBuildingDetails[]>([]);

const building = computed(() => {
	if (buildingIds.value && activeBuildingId.value !== null) {
		return (
			buildings.value.find((b) => b.id === activeBuildingId.value) || null
		);
	}
	return null;
});

const activeBuildingIndex = computed(() => {
	if (buildingIds.value && activeBuildingId.value !== null) {
		return buildingIds.value.indexOf(activeBuildingId.value);
	}
	return -1;
});

const previousBuilding = () => {
	const currentIndex = activeBuildingIndex.value;
	if (buildingIds.value && currentIndex > 0) {
		activeBuildingId.value = buildingIds.value[currentIndex - 1];
	}
};

const nextBuilding = () => {
	const currentIndex = activeBuildingIndex.value;
	if (
		buildingIds.value &&
		currentIndex >= 0 &&
		currentIndex < buildingIds.value.length - 1
	) {
		activeBuildingId.value = buildingIds.value[currentIndex + 1];
	}
};

const close = () => {
	buildingIds.value = null;
	activeBuildingId.value = null;
	buildings.value = [];
};

const lowercaseAddress = computed(() => {
	if (!building.value?.address) return '';
	const adr = building.value.address;
	const address = `${adr.street.trim()}, ${adr.zipCode.trim()} ${adr.city.trim()}`;
	return address.toLocaleLowerCase();
});

const properties = computed(() => {
	return [
		{
			label: t('estateCommon.addressLabel'),
			value: lowercaseAddress.value,
			class: 'text-capitalize',
		},
		{
			label: t('estateCommon.yearOfConstructionLabel'),
			value: building.value?.metrics?.yearOfConstruction,
		},
		{
			label: t('estateCommon.operationalAreaLabel'),
			value: building.value?.region.name?.toLocaleLowerCase(),
			class: 'text-capitalize',
		},
	];
});

const fetchBuildings = async (buildingIds: number[]) => {
	if (!buildingIds.length) {
		buildings.value = [];
		return;
	}
	isBusyLoadingBuildings.value = true;
	try {
		buildings.value = await Promise.all(
			buildingIds.map((id) =>
				store.dispatch(DispatchType.GetBuildingById, { buildingId: id })
			)
		);
	} catch (err) {
		ErrorService.onError({ err });
	} finally {
		isBusyLoadingBuildings.value = false;
	}
};

watch(
	() => buildingIds.value,
	(newBuildingIds) => {
		if (newBuildingIds?.length) {
			activeBuildingId.value = newBuildingIds[0];
			fetchBuildings(newBuildingIds);
		} else {
			activeBuildingId.value = null;
			buildings.value = [];
		}
	}
);
</script>

<style scoped lang="scss">
.map-building-carousel {
	.building-card {
		position: relative;
		pointer-events: auto;
		container-type: inline-size;

		.card-header {
			display: flex;

			.v-btn {
				margin: 4px;
			}
			.v-card-title {
				flex: 1;

				.title {
					overflow: hidden;
					text-overflow: ellipsis;
					white-space: nowrap;

					a {
						text-decoration: none;
						&:hover {
							text-decoration: underline;
						}
					}
				}
			}

			:deep(.v-skeleton-loader) {
				flex: 1;
				min-width: 200px;
			}
		}

		.building-image {
			width: 100%;
			max-height: 110px;
			border-radius: 8px 8px 0 0;
			corner-shape: initial;
			object-fit: cover;
		}
	}
	.navigation {
		pointer-events: auto;
		display: flex;
		justify-content: center;
		align-items: center;
		gap: 8px;
		font-size: size(14);
		.label {
			flex: 1;
			text-align: center;
		}
		.v-btn {
			margin: 0;
		}
		.v-btn.v-btn--disabled {
			opacity: 0.4;
			:deep(.v-btn__overlay) {
				display: none;
			}
		}
	}
}
</style>
