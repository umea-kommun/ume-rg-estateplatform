<template>
	<div class="circle-button-toggles">
		<base-icon-button
			icon="location_pin"
			:label="$t('component.internal.buildingDetails.mapButton')"
			:active="activeMap === ActiveMapType.Map && !isMobile"
			@click="onMapClick"
		/>
		<base-icon-button
			icon="map"
			:label="$t('component.internal.buildingDetails.blueprintButton')"
			:tooltip="
				!building.blueprintAvailable
					? $t(
							'component.internal.buildingDetails.blueprintMissingTooltip'
					  )
					: undefined
			"
			:active="activeMap === ActiveMapType.Blueprint && !isMobile"
			:disabled="!building.blueprintAvailable"
			@click="onBlueprintClick"
		/>
		<base-icon-button
			v-if="isEnabled('ContactPersons')"
			icon="contacts"
			:label="
				$t('component.internal.buildingDetails.contactPersonsButton')
			"
			:tooltip="
				!contactPersonsCount
					? $t(
							'component.internal.buildingDetails.contactPersonsMissingTooltip'
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
			:label="$t('component.internal.buildingDetails.documentsButton')"
			:tooltip="
				building.numDocuments === 0
					? $t(
							'component.internal.buildingDetails.documentsMissingTooltip'
					  )
					: undefined
			"
			:count="building.numDocuments ?? undefined"
			:disabled="building.numDocuments === 0"
			@click="showDocumentModal = true"
		/>
		<base-icon-button
			v-if="isEnabled('ErrorReport')"
			icon="warning"
			icon-color="error"
			:label="$t('component.internal.buildingDetails.reportButton')"
			:to="{
				name: EstateRoutes.FaultReport,
				query: {
					buildingId: building.id,
				},
			}"
		/>

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
import { ActiveMapType } from '@/models/estate/Enums';
import { computed, ref } from 'vue';
import { useEstateIsMobile } from '../useEstateIsMobile';
import { IBuildingDetails } from '@/models/estate/Interfaces';
import BaseIconButton from '@/components/base/BaseIconButton.vue';
import BuildingContactModal from './BuildingContactModal.vue';
import BuildingDocumentModal from './BuildingDocumentModal.vue';
import { EstateRoutes } from '@/router/routes';
import { useFeatureFlags } from '@/utils/useFeatureFlags';

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
};

const onBlueprintClick = () => {
	if (isMobile.value) {
		emit('openBlueprintFullscreen');
	} else {
		activeMap.value = ActiveMapType.Blueprint;
	}
};

const contactPersonsCount = computed(() => {
	return Object.values(props.building?.contactPersons || {}).reduce(
		(count, value) => {
			if (value && value.trim() !== '') {
				return count + 1;
			}
			return count;
		},
		0
	);
});
</script>
