<template>
	<div
		class="map-building-carousel estate-default"
		v-if="isBusyLoadingBuilding || building"
	>
		<v-card>
			<div>
				<div class="card-header">
					<v-skeleton-loader
						v-if="isBusyLoadingBuilding && !building"
						type="heading"
						width="100%"
						height="48px"
						flex-grow="1"
					></v-skeleton-loader>
					<v-card-title v-else-if="building">
						<router-link
							:to="{
								name: EstateRoutes.BuildingDetails,
								params: { buildingId: building.id },
							}"
						>
							{{ building?.popularName ?? building?.name }}
						</router-link>
					</v-card-title>
					<div class="d-flex">
						<div
							v-if="(buildingIds?.length ?? 0) > 1"
							class="navigation"
						>
							<v-btn
								icon="chevron_left"
								rounded="xl"
								size="small"
								flat
								:disabled="activeBuildingIndex <= 0"
								@click="previousBuilding"
							/>
							{{ activeBuildingIndex + 1 }} /
							{{ buildingIds?.length }}
							<v-btn
								icon="chevron_right"
								rounded="xl"
								size="small"
								flat
								:disabled="
									activeBuildingIndex >=
									(buildingIds?.length ?? 0) - 1
								"
								@click="nextBuilding"
							/>
						</div>
						<v-btn
							icon="close"
							rounded="xl"
							size="small"
							flat
							@click="close"
						/>
					</div>
				</div>
				<v-skeleton-loader
					v-if="isBusyLoadingBuilding && !building"
					type="list-item-three-line"
					width="100%"
					height="114px"
				></v-skeleton-loader>
				<v-card-text
					v-else-if="building"
					class="pt-2 d-flex align-start"
				>
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
								{{
									building.metrics?.areaSqm?.toLocaleString()
								}}
								m²
							</v-chip>
						</div>
					</div>
					<router-link
						v-if="building.imageUrl"
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
				</v-card-text>
			</div>
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

const props = defineProps<{
	modelValue: number[] | null;
	activeBuildingId: number | null;
}>();

const emit = defineEmits(['update:modelValue', 'update:activeBuildingId']);

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

const isBusyLoadingBuilding = ref(false);
const building = ref<IBuildingDetails | null>(null);

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
	building.value = null;
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

const fetchBuilding = async (buildingId: number) => {
	isBusyLoadingBuilding.value = true;
	building.value = await store.dispatch(DispatchType.GetBuildingById, {
		buildingId,
	});
	isBusyLoadingBuilding.value = false;
};

watch(activeBuildingId, (newBuildingId) => {
	building.value = null;
	if (newBuildingId !== null) {
		fetchBuilding(newBuildingId);
	}
});
watch(
	() => buildingIds.value,
	(newBuildingIds) => {
		if (newBuildingIds?.length) {
			activeBuildingId.value = newBuildingIds[0];
		} else {
			activeBuildingId.value = null;
		}
	}
);
</script>

<style scoped lang="scss">
.map-building-carousel {
	pointer-events: none;
	position: absolute;
	bottom: 0;
	right: 0;
	left: 0;
	padding: 14px;
	display: flex;
	justify-content: center;

	.v-card {
		pointer-events: auto;
		width: 100%;
		max-width: 520px;
		container-type: inline-size;

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

				a {
					text-decoration: none;
					&:hover {
						text-decoration: underline;
					}
				}
			}
		}

		.v-card-text {
			.building-image {
				margin-left: 16px;
			}

			@container (max-width: 400px) {
				flex-wrap: wrap-reverse;

				a:has(.building-image) {
					flex: 1;
				}
				.building-image {
					margin: 0 0 8px 0;
					width: 100%;
					height: 100px;

					aspect-ratio: initial;
					object-fit: cover;
				}
			}
		}
	}
}
</style>
