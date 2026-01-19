<template>
	<app-content
		class="building-details estate-default"
		:pageTitle="`${buildingName} - ${estateName} - ${$t(
			'component.appHeader.title.internalEstate'
		)}`"
	>
		<div class="container">
			<div class="content">
				<div v-if="isBusyFetching" class="loader-lazy">
					<v-skeleton-loader type="article" class="mx-4 my-4" />
				</div>
				<div v-if="building">
					<div class="content-header" :class="{ scrolled: y > 0 }">
						<div class="content-header--content pa-4 px-6">
							<nav-breadcrumbs
								class="mb-2"
								:breadcrumbs="breadcrumbs"
							/>

							<div class="d-flex align-center ga-4">
								<h1 :title="buildingName">
									{{ buildingName }}
								</h1>

								<v-chip
									variant="flat"
									color="primary"
									class="flex-shrink-0"
								>
									{{ $t('estateCommon.type.building') }}
								</v-chip>
							</div>
						</div>
					</div>
					<building-notice-board
						v-if="noticeBoard"
						class="mx-6"
						:notice-board="noticeBoard"
					/>

					<div class="properties-wrap px-6 d-flex align-start ga-4">
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

							<div class="chip-properties">
								<v-chip v-if="building.metrics?.floorCount">
									{{
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
						<img
							v-if="building.image"
							:src="building.image?.thumbnailUrl"
							:alt="
								$t(
									'component.internal.buildingDetails.buildingImageAlt'
								)
							"
							class="cursor-pointer building-image"
							@click="showImageInModal(building.image.imageUrl)"
						/>
					</div>
					<external-owner-info
						v-if="
							building.externalOwnerInfo?.name ||
							building.externalOwnerInfo?.note
						"
						class="mt-4"
						:externalOwnerInfo="building.externalOwnerInfo"
					/>

					<hr class="my-4 mx-6" />
					<div class="circle-button-toggles px-6">
						<base-icon-button
							icon="location_pin"
							:label="
								$t(
									'component.internal.buildingDetails.mapButton'
								)
							"
							:active="activeMap === ActiveMapType.Map"
							@click="activeMap = ActiveMapType.Map"
						/>
						<base-icon-button
							icon="map"
							:label="
								building?.blueprintAvailable
									? $t(
											'component.internal.buildingDetails.blueprintButton'
									  )
									: $t(
											'component.internal.buildingDetails.blueprintMissingButton'
									  )
							"
							:active="activeMap === ActiveMapType.Blueprint"
							:disabled="!building.blueprintAvailable"
							@click="activeMap = ActiveMapType.Blueprint"
						/>
					</div>
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
						<base-icon-button
							icon="map"
							:label="
								building?.blueprintAvailable
									? $t(
											'component.internal.buildingDetails.blueprintButton'
									  )
									: $t(
											'component.internal.buildingDetails.blueprintMissingButton'
									  )
							"
							:disabled="!building.blueprintAvailable"
							@click="buildingBlueprintRef?.openFullscreen()"
						/>
					</div>

					<hr class="mt-4 mx-6" />

					<h2 class="mt-4 px-6">
						{{
							$t('component.internal.buildingDetails.room.title')
						}}
					</h2>
					<building-details-rooms
						ref="room-list"
						class="list mb-4"
						:buildingId="building.id"
						@room-selected="
							(roomId) => buildingBlueprintRef?.selectRoom(roomId)
						"
						v-model:floor="selectedFloorId"
					/>
				</div>
			</div>
			<div class="map">
				<building-blueprint
					v-if="building && building.blueprintAvailable"
					v-show="activeMap === ActiveMapType.Blueprint"
					ref="building-blueprint"
					:buildingId="building.id"
					@room-selected="(roomId) => roomList?.focusRoom(roomId)"
					v-model:floor="selectedFloorId"
				/>
				<building-map
					v-if="building"
					v-show="activeMap === ActiveMapType.Map"
					ref="building-map"
					:points="
						building.geoLocation
							? [
									{
										...building.geoLocation,
										id: building.id,
										type: EstateType.Building,
									},
							  ]
							: []
					"
					:highlighted-point-id="building.id"
					fit-points
				/>
			</div>
		</div>

		<base-image-modal />
	</app-content>
</template>

<script setup lang="ts">
import AppContent from '@/components/app/AppContent.vue';
import { DispatchType } from '@/models/Enums';
import { IBuildingDetails } from '@/models/estate/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { EstateRoutes, MyPagesRoutes } from '@/router/routes';
import { computed, ref, useTemplateRef, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import NavBreadcrumbs from '@/components/internal/shared/NavBreadcrumbs.vue';
import BuildingDetailsRooms from '@/components/internal/estate//BuildingDetailsRooms.vue';
import BuildingBlueprint from '@/components/internal/estate/blueprint/BuildingBlueprint.vue';
import { useScroll } from '@vueuse/core';
import BuildingNoticeBoard from '@/components/internal/estate/BuildingNoticeBoard.vue';
import '@/themes/estate.scss';
import BaseImageModal from '@/components/base/baseImageModal/BaseImageModal.vue';
import { useBaseImageModal } from '@/components/base/baseImageModal/baseImageModal';
import { EstateType } from '../../../models/estate/Enums';
import BaseIconButton from '@/components/base/BaseIconButton.vue';
import BuildingMap from './map/BuildingMap.vue';
import { useRouteSlug } from '@/router/routeSlug';
import ExternalOwnerInfo from './ExternalOwnerInfo.vue';

const props = defineProps<{
	buildingId: string;
}>();

const { t } = useI18n();
const store = useStore<IRootState>();
const building = ref<IBuildingDetails | null>(null);
const selectedFloorId = ref<number | null>(null);

enum ActiveMapType {
	Map = 'map',
	Blueprint = 'blueprint',
}
const activeMap = ref<ActiveMapType>(ActiveMapType.Map);

const roomList = useTemplateRef('room-list');
const buildingMapRef = useTemplateRef('building-map');
const buildingBlueprintRef = useTemplateRef('building-blueprint');
const { showImageInModal } = useBaseImageModal();

const estateName = computed(() => {
	return (
		building.value?.estate.popularName || building.value?.estate.name || ''
	);
});
const buildingName = computed(() => {
	return building.value?.popularName || building.value?.name || '';
});
useRouteSlug(buildingName);

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
				params: { estateId: building.value?.estate.id },
			},
		},
		{
			title: buildingName.value,
			to: {
				name: EstateRoutes.BuildingDetails,
				params: { buildingId: props.buildingId },
			},
		},
	];
});

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
		{
			label: t('estateCommon.externalOwner.status'),
			value: building.value?.externalOwnerInfo?.status,
		},
	];
});

const noticeBoard = computed(() => {
	return building.value?.noticeBoard || null;
});

const isBusyFetching = ref(false);
const fetchBuilding = async (id: string) => {
	isBusyFetching.value = true;
	try {
		building.value = await store.dispatch(DispatchType.GetBuildingById, {
			buildingId: id,
		});

		if (building.value?.blueprintAvailable) {
			activeMap.value = ActiveMapType.Blueprint;
		} else {
			activeMap.value = ActiveMapType.Map;
		}
	} finally {
		isBusyFetching.value = false;
	}
};

watch(
	() => props.buildingId,
	(newId) => {
		fetchBuilding(newId);
	},
	{ immediate: true }
);

const { y } = useScroll(window);
</script>

<style scoped lang="scss">
.building-details {
	.properties-wrap {
		justify-content: space-between;
	}
	.chip-properties {
		.v-chip {
			margin: 1rem 1rem 0 0;
		}
	}

	@media only screen and (max-width: 620px) {
		.properties-wrap {
			flex-wrap: wrap-reverse;
		}
		.building-image {
			width: 100%;
			height: 120px;
			aspect-ratio: initial;
		}
	}
}
</style>
