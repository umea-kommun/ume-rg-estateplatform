<template>
	<location-selector
		:options="options"
		:selected="problemLocation"
		@select="emit('select', $event as EstateFaultLocation | null)"
	/>
</template>

<script setup lang="ts">
/**
 * Fault-report wording for the shared indoor/outdoor LocationSelector. The generic
 * tile/collapse behaviour lives in LocationSelector; this just supplies the fault
 * copy and maps the value back to EstateFaultLocation.
 */
import { EstateFaultLocation } from '@/models/Enums';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import LocationSelector from '../order/LocationSelector.vue';
import type { LocationOption } from '../order/LocationSelector.vue';

defineProps<{
	problemLocation: EstateFaultLocation | null;
}>();

const emit = defineEmits(['select']);

const { t } = useI18n();

const options = computed<LocationOption[]>(() => [
	{
		value: EstateFaultLocation.Indoor,
		icon: 'meeting_room',
		title: t('component.faultReport.location.indoor.select'),
		description: t('component.faultReport.location.indoor.description'),
		selected: t('component.faultReport.location.indoor.selected'),
	},
	{
		value: EstateFaultLocation.Outdoor,
		icon: 'park',
		title: t('component.faultReport.location.outdoor.select'),
		description: t('component.faultReport.location.outdoor.description'),
		selected: t('component.faultReport.location.outdoor.selected'),
	},
]);
</script>
