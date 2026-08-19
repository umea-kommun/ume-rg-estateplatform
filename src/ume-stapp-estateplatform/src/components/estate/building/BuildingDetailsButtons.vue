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
			:disabled="building.numDocuments === 0"
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
					prepend-icon="warning"
					:to="{
						name: EstateRoutes.FaultReport,
						query: { buildingId: building.id },
					}"
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
					prepend-icon="handyman"
					:to="{
						name: EstateRoutes.Order,
						query: { buildingId: building.id },
					}"
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
					prepend-icon="space_dashboard"
					:to="{
						name: EstateRoutes.SpaceRequirement,
						query: { buildingId: building.id },
					}"
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
import { ActiveMapType } from '@/models/Enums';
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
	max-width: 360px;

	:deep(.v-list-item-subtitle) {
		white-space: normal;
	}
	:deep(.v-list-item) {
		padding-top: 8px;
		padding-bottom: 8px;
	}
}
</style>
