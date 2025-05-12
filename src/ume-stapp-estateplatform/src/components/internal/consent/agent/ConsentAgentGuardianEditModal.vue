<template>
	<v-dialog
		v-model="showModal"
		scrollable
		:max-width="fullScreenModal ? undefined : 820"
		:fullscreen="fullScreenModal"
		aria-labelledby="consent-title"
		aria-live="polite"
		:aria-busy="isBusySendingToServer"
		:retain-focus="!showingConfirmDialog"
		:persistent="true"
	>
		<v-card>
			<div class="content" :class="{ disabled: isBusySendingToServer }">
				<v-card-title>
					<div
						class="title"
						id="consent-title"
						ref="titleElement"
						tabindex="-1"
					>
						{{
							$t(
								'component.internal.consentAgentGuardianEditModal.title'
							)
						}}
					</div>
				</v-card-title>
				<v-card-text>
					<base-form-field
						id="label-guardian-answer"
						labelFor="guardian-answer"
						:label="
							$t(
								'component.internal.consentAgentGuardianEditModal.guardianNewAnswer'
							)
						"
						:is-required="true"
					>
						<v-radio-group
							id="guardian-answer"
							v-model="guardianNewAnswer"
							color="primary"
						>
							<v-radio
								:label="
									getConsentUserStatusText(
										UserConsentStatus.Approved
									)
								"
								:value="UserConsentStatus.Approved"
							></v-radio>
							<v-radio
								:label="
									getConsentUserStatusText(
										UserConsentStatus.Rejected
									)
								"
								:value="UserConsentStatus.Rejected"
							></v-radio>
						</v-radio-group>
					</base-form-field>
					<base-form-field
						id="label-guardian-photo"
						labelFor="guardian-photo"
						:label="
							$t(
								'component.internal.consentAgentGuardianEditModal.fileUploadLabel'
							)
						"
						:is-required="true"
						class="mt-6"
					>
						<v-file-input
							accept="image/*"
							variant="outlined"
							prepend-icon="camera_alt"
							v-model="guardianImage"
							color="primary"
						>
							<template v-slot:append-inner>
								<v-btn flat variant="tonal">{{
									$t(
										'component.internal.consentAgentGuardianEditModal.fileUploadSelectImage'
									)
								}}</v-btn>
							</template>
						</v-file-input>
						<base-help-and-error-text
							:helpText="
								$t(
									'component.internal.consentAgentGuardianEditModal.fileUploadHelpText'
								)
							"
						/>
					</base-form-field>
					<v-alert
						icon="info"
						v-if="guardianNewAnswer && guardianImage?.length"
						class="mt-4"
					>
						{{
							$t(
								'component.internal.consentAgentGuardianEditModal.alertPart1'
							)
						}}<b>{{ guardianName }}</b
						>{{
							$t(
								'component.internal.consentAgentGuardianEditModal.alertPart2'
							)
						}}
						<b>{{ getConsentUserStatusText(guardianNewAnswer) }}</b>
						{{
							$t(
								'component.internal.consentAgentGuardianEditModal.alertPart3'
							)
						}}
						<b>{{ consent.childName }}.</b>
					</v-alert>
				</v-card-text>
			</div>
			<v-card-actions>
				<hr class="mb-4 mt-4" />
				<v-btn @click="showModal = false">{{
					$t('app.nav.cancel')
				}}</v-btn>
				<v-spacer />
				<v-btn
					@click="updateGuardianAnswer"
					variant="outlined"
					color="primary"
					:disabled="!guardianNewAnswer || !guardianImage?.length"
					:loading="isBusySendingToServer"
					>{{
						$t(
							'component.internal.consentAgentGuardianEditModal.updateButton'
						)
					}}</v-btn
				>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { IAgentConsent, IRootState } from '@/models/Interfaces';
import { PropType, computed, watch } from 'vue';
import { useDisplay } from 'vuetify';
import { ref } from 'vue';
import { UserConsentStatus } from '@/models/Enums';
import { getConsentUserStatusText } from '@/utils/utils';
import BaseFormField from '@/components/base/BaseFormField.vue';
import BaseHelpAndErrorText from '@/components/base/BaseHelpAndErrorText.vue';
import { useTConfirmDialog } from '@turkos/components';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import { compressImageFile } from '@/utils/imageUtils';
import ErrorService from '@/utils/ErrorService';

const props = defineProps({
	modelValue: { type: Boolean, required: true },
	consent: {
		type: Object as PropType<IAgentConsent>,
		required: true,
	},
	guardianSSN: {
		type: String,
		required: true,
	},
});

const { t } = useI18n();
const emit = defineEmits(['update:modelValue', 'answerChanged']);
const store = useStore<IRootState>();

const { width } = useDisplay();
const fullScreenModal = computed(() => width.value < 820);
const showModal = computed({
	get: () => props.modelValue,
	set: (show) => emit('update:modelValue', show),
});

const isBusySendingToServer = ref<boolean>(false);
const guardianNewAnswer = ref<UserConsentStatus>();
const guardianImage = ref<File[]>([]);
const showingConfirmDialog = ref(false);

watch(
	() => showModal.value,
	() => {
		if (!showModal.value) {
			// Reset fields when closed
			guardianNewAnswer.value = undefined;
			guardianImage.value = [];
		}
	}
);

const guardianName = computed(() => {
	return props.consent.linkedPersons.find(
		(person) => person.socialSecurityNumber === props.guardianSSN
	)?.name;
});

const { tConfirmDialogAsync } = useTConfirmDialog();
const updateGuardianAnswer = async () => {
	if (
		guardianNewAnswer.value &&
		guardianImage.value.length &&
		!isBusySendingToServer.value
	) {
		showingConfirmDialog.value = true;
		const confirmed = await tConfirmDialogAsync(
			t(
				'component.internal.consentAgentGuardianEditModal.updateConsentConfirmTitle'
			),
			t(
				'component.internal.consentAgentGuardianEditModal.updateConsentConfirmText',
				{
					guardianName: guardianName.value,
					childName: props.consent.childName,
					consentStatus: getConsentUserStatusText(
						guardianNewAnswer.value
					),
					templateTitle: props.consent.title,
				}
			),
			{
				text: t(
					'component.internal.consentAgentGuardianEditModal.updateConsentConfirmButton'
				),
			}
		);
		showingConfirmDialog.value = false;
		if (confirmed) {
			isBusySendingToServer.value = true;
			try {
				const compressedImage = await compressImageFile(
					guardianImage.value[0]
				);

				const updatedConsent = await store.dispatch(
					'agentUpdateConsent',
					{
						templateGuid: props.consent.templateGuid,
						childSSN: props.consent.childSSNo,
						guardianStatus: guardianNewAnswer.value,
						guardianSSN: props.guardianSSN,
						image: compressedImage,
						stamp: 'Stamp from BankId',
						signType: store.state.user.idp,
					}
				);
				if (updatedConsent) {
					emit('answerChanged', updatedConsent);
					showModal.value = false;
				}
			} catch (err) {
				ErrorService.onError({ err });
			}
			isBusySendingToServer.value = false;
		}
	}
};
</script>

<style scoped lang="scss">
.content {
	flex: 1;

	&.disabled {
		opacity: 0.5;
		pointer-events: none;
	}

	.v-card-text {
		padding-left: 20px;
		padding-right: 20px;
	}
}
</style>
