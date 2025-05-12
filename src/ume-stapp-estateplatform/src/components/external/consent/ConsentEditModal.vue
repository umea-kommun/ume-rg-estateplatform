<template>
	<v-dialog
		v-model="showModal"
		scrollable
		:max-width="fullScreenModal ? undefined : 820"
		:fullscreen="fullScreenModal"
		aria-labelledby="consent-title"
		aria-describedby="consent-text"
		aria-live="polite"
		:aria-busy="isBusyLoadingFromServer"
	>
		<v-card class="consent-edit">
			<div class="loading-wrap" v-if="isBusyLoadingFromServer">
				<v-progress-circular color="primary" indeterminate />
				<div
					ref="loaderElement"
					tabindex="-1"
					class="mt-4"
					aria-busy="true"
					aria-live="polite"
				>
					{{ $t('component.external.consent.loading') }}
				</div>
			</div>
			<div
				v-if="!isBusyLoadingFromServer && guardianConsent"
				class="content"
			>
				<v-card-title>
					<div
						class="title"
						id="consent-title"
						ref="titleElement"
						tabindex="-1"
					>
						{{ $t('component.external.consent.consentTitle') }}
						-
						{{ guardianConsent.title }}
					</div>
				</v-card-title>
				<v-card-text id="consent-text">
					<hr class="mb-2 mt-2 ml-3 mr-3" aria-hidden="true" />
					<v-row>
						<!-- eslint-disable-next-line vue/no-v-text-v-html-on-component -->
						<v-col cols="12" v-html="guardianConsent.data"></v-col>
					</v-row>

					<hr class="mb-4 mt-4 ml-3 mr-3" aria-hidden="true" />
					<v-row>
						<v-col cols="12">
							{{
								$t(
									'component.external.consent.handleConsentFor'
								)
							}}
							<b>{{ guardianConsent.childName }}.</b>
						</v-col>
					</v-row>
					<hr class="mb-4 mt-4 ml-3 mr-3" aria-hidden="true" />
					<v-row v-if="hasAnswered">
						<v-col class="text-center">
							<h3 class="mt-4 mb-8">
								{{
									hasGivenConsent
										? $t(
												'component.external.consent.youConsentTo'
										  )
										: $t(
												'component.external.consent.youDoNotConsentTo'
										  )
								}}
								{{ guardianConsent.title }}
							</h3>

							<p>
								{{
									isExpiredConsent
										? $t(
												'component.external.consent.youCanNoLongerChangeAnswer'
										  )
										: $t(
												'component.external.consent.youCanChangeYourAnswerLater'
										  )
								}}
							</p>
						</v-col>
					</v-row>
					<v-row class="mt-0">
						<v-col
							class="text-center mb-4 mt-4"
							:style="
								isBusySendingToServer
									? { pointerEvents: 'none' }
									: undefined
							"
						>
							<v-btn
								v-if="!isExpiredConsent || hasGivenConsent"
								id="consent-button"
								:disabled="hasGivenConsent"
								:prependIcon="
									hasGivenConsent ? 'check_circle' : ''
								"
								:color="
									hasGivenConsent ? 'darkgrey' : 'primary'
								"
								flat
								size="large"
								@click="
									manageConsent(
										UserConsentStatus.Approved,
										true
									)
								"
								:loading="
									isBusySendingToServer && sendingGivenConsent
								"
								>{{
									$t('component.external.consent.giveConsent')
								}}</v-btn
							>
							<v-btn
								v-if="!isExpiredConsent || hasRejectedConsent"
								id="reject-button"
								:disabled="hasRejectedConsent"
								:prependIcon="
									hasRejectedConsent ? 'check_circle' : ''
								"
								:color="
									hasRejectedConsent ? 'darkgrey' : 'error'
								"
								flat
								size="large"
								@click="
									manageConsent(
										UserConsentStatus.Rejected,
										false
									)
								"
								:loading="
									isBusySendingToServer &&
									!sendingGivenConsent
								"
								>{{
									$t(
										'component.external.consent.rejectConsent'
									)
								}}</v-btn
							>
						</v-col>
					</v-row>
					<v-row v-if="!hasAnswered && !isExpiredConsent">
						<v-col class="mb-6 text-center">{{
							$t('component.external.consent.youCanChangeLater')
						}}</v-col>
					</v-row>
					<v-row v-if="guardianConsent.consentHistory.length > 0">
						<v-col cols="12">
							<consent-history
								:history="guardianConsent.consentHistory"
								:child-name="guardianConsent.childName"
							/>
						</v-col>
					</v-row>
				</v-card-text>
			</div>
			<v-card-actions>
				<hr class="mb-4 mt-4" />
				<v-btn @click="showModal = false">{{
					$t('app.nav.close')
				}}</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { UserConsentStatus } from '@/models/Enums';
import { useStore } from 'vuex';
import { useDisplay } from 'vuetify';
import { IChildConsentRequest, IRootState } from '@/models/Interfaces';
import ConsentHistory from '@/components/consent/ConsentHistory.vue';

const store = useStore<IRootState>();
const isBusyLoadingFromServer = ref<boolean>(true);
const isBusySendingToServer = ref<boolean>(false);
const sendingGivenConsent = ref<boolean>();

const props = defineProps({
	modelValue: { type: Boolean, required: true },
	childSSNo: {
		type: String,
		required: true,
	},
	templateGuid: {
		type: String,
		required: true,
	},
});
const emit = defineEmits(['update:modelValue', 'answerChanged']);

const showModal = computed({
	get: () => props.modelValue,
	set: (show) => emit('update:modelValue', show),
});
const { width } = useDisplay();
const fullScreenModal = computed(() => width.value < 820);

const guardianConsent = computed(() => {
	return store.state.guardianConsent;
});

const hasAnswered = computed(() => {
	return guardianConsent.value?.linkedPersons.find(
		(person) =>
			person.socialSecurityNumber ===
			store.state.user.socialSecurityNumber
	)?.userStatus
		? true
		: false;
});

const hasGivenConsent = computed(() => {
	const answered = guardianConsent.value?.linkedPersons.find(
		(person) =>
			person.socialSecurityNumber ===
			store.state.user.socialSecurityNumber
	)?.userStatus;

	if (answered === UserConsentStatus.Approved) {
		return true;
	} else {
		return false;
	}
});

const hasRejectedConsent = computed(() => {
	const answered = guardianConsent.value?.linkedPersons.find(
		(person) =>
			person.socialSecurityNumber ===
			store.state.user.socialSecurityNumber
	)?.userStatus;

	if (answered === UserConsentStatus.Rejected) {
		return true;
	} else {
		return false;
	}
});

const isExpiredConsent = computed(() => {
	return !guardianConsent.value?.isActive;
});

const manageConsent = async (
	guardianStatus: UserConsentStatus,
	isSendingGivenConsent: boolean
): Promise<void> => {
	if (
		guardianConsent.value &&
		!isExpiredConsent.value &&
		!isBusySendingToServer.value
	) {
		isBusySendingToServer.value = true;
		sendingGivenConsent.value = isSendingGivenConsent;
		await store.dispatch('updateConsent', {
			templateGuid: guardianConsent.value.templateGuid,
			childSSN: guardianConsent.value.childSSNo,
			guardianStatus: guardianStatus,
			stamp: 'Stamp from BankId',
			signType: store.state.user.idp,
		});
		emit('answerChanged');
		isBusySendingToServer.value = false;
	}
};

const loaderElement = ref<HTMLElement | null>(null);
const titleElement = ref<HTMLElement | null>(null);
onMounted(async () => {
	loaderElement.value?.focus();
	isBusyLoadingFromServer.value = true;
	const childConsentRequest: IChildConsentRequest = {
		childSSNo: props.childSSNo,
		templateGuid: props.templateGuid,
	};
	await store.dispatch('getConsent', { childConsentRequest });

	isBusyLoadingFromServer.value = false;

	setTimeout(() => {
		titleElement.value?.focus();
	});
});
</script>

<style scoped lang="scss">
.consent-edit {
	.v-col {
		font-size: size(16);

		.v-btn {
			text-transform: none;
			letter-spacing: normal;
			--v-btn-height: 50px;
			padding-left: 48px;
			padding-right: 48px;

			&--disabled {
				background-color: $grey-lighten-3;
				opacity: 1;
				padding-left: 28px;

				:deep(.v-btn__content, .v-btn__prepend) {
					color: $black;
				}
				:deep(.v-btn__prepend) {
					color: $grey-darken-2;
				}
			}
			&:focus {
				outline: solid 2px $grey-darken-3;
			}
		}
	}

	.title {
		font-size: size(28);
		line-height: size(28);
		font-weight: bold;
		padding: 12px;
		white-space: normal;
	}
	h2 {
		font-size: size(22);
		line-height: size(22);
	}
	h3 {
		font-size: size(18);
		line-height: size(18);
	}

	hr {
		border: solid 1px $grey-lighten-3;
	}

	.loading-wrap {
		padding: 100px 0;
		text-align: center;
	}
	.v-card-actions {
		display: flex;
		justify-content: flex-end;
		box-shadow: 0px 0px 5px 0px rgba(0, 0, 0, 0.2);
	}

	.content {
		padding-bottom: 30px;
		display: flex;
		flex-direction: column;
		flex: 1;
		overflow-y: auto;
		max-height: calc(80vh - 100px);
	}
}
</style>
<style lang="scss">
.v-dialog--fullscreen .consent-edit .content {
	max-height: none;
}
</style>
