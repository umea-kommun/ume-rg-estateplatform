<template>
	<app-content
		class="building-details estate-default"
		:pageTitle="`${buildingName} - ${estateName} - ${$t(
			'component.appHeader.title.default'
		)}`"
	>
		<div class="container">
			<div ref="content" class="content pb-6">
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

							<div
								class="d-flex align-start justify-space-between"
							>
								<h1 :title="buildingName" class="ma-0 mt-2">
									{{ buildingName }}
								</h1>
								<favorite-button
									:id="building.id"
									class="mt-2"
									:type="EstateType.Building"
									:isFavorite="building.isFavorite"
								/>
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

					<hr class="mobile-actions-divider mt-8 mx-6" />

					<div class="mobile-actions px-6 mt-4">
						<building-map-toggle
							variant="inline"
							:active-map="activeMap"
							:blueprint-available="!!building.blueprintAvailable"
							@select="selectMap"
						/>
						<building-details-actions :building="building" />
					</div>

					<hr class="mobile-actions-divider mt-4 mx-6" />

					<div
						ref="tabs-bar"
						class="details-tabs-bar d-flex align-center ga-4 px-6"
					>
						<v-tabs
							v-model="activeTab"
							class="details-tabs"
							color="primary"
						>
							<v-tab
								v-for="tab in tabs"
								:key="tab.value"
								:value="tab.value"
								:prepend-icon="tab.icon"
							>
								<span class="tab-label" :data-label="tab.label">
									{{ tab.label }}
								</span>
							</v-tab>
						</v-tabs>
						<div class="tabs-bar-action">
							<building-details-actions :building="building" />
						</div>
					</div>

					<!-- BuildingRooms has several root nodes, so v-show needs
					an element of its own -->
					<div v-show="activeTab === DetailsTab.Rooms">
						<h2 class="mt-4 mb-0 mx-6">
							{{ $t('component.buildingDetails.room.title') }}
						</h2>
						<div
							v-if="building.hasRoomInformation === false"
							class="mx-6 mt-4"
						>
							{{
								$t(
									'component.buildingDetails.room.noRoomInformation'
								)
							}}
						</div>
						<building-rooms
							v-else
							ref="room-list"
							class="list"
							:buildingId="building.id"
							@room-selected="openRoomInBlueprint"
							v-model:floor="selectedFloorId"
						/>
					</div>
					<building-contact-panel
						v-if="isEnabled('ContactPersons')"
						v-show="activeTab === DetailsTab.ContactPersons"
						class="tab-panel px-6"
						:building="building"
					/>
					<building-document-panel
						v-if="isEnabled('Documents')"
						v-show="activeTab === DetailsTab.Documents"
						class="tab-panel px-6"
						:building="building"
						:active="activeTab === DetailsTab.Documents"
					/>
				</div>
			</div>
			<div ref="map-pane" class="map">
				<building-map-toggle
					v-if="building"
					variant="overlay"
					:active-map="activeMap"
					:blueprint-available="!!building.blueprintAvailable"
					@select="selectMap"
				/>
				<building-blueprint
					v-if="building && building.blueprintAvailable"
					v-show="activeMap === ActiveMapType.Blueprint"
					ref="building-blueprint"
					:building="building"
					@room-opened="focusRoomInList"
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
import { IBuildingDetails } from '@/models/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { EstateRoutes } from '@/router/routes';
import { computed, nextTick, ref, useTemplateRef, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import NavBreadcrumbs from '@/components/shared/NavBreadcrumbs.vue';
import BuildingRooms from '@/components/estate/building/BuildingRooms.vue';
import BuildingBlueprint from '@/components/estate/blueprint/BuildingBlueprint.vue';
import { useScroll } from '@vueuse/core';
import BuildingNoticeBoard from '@/components/estate/building/BuildingNoticeBoard.vue';
import '@/themes/estate.scss';
import { ActiveMapType, EstateType } from '@/models/Enums';
import BuildingMap from '@/components/estate/map/BuildingMap.vue';
import ExternalOwnerInfo from '@/components/estate/estate/ExternalOwnerInfo.vue';
import BuildingProperties from './BuildingProperties.vue';
import BuildingContactPanel from './BuildingContactPanel.vue';
import BuildingDetailsActions from './BuildingDetailsActions.vue';
import BuildingDocumentPanel from './BuildingDocumentPanel.vue';
import BuildingMapToggle from './BuildingMapToggle.vue';
import ErrorService from '@/utils/ErrorService';
import FavoriteButton from '../favorite/FavoriteButton.vue';
import { useRoute } from 'vue-router';
import { useEstateIsMobile } from '../useEstateIsMobile';
import { appInsights } from '@/plugins/appInsights';
import { useFeatureFlags } from '@/utils/useFeatureFlags';

const props = defineProps<{
	buildingId: string;
}>();

const { t, locale } = useI18n();
const store = useStore<IRootState>();
const route = useRoute();
const building = ref<IBuildingDetails | null>(null);
const selectedFloorId = ref<number | null>(null);

const activeMap = ref<ActiveMapType>(ActiveMapType.Map);

enum DetailsTab {
	Rooms = 'rooms',
	ContactPersons = 'contactPersons',
	Documents = 'documents',
}

const { isEnabled } = useFeatureFlags();
const isMobile = useEstateIsMobile();
const activeTab = ref<DetailsTab>(DetailsTab.Rooms);

interface DetailsTabItem {
	value: DetailsTab;
	label: string;
	icon: string;
}

const tabs = computed(() => {
	const items: DetailsTabItem[] = [
		{
			value: DetailsTab.Rooms,
			label: t('component.buildingDetails.room.title'),
			icon: 'meeting_room',
		},
	];

	if (isEnabled('ContactPersons')) {
		items.push({
			value: DetailsTab.ContactPersons,
			label: isMobile.value
				? t('component.buildingDetails.contactPersonsButtonShort')
				: t('component.buildingDetails.contactPersonsButton'),
			icon: 'contacts',
		});
	}

	if (isEnabled('Documents')) {
		items.push({
			value: DetailsTab.Documents,
			label: t('component.buildingDetails.documentsButton'),
			icon: 'insert_drive_file',
		});
	}

	return items;
});

const contentEl = useTemplateRef<HTMLElement>('content');
const mapEl = useTemplateRef<HTMLElement>('map-pane');
const tabsBarEl = useTemplateRef<HTMLElement>('tabs-bar');

watch(activeTab, async () => {
	const contentHeightBefore = contentEl.value?.offsetHeight;

	await nextTick();

	// The map pane is hidden here and reports no height to compare against
	if (isMobile.value) {
		const barTop = tabsBarEl.value?.getBoundingClientRect().top;

		if (barTop !== undefined && barTop < 0) {
			tabsBarEl.value?.scrollIntoView({ block: 'start' });
		}

		return;
	}

	const contentHeightAfter = contentEl.value?.offsetHeight;
	const mapHeight = mapEl.value?.offsetHeight;

	// A left pane shorter than the sticky map leaves it no scroll range to hold
	// its offset, so the page goes back to the top instead
	if (
		contentHeightBefore &&
		contentHeightAfter &&
		mapHeight &&
		contentHeightBefore > contentHeightAfter &&
		contentHeightAfter <= mapHeight
	) {
		window.scrollTo({ top: 0 });
	}
});

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
	if (!locale.value) return [];
	return [
		{
			title: t('component.estateSearch.breadcrumb'),
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

		if (route.query.roomId) {
			// Open room in the blueprint and focus it in the list
			await nextTick();
			roomList.value?.focusRoom(Number(route.query.roomId));
			buildingBlueprintRef.value?.openRoom(Number(route.query.roomId));
		}
	} catch (err) {
		ErrorService.onError({ err, errorPage: { visible: true } });
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

const focusRoomInList = (roomId: number | null) => {
	activeTab.value = DetailsTab.Rooms;
	roomList.value?.focusRoom(roomId);
};

const openRoomInBlueprint = async (roomId: number) => {
	activeMap.value = ActiveMapType.Blueprint;
	await nextTick();
	buildingBlueprintRef.value?.openRoom(roomId);
};

const selectMap = (type: ActiveMapType) => {
	activeMap.value = type;

	if (isMobile.value) {
		if (type === ActiveMapType.Map) {
			buildingMapRef.value?.openFullscreen();
		} else {
			buildingBlueprintRef.value?.openFullscreen();
		}
	}

	appInsights?.trackEvent({
		name:
			type === ActiveMapType.Map
				? 'EstateMapButtonClicked'
				: 'EstateBlueprintButtonClicked',
		properties: {
			isMobile: isMobile.value,
			buildingId: building.value?.id,
			buildingName: building.value?.name,
		},
	});
};

const { y } = useScroll(window);
</script>

<style scoped lang="scss">
.mobile-actions-divider {
	display: none;

	@media only screen and (max-width: $estate-mobile-threshold) {
		display: block;
	}
}
.mobile-actions {
	display: none;

	@media only screen and (max-width: $estate-mobile-threshold) {
		// One equal cell per action, however many the feature flags leave
		display: grid;
		grid-auto-flow: column;
		grid-auto-columns: minmax(0, 1fr);
		gap: 16px;

		:deep(.base-icon-button) {
			width: 100%;
		}
	}
}
.tabs-bar-action {
	@media only screen and (max-width: $estate-mobile-threshold) {
		display: none;
	}
}
.details-tabs-bar {
	border-bottom: solid 1px $grey-lighten-3;
	// Reversed so the wrapped actions button lands above the tabs
	flex-wrap: wrap-reverse;
	scroll-margin-top: $site-header-height;
	margin-top: 32px;

	@media only screen and (max-width: $estate-mobile-threshold) {
		margin-top: 8px;
	}
}
.details-tabs {
	// Floor under which the actions button wraps instead of the strip shrinking
	min-width: min(100%, 26rem);
	flex: 1 1 auto;

	:deep(.v-tab) {
		border-radius: $border-radius $border-radius 0 0;
		color: $grey-darken-3;
	}
	:deep(.v-tab--selected) {
		color: $primary;
		font-weight: bold;
	}
	// Reserves the bold width on every tab so selecting one moves nothing
	:deep(.tab-label) {
		display: inline-flex;
		flex-direction: column;
		align-items: center;
		white-space: nowrap;

		&::after {
			content: attr(data-label);
			height: 0;
			overflow: hidden;
			visibility: hidden;
			font-weight: bold;
		}
	}

	@media only screen and (max-width: $estate-mobile-threshold) {
		min-width: 100%;

		:deep(.v-tab) {
			flex: 1 1 0;
			min-width: 0;
			max-width: none;
		}
		:deep(.tab-label) {
			display: block;
			max-width: 100%;
			overflow: hidden;
			text-overflow: ellipsis;

			&::after {
				display: none;
			}
		}
	}

	@media only screen and (max-width: 490px) {
		:deep(.v-btn__prepend) {
			display: none;
		}
	}
}
.tab-panel {
	padding-top: 16px;
}
</style>
