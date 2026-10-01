<template>
	<v-menu v-if="isEnabled('ErrorReport')">
		<template v-slot:activator="{ props }">
			<base-icon-button
				v-if="isMobile"
				v-bind="props"
				active
				icon="add"
				:label="$t('component.buildingDetails.createCase')"
				@click="trackMenuOpened"
			/>
			<v-btn
				v-else
				v-bind="props"
				class="create-case-btn"
				variant="text"
				color="primary"
				prepend-icon="add"
				height="48"
				@click="trackMenuOpened"
			>
				{{ $t('component.buildingDetails.createCase') }}
			</v-btn>
		</template>
		<v-list class="create-case-list">
			<v-list-item
				v-for="action in visibleActions"
				:key="action.key"
				:prepend-icon="action.icon"
				:disabled="!action.enabled"
				:to="
					action.enabled
						? {
								name: action.route,
								query: { buildingId: building.id },
						  }
						: undefined
				"
				@click="trackAction(action.key)"
				:title="action.title"
				:subtitle="action.description"
			/>
		</v-list>
	</v-menu>
</template>

<script setup lang="ts">
import { EstateOrderCategory } from '@/models/Enums';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { IBuildingDetails } from '@/models/Interfaces';
import { EstateRoutes } from '@/router/routes';
import { appInsights } from '@/plugins/appInsights';
import { useFeatureFlags } from '@/utils/useFeatureFlags';
import { useEstateIsMobile } from '../useEstateIsMobile';
import BaseIconButton from '@/components/shared/BaseIconButton.vue';

const { t } = useI18n();
const { isEnabled } = useFeatureFlags();
const isMobile = useEstateIsMobile();

const props = defineProps<{
	building: IBuildingDetails;
}>();

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

const ERROR_REPORT_TYPE = 'errorReport';

const ORDER_TYPES = [
	EstateOrderCategory.BuildingService,
	EstateOrderCategory.TownHallService,
	EstateOrderCategory.FacilityService,
];

// A type the user may not use at all is absent from the map; a present one is
// 'enabled' or 'disabled' for this building.
const workOrderTypeAccess = computed(() => props.building.workOrderTypeAccess);

const hasAccessMap = computed(
	() => Object.keys(workOrderTypeAccess.value).length > 0
);

const isPermitted = (type: string) =>
	!hasAccessMap.value || type in workOrderTypeAccess.value;

const isTypeEnabled = (type: string) =>
	!hasAccessMap.value || workOrderTypeAccess.value[type] === 'enabled';

interface BuildingAction {
	key: 'faultReport' | 'order' | 'spaceRequirement';
	route: EstateRoutes;
	icon: string;
	title: string;
	description: string;
	enabled: boolean;
}

const visibleActions = computed<BuildingAction[]>(() => {
	const actions: BuildingAction[] = [];

	if (isPermitted(ERROR_REPORT_TYPE)) {
		actions.push({
			key: 'faultReport',
			route: EstateRoutes.FaultReport,
			icon: 'warning',
			title: t('component.estatePortal.actions.faultReport.title'),
			description: t(
				'component.estatePortal.actions.faultReport.description'
			),
			enabled: isTypeEnabled(ERROR_REPORT_TYPE),
		});
	}

	if (ORDER_TYPES.some(isPermitted)) {
		actions.push({
			key: 'order',
			route: EstateRoutes.Order,
			icon: 'handyman',
			title: t('component.estatePortal.actions.order.title'),
			description: t('component.estatePortal.actions.order.description'),
			enabled: ORDER_TYPES.some(isTypeEnabled),
		});
	}

	if (isPermitted(EstateOrderCategory.SpaceRequirement)) {
		actions.push({
			key: 'spaceRequirement',
			route: EstateRoutes.SpaceRequirement,
			icon: 'space_dashboard',
			title: t('component.estatePortal.actions.spaceRequirement.title'),
			description: t(
				'component.estatePortal.actions.spaceRequirement.description'
			),
			enabled: isTypeEnabled(EstateOrderCategory.SpaceRequirement),
		});
	}

	return actions;
});
</script>

<style scoped lang="scss">
.create-case-btn {
	border-radius: $border-radius $border-radius 0 0;
}

.create-case-list {
	// The menu sizes itself from the activator, too narrow for items with
	// descriptions
	width: 380px;
	max-width: calc(100vw - 32px);

	// The activator is now a third of the action row, too narrow to size from
	@media only screen and (max-width: $estate-mobile-threshold) {
		width: calc(100vw - 48px);
		max-width: none;
	}

	:deep(.v-list-item-subtitle) {
		display: block; /* Text can be multiline */
	}
	:deep(.v-list-item) {
		padding-top: 8px;
		padding-bottom: 8px;
	}
}
</style>
