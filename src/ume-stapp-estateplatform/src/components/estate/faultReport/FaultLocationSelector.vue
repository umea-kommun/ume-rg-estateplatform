<template>
	<div class="fault-location-selector">
		<div v-if="problemLocation">
			<p class="text-medium-emphasis">
				<span v-if="problemLocation === EstateFaultLocation.Indoor">
					{{
						$t(
							'component.faultReport.location.indoor.selected'
						)
					}}
				</span>
				<span v-else>
					{{
						$t(
							'component.faultReport.location.outdoor.selected'
						)
					}}
				</span>
			</p>
		</div>
		<div v-else>
			<div class="location-wrap py-2 d-flex ga-4">
				<v-card
					v-for="location in locations"
					:key="location.type"
					@click="emit('select', location.type)"
				>
					<v-icon
						:icon="location.icon"
						:size="32"
						class="mt-6"
						color="info"
					/>
					<v-card-title>
						{{ location.title }}
					</v-card-title>
					<v-card-text>
						{{ location.description }}
					</v-card-text>
					<div class="d-flex justify-center">
						<v-btn class="mb-4" color="primary" variant="tonal">
							{{ location.buttonText }}
						</v-btn>
					</div>
				</v-card>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { EstateFaultLocation } from '@/models/Enums';
import { useI18n } from 'vue-i18n';

defineProps<{
	problemLocation: EstateFaultLocation | null;
}>();

const emit = defineEmits(['select']);

const { t } = useI18n();

const locations = [
	{
		type: EstateFaultLocation.Indoor,
		icon: 'meeting_room',
		title: t('component.faultReport.location.indoor.select'),
		description: t(
			'component.faultReport.location.indoor.description'
		),
		buttonText: t(
			'component.faultReport.location.indoor.selectButton'
		),
	},
	{
		type: EstateFaultLocation.Outdoor,
		icon: 'park',
		title: t('component.faultReport.location.outdoor.select'),
		description: t(
			'component.faultReport.location.outdoor.description'
		),
		buttonText: t(
			'component.faultReport.location.outdoor.selectButton'
		),
	},
];
</script>

<style lang="scss" scoped>
.fault-location-selector {
	.location-wrap {
		.v-card {
			flex: 1;
			text-align: center;
			display: flex;
			flex-direction: column;
			align-items: center;

			.v-card-text {
				flex: 1;
			}
		}
		@media only screen and (max-width: 620px) {
			flex-wrap: wrap;
			.v-card {
				flex: auto;
			}
		}
	}
}
</style>
