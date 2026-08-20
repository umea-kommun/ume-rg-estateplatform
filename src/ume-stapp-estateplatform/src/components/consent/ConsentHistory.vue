<template>
	<div class="consent-history">
		<base-image-modal />
		<h2 class="mt-2">
			{{ $t('component.external.consent.historyTitle') }}
		</h2>
		<div v-for="(history, index) in sortedConsentHistory" :key="index">
			<hr class="mb-4 mt-4" />
			<div class="d-flex flex-wrap justify-space-between align-center">
				<div class="mb-0">
					<p v-html="historyText(history)" class="ma-0"></p>
					<div
						class="d-flex flex-wrap align-center"
						v-if="history.agentName"
					>
						<v-icon
							icon="info"
							color="#444"
							:size="20"
							class="mr-2"
						/>
						<span class="mr-2">
							{{
								$t(
									'component.external.consent.usingConsentAgent',
									{
										agentName: history.agentName,
									}
								)
							}}
						</span>
						<v-btn
							class="mt-2"
							prepend-icon="image"
							variant="tonal"
							color="#444"
							:title="
								$t(
									'component.external.consent.openConsentAgentImageHelpText'
								)
							"
							@click="openHistoryImage(history)"
						>
							{{
								$t(
									'component.external.consent.openConsentAgentImage'
								)
							}}
						</v-btn>
					</div>
				</div>
				<p class="mb-0">
					{{ formatDateAndTime(history.created) }}
				</p>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { UserConsentStatus } from '@/models/Enums';
import { IGuardianConsentHistory } from '@/models/Interfaces';
import BaseImageModal from '@/components/base/baseImageModal/BaseImageModal.vue';
import { useBaseImageModal } from '@/components/base/baseImageModal/baseImageModal';
import moment from 'moment';
import { PropType, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import Config from '@/Config';

const props = defineProps({
	history: {
		required: true,
		type: Array as PropType<IGuardianConsentHistory[]>,
	},
	childName: {
		required: true,
		type: String,
	},
});

const { t } = useI18n();
const { showImageInModal } = useBaseImageModal();

const sortedConsentHistory = computed(() => {
	return props.history
		.slice()
		.sort((a, b) => (moment(b.created).isBefore(a.created) ? -1 : 1));
});

const historyText = (history: IGuardianConsentHistory) => {
	if (history.status === UserConsentStatus.Approved) {
		return t('component.external.consent.guardianGiveConsent', {
			guardianName: history.guardianName,
			childName: props.childName,
		});
	} else if (history.status === UserConsentStatus.Rejected) {
		return t('component.external.consent.guardianRejectConsent', {
			guardianName: history.guardianName,
			childName: props.childName,
		});
	}
	return '';
};

const formatDateAndTime = (date: string): string => {
	if (date) {
		return moment.utc(date).local().format('Do MMMM YYYY, HH:mm');
	}
	return '';
};

const openHistoryImage = (history: IGuardianConsentHistory) => {
	if (history.imageIdToken) {
		const fullImageUrl =
			Config.VUE_APP_CONSENT_BRIDGE_SERVICE_HISTORY_IMAGE +
			`?imageIdToken=${history.imageIdToken}`;

		showImageInModal(
			fullImageUrl,
			t('component.external.consent.imageOfGuardianSignature')
		);
	}
};
</script>

<style scoped lang="scss">
.consent-history {
	hr {
		margin: 16px 0;
		border: solid 1px $grey-lighten-3;
	}
}
</style>
