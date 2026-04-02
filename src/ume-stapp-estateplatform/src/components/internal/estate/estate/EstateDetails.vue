<template>
	<app-content
		class="estate-details estate-default"
		:pageTitle="`${estateName} - ${$t(
			'component.appHeader.title.internalEstate'
		)}`"
	>
		<div class="container">
			<div class="content pb-6">
				<div v-if="isBusyFetchingEstate" class="loader-lazy">
					<v-skeleton-loader type="article" class="mx-4 my-4" />
				</div>
				<div v-if="estate">
					<div class="content-header" :class="{ scrolled: y > 0 }">
						<div class="content-header--content pa-4 px-6">
							<nav-breadcrumbs
								class="mb-2"
								:breadcrumbs="breadcrumbs"
							/>

							<div
								class="d-flex align-start justify-space-between"
							>
								<h1 :title="estateName">
									{{ estateName }}
								</h1>
								<favorite-button
									:id="estate.id"
									:type="EstateType.Estate"
									:isFavorite="estate.isFavorite"
								/>
							</div>
						</div>
					</div>
					<div class="px-6">
						<div class="properties">
							<div
								class="prop"
								v-for="prop in properties"
								:key="prop.label"
								v-show="prop.value"
							>
								<div class="label">{{ prop.label }}</div>
								<div class="value">
									{{ prop.value }}
								</div>
							</div>
						</div>
						<div class="chip-properties">
							<v-chip
								class="flex-shrink-0"
								:class="estate.type"
								variant="flat"
								color="info"
							>
								{{ $t('estateCommon.type.estate') }}
							</v-chip>
							<v-chip v-if="estate.metrics?.buildingCount">
								{{
									$t('estateCommon.buildingCount', {
										count: estate.metrics?.buildingCount,
									})
								}}
							</v-chip>
							<v-chip v-if="estate.metrics?.areaSqm">
								{{ estate.metrics?.areaSqm?.toLocaleString() }}
								m²
							</v-chip>
						</div>
					</div>

					<external-owner-info
						v-if="
							estate.externalOwnerInfo?.name ||
							estate.externalOwnerInfo?.note
						"
						class="mt-4"
						:externalOwnerInfo="estate.externalOwnerInfo"
					/>

					<hr class="my-4 mx-6" />
					<div class="circle-button-toggles mobile px-6">
						<base-icon-button
							icon="location_pin"
							:label="
								$t(
									'component.internal.buildingDetails.mapButton'
								)
							"
							@click="buildingMapRef?.openFullscreen()"
						/>
					</div>
					<hr class="circle-button-toggles mobile mt-4 mx-6" />

					<h2 class="px-6">
						{{ $t('component.internal.estateDetails.buildings') }}
					</h2>
					<estate-details-buildings
						:estateId="estate.id"
						:loading="isBusyFetchingBuildings"
						:buildings="buildings"
						class="list"
						@building-mouseenter="(id) => (hoveredBuildingId = id)"
						@building-mouseleave="hoveredBuildingId = null"
					/>
				</div>
			</div>

			<div class="map">
				<building-map
					ref="building-map"
					:points="buildingPoints"
					:highlighted-point-id="hoveredBuildingId"
					fit-points
				/>
			</div>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import AppContent from '@/components/app/AppContent.vue';
import { DispatchType } from '@/models/Enums';
import {
	IEstateBuilding,
	IEstateDetails,
	IMapPoint,
} from '@/models/estate/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { EstateRoutes, MyPagesRoutes } from '@/router/routes';
import { computed, ref, watch, useTemplateRef } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import { useScroll } from '@vueuse/core';
import NavBreadcrumbs from '@/components/internal/shared/NavBreadcrumbs.vue';
import EstateDetailsBuildings from './EstateDetailsBuildings.vue';
import '@/themes/estate.scss';
import { EstateType } from '@/models/estate/Enums';
import BuildingMap from '../map/BuildingMap.vue';
import BaseIconButton from '@/components/base/BaseIconButton.vue';
import ExternalOwnerInfo from './ExternalOwnerInfo.vue';
import FavoriteButton from '../favorite/FavoriteButton.vue';

const props = defineProps<{
	estateId: string;
}>();

const { t } = useI18n();
const store = useStore<IRootState>();
const estate = ref<IEstateDetails | null>(null);
const buildingMapRef = useTemplateRef('building-map');

const estateName = computed(() => {
	return estate.value?.popularName || estate.value?.name || '';
});

const breadcrumbs = computed(() => {
	return [
		{
			title: t('app.nav.home'),
			to: { name: MyPagesRoutes.InternalStart },
		},
		{
			title: t('component.internal.estateSearch.breadcrumb'),
			to: { name: EstateRoutes.Search },
		},
		{
			title: estateName.value,
			to: {
				name: EstateRoutes.EstateDetails,
				params: { estateId: props.estateId },
			},
		},
	];
});

const properties = computed(() => {
	return [
		{
			label: t('estateCommon.propertyDesignationLabel'),
			value: estate.value?.name,
		},
		{
			label: t('estateCommon.municipalityAreaLabel'),
			value: estate.value?.municipalityArea,
		},
		{
			label: t('estateCommon.operationalAreaLabel'),
			value: estate.value?.operationalArea,
		},
		{
			label: t('estateCommon.administrativeAreaLabel'),
			value: estate.value?.administrativeArea,
		},
		{
			label: t('estateCommon.externalOwner.status'),
			value: estate.value?.externalOwnerInfo?.status,
		},
	];
});

const buildings = ref<IEstateBuilding[] | null>(null);
const hoveredBuildingId = ref<number | null>(null);

const buildingPoints = computed<IMapPoint[]>(() => {
	const list = buildings.value ?? [];

	return list.flatMap((building) => {
		const geo = building.geoLocation;
		if (!geo?.lat || !geo?.lon) return [];
		return [
			{
				id: building.id,
				type: EstateType.Building,
				lon: geo.lon,
				lat: geo.lat,
			},
		];
	});
});

const isBusyFetchingBuildings = ref(false);
const fetchBuildings = async (estateId: string) => {
	isBusyFetchingBuildings.value = true;
	buildings.value = await store.dispatch(DispatchType.GetEstateBuildings, {
		estateId: estateId,
	});
	isBusyFetchingBuildings.value = false;
};

const isBusyFetchingEstate = ref(false);
const fetchEstate = async (id: string) => {
	isBusyFetchingEstate.value = true;
	try {
		estate.value = await store.dispatch(DispatchType.GetEstateById, {
			estateId: id,
		});
	} finally {
		isBusyFetchingEstate.value = false;
	}
};

watch(
	() => props.estateId,
	(newId) => {
		fetchEstate(newId);
		fetchBuildings(newId);
	},
	{ immediate: true }
);

const { y } = useScroll(window);
</script>

<style scoped lang="scss">
.estate-details {
	.map {
		/** Map flickers when moving to buildings, fade it in to hide flicker */
		animation: fadeIn 0.3s ease-in-out;

		@keyframes fadeIn {
			0% {
				opacity: 0;
			}
			70% {
				opacity: 0;
			}
			100% {
				opacity: 1;
			}
		}
	}
}
.chip-properties {
	.v-chip {
		margin-top: 1rem;
		margin-right: 1rem;
	}
}
</style>
