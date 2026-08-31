<template>
	<div class="circle-button-toggles">
		<base-icon-button
			icon="location_pin"
			:label="$t('component.buildingDetails.mapButton')"
			:active="activeMap === ActiveMapType.Map && !isMobile"
			@click="onMapClick"
		/>
		<base-icon-button
			icon="map"
			:label="$t('component.buildingDetails.blueprintButton')"
			:tooltip="
				!building.blueprintAvailable
					? $t('component.buildingDetails.blueprintMissingTooltip')
					: undefined
			"
			:active="activeMap === ActiveMapType.Blueprint && !isMobile"
			:disabled="!building.blueprintAvailable"
			@click="onBlueprintClick"
		/>
		<base-icon-button
			v-if="isEnabled('ContactPersons')"
			icon="contacts"
			:label="$t('component.buildingDetails.contactPersonsButton')"
			:tooltip="
				!contactPersonsCount
					? $t(
							'component.buildingDetails.contactPersonsMissingTooltip'
					  )
					: undefined
			"
			:count="contactPersonsCount"
			:disabled="!contactPersonsCount"
			@click="showContactPersonsModal = true"
		/>
		<base-icon-button
			v-if="isEnabled('Documents')"
			icon="insert_drive_file"
			:label="$t('component.buildingDetails.documentsButton')"
			:tooltip="
				building.numDocuments === 0
					? $t('component.buildingDetails.documentsMissingTooltip')
					: undefined
			"
			:count="building.numDocuments ?? undefined"
			@click="showDocumentModal = true"
		/>
		<!--
			One "create case" entry instead of three separate circles: the
			view buttons stay one group and the work order actions live in a
			menu, each deep-linking with the building prefilled.
		-->
		<v-menu attach v-if="isEnabled('ErrorReport')">
			<template v-slot:activator="{ props }">
				<base-icon-button
					v-bind="props"
					icon="add"
					active
					:label="$t('component.buildingDetails.createCase')"
					@click="trackMenuOpened"
				/>
			</template>
			<v-list class="create-case-list">
				<v-list-item
					v-if="isPermitted('errorReport')"
					prepend-icon="warning"
					:disabled="!canFaultReport"
					:to="
						canFaultReport
							? {
									name: EstateRoutes.FaultReport,
									query: { buildingId: building.id },
							  }
							: undefined
					"
					@click="trackAction('faultReport')"
				>
					<v-list-item-title>{{
						$t('component.estatePortal.actions.faultReport.title')
					}}</v-list-item-title>
					<v-list-item-subtitle>{{
						$t(
							'component.estatePortal.actions.faultReport.description'
						)
					}}</v-list-item-subtitle>
				</v-list-item>
				<v-list-item
					v-if="isOrderPermitted"
					prepend-icon="handyman"
					:disabled="!canOrder"
					:to="
						canOrder
							? {
									name: EstateRoutes.Order,
									query: { buildingId: building.id },
							  }
							: undefined
					"
					@click="trackAction('order')"
				>
					<v-list-item-title>{{
						$t('component.estatePortal.actions.order.title')
					}}</v-list-item-title>
					<v-list-item-subtitle>{{
						$t('component.estatePortal.actions.order.description')
					}}</v-list-item-subtitle>
				</v-list-item>
				<v-list-item
					v-if="isPermitted(EstateOrderCategory.SpaceRequirement)"
					prepend-icon="space_dashboard"
					:disabled="!canSpaceRequirement"
					:to="
						canSpaceRequirement
							? {
									name: EstateRoutes.SpaceRequirement,
									query: { buildingId: building.id },
							  }
							: undefined
					"
					@click="trackAction('spaceRequirement')"
				>
					<v-list-item-title>{{
						$t(
							'component.estatePortal.actions.spaceRequirement.title'
						)
					}}</v-list-item-title>
					<v-list-item-subtitle>{{
						$t(
							'component.estatePortal.actions.spaceRequirement.description'
						)
					}}</v-list-item-subtitle>
				</v-list-item>
			</v-list>
		</v-menu>

		<building-contact-modal
			v-if="building && isEnabled('ContactPersons')"
			v-model="showContactPersonsModal"
			:building="building"
		/>
		<building-document-modal
			v-if="building && isEnabled('Documents')"
			v-model="showDocumentModal"
			:building="building"
		/>
	</div>
</template>

<script setup lang="ts">
import { ActiveMapType, EstateOrderCategory } from '@/models/Enums';
import { computed, ref } from 'vue';
import { useEstateIsMobile } from '../useEstateIsMobile';
import { IBuildingDetails } from '@/models/Interfaces';
import BaseIconButton from '@/components/shared/BaseIconButton.vue';
import BuildingContactModal from './BuildingContactModal.vue';
import BuildingDocumentModal from './BuildingDocumentModal.vue';
import { EstateRoutes } from '@/router/routes';
import { useFeatureFlags } from '@/utils/useFeatureFlags';
import { appInsights } from '@/plugins/appInsights';

const { isEnabled } = useFeatureFlags();

const props = defineProps<{
	building: IBuildingDetails;
	activeMap: ActiveMapType;
}>();

const emit = defineEmits([
	'update:activeMap',
	'openMapFullscreen',
	'openBlueprintFullscreen',
]);

const isMobile = useEstateIsMobile();

const showDocumentModal = ref(false);
const showContactPersonsModal = ref(false);

const activeMap = computed({
	get: () => props.activeMap,
	set: (value: ActiveMapType) => emit('update:activeMap', value),
});

const onMapClick = () => {
	if (isMobile.value) {
		emit('openMapFullscreen');
	} else {
		activeMap.value = ActiveMapType.Map;
	}
	appInsights?.trackEvent({
		name: 'EstateMapButtonClicked',
		properties: {
			isMobile: isMobile.value,
			buildingId: props.building.id,
			buildingName: props.building.name,
		},
	});
};

const onBlueprintClick = () => {
	if (isMobile.value) {
		emit('openBlueprintFullscreen');
	} else {
		activeMap.value = ActiveMapType.Blueprint;
	}
	appInsights?.trackEvent({
		name: 'EstateBlueprintButtonClicked',
		properties: {
			isMobile: isMobile.value,
			buildingId: props.building.id,
			buildingName: props.building.name,
		},
	});
};

const trackMenuOpened = () => {
	appInsights?.trackEvent({
		name: 'EstateCreateCaseMenuOpened',
		properties: {
			buildingId: props.building.id,
			buildingName: props.building.name,
		},
	});
};

const trackAction = (type: string) => {
	appInsights?.trackEvent({
		name: 'EstateBuildingActionClicked',
		properties: {
			type,
			buildingId: props.building.id,
			buildingName: props.building.name,
		},
	});
};

const ORDER_TYPES = [
	EstateOrderCategory.BuildingService,
	EstateOrderCategory.TownHallService,
	EstateOrderCategory.FacilityService,
];

// The API resolves access per type: a type the user may not use at all is
// absent from the map and gets no menu entry, while a present one is enabled or
// disabled depending on whether this building offers it - disabled still shows,
// greyed, so the user sees the action exists without reaching a submit the API
// would reject. Externally owned buildings keep felanmälan enabled; the form
// warns about the landlord and lets the user send it anyway.
const workOrderTypeAccess = computed(
	() => props.building?.workOrderTypeAccess ?? {}
);

// A response without the map (an older API) leaves every entry on offer rather
// than emptying the menu.
const hasAccessMap = computed(
	() => Object.keys(workOrderTypeAccess.value).length > 0
);

const isPermitted = (type: string) =>
	!hasAccessMap.value || type in workOrderTypeAccess.value;

const isTypeEnabled = (type: string) =>
	!hasAccessMap.value || workOrderTypeAccess.value[type] === 'enabled';

const isOrderPermitted = computed(() => ORDER_TYPES.some(isPermitted));

const canFaultReport = computed(() => isTypeEnabled('errorReport'));

const canOrder = computed(() => ORDER_TYPES.some(isTypeEnabled));

const canSpaceRequirement = computed(() =>
	isTypeEnabled(EstateOrderCategory.SpaceRequirement)
);

const contactPersonsCount = computed(() => {
	return Object.values(props.building?.contactPersons || {}).reduce(
		(count, contact) => {
			if (contact && contact.name?.trim() !== '') {
				return count + 1;
			}
			return count;
		},
		0
	);
});
</script>

<style scoped lang="scss">
.create-case-list {
	// The menu sizes itself from the circle activator, which is far too
	// narrow for items with descriptions - give it a firm width instead,
	// capped to the viewport on small screens.
	width: 380px;
	max-width: calc(100vw - 32px);

	:deep(.v-list-item-subtitle) {
		white-space: normal;
	}
	:deep(.v-list-item) {
		padding-top: 8px;
		padding-bottom: 8px;
	}
}
</style>
