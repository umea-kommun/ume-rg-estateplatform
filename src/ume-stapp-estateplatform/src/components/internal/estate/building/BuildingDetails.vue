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

					<building-properties class="px-6" :building="building" />

					<external-owner-info
						v-if="
							building.externalOwnerInfo?.name ||
							building.externalOwnerInfo?.note
						"
						class="mt-4"
						:externalOwnerInfo="building.externalOwnerInfo"
					/>

					<hr class="my-4 mx-6" />

					<building-details-buttons
						class="px-6"
						:building="building"
						v-model:active-map="activeMap"
						@open-map-fullscreen="buildingMapRef?.openFullscreen()"
						@open-blueprint-fullscreen="
							buildingBlueprintRef?.openFullscreen()
						"
					/>

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
import BuildingDetailsRooms from '@/components/internal/estate/building/BuildingDetailsRooms.vue';
import BuildingBlueprint from '@/components/internal/estate/blueprint/BuildingBlueprint.vue';
import { useScroll } from '@vueuse/core';
import BuildingNoticeBoard from '@/components/internal/estate/building/BuildingNoticeBoard.vue';
import '@/themes/estate.scss';
import { ActiveMapType, EstateType } from '@/models/estate/Enums';
import BuildingMap from '@/components/internal/estate/map/BuildingMap.vue';
import ExternalOwnerInfo from '@/components/internal/estate/estate/ExternalOwnerInfo.vue';
import BuildingProperties from './BuildingProperties.vue';
import BuildingDetailsButtons from './BuildingDetailsButtons.vue';

const props = defineProps<{
	buildingId: string;
}>();

const { t } = useI18n();
const store = useStore<IRootState>();
const building = ref<IBuildingDetails | null>(null);
const selectedFloorId = ref<number | null>(null);

const activeMap = ref<ActiveMapType>(ActiveMapType.Map);

const roomList = useTemplateRef('room-list');
const buildingMapRef = useTemplateRef('building-map');
const buildingBlueprintRef = useTemplateRef('building-blueprint');

const estateName = computed(() => {
	return (
		building.value?.estate.popularName || building.value?.estate.name || ''
	);
});
const buildingName = computed(() => {
	return building.value?.popularName || building.value?.name || '';
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
