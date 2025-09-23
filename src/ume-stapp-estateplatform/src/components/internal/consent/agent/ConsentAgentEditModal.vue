<template>
	<v-dialog
		v-model="showDialog"
		:max-width="fullScreenModal ? undefined : 800"
		:fullscreen="fullScreenModal"
	>
		<v-card class="consent-agent-edit" :title="consentTitle">
			<app-loading-spinner
				v-if="isBusyLoadingFromServer"
				:isVisible="true"
			/>
			<v-card-text v-else-if="consentData">
				<v-row>
					<v-col>
						<BaseExpandableContent>
							<p v-html="consentData.data"></p>
						</BaseExpandableContent>
					</v-col>
				</v-row>
				<hr aria-hidden="true" />

				<h2 class="mt-10">
					{{
						$t('component.internal.consentAgentEdit.guardianTitle')
					}}
				</h2>
				<v-alert class="mt-2" icon="info">
					{{
						$t(
							'component.internal.consentAgentEdit.handleConsentFor'
						)
					}}
					<b>{{ consentData.childName }}.</b>
				</v-alert>
				<v-row>
					<v-col>
						<div
							v-for="linkedPerson in consentData.currentlyResponsiblePersons ??
							consentData.linkedPersons"
							:key="linkedPerson.socialSecurityNumber"
							class="linked-person pb-2 pt-2"
						>
							<div class="name-status">
								<b class="mr-4">{{ linkedPerson.name }}</b>
								<v-chip
									:class="{
										approved:
											linkedPerson.userStatus ===
											UserConsentStatus.Approved,
										denied:
											linkedPerson.userStatus ===
											UserConsentStatus.Rejected,
									}"
									class="mt-2 mb-2"
									variant="outlined"
								>
									{{
										getConsentUserStatusText(
											linkedPerson.userStatus
										)
									}}
								</v-chip>
							</div>
							<v-btn
								size="large"
								variant="outlined"
								class="regular-text"
								:disabled="!consentData.isActive"
								@click="
									guardianEditSSN =
										linkedPerson.socialSecurityNumber
								"
								>{{
									$t(
										'component.internal.consentAgentList.answer'
									)
								}}</v-btn
							>
						</div>
					</v-col>
				</v-row>

				<div v-if="consentData.currentlyResponsiblePersons">
					<h3 class="mt-10">
						{{
							$t(
								'component.internal.consentAgentEdit.responsiblesHaveChangedTitle'
							)
						}}
					</h3>
					<v-alert class="mt-2" icon="info">
						{{
							$t(
								'component.internal.consentAgentEdit.responsiblesHaveChangedInfo'
							)
						}}
					</v-alert>
					<v-row>
						<v-col>
							<div
								v-for="linkedPerson in consentData.linkedPersons"
								:key="linkedPerson.socialSecurityNumber"
								class="linked-person pb-2 pt-2"
							>
								<div class="name-status">
									<b class="mr-4">{{ linkedPerson.name }}</b>
									<v-chip
										:class="{
											approved:
												linkedPerson.userStatus ===
												UserConsentStatus.Approved,
											denied:
												linkedPerson.userStatus ===
												UserConsentStatus.Rejected,
										}"
										class="mt-2 mb-2"
										variant="outlined"
									>
										{{
											getConsentUserStatusText(
												linkedPerson.userStatus
											)
										}}
									</v-chip>
								</div>
							</div>
						</v-col>
					</v-row>
				</div>

				<v-row v-if="consentData.consentHistory.length > 0">
					<v-col cols="12">
						<consent-history
							:history="consentData.consentHistory"
							:child-name="consentData.childName"
						/>
					</v-col>
				</v-row>

				<consent-agent-guardian-edit-modal
					v-model="showEditGuardianAnswerModal"
					:guardianSSN="guardianEditSSN ?? ''"
					:consent="consentData"
					@answer-changed="
						(newConsentData) => (consentData = newConsentData)
					"
				/> </v-card-text
			><v-card-actions>
				<v-spacer></v-spacer>
				<v-btn
					:text="
						$t('component.internal.kvittensAgent.closeDialogButton')
					"
					@click="showDialog = false"
				></v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { IAgentConsent, IRootState } from '@/models/Interfaces';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { ref, watch } from 'vue';
import { computed } from 'vue';
import { useStore } from 'vuex';
import { UserConsentStatus } from '@/models/Enums';
import { getConsentUserStatusText } from '@/utils/utils';
import ConsentHistory from '@/components/consent/ConsentHistory.vue';
import ConsentAgentGuardianEditModal from './ConsentAgentGuardianEditModal.vue';
import { useDisplay } from 'vuetify';
import BaseExpandableContent from '@/components/base/BaseExpandableContent.vue';

const props = defineProps<{
	modelValue: {
		childSSNo: string;
		templateGuid: string;
	} | null;
}>();

const emit = defineEmits(['update:modelValue', 'answerChanged']);

const store = useStore<IRootState>();

const isBusyLoadingFromServer = ref<boolean>(true);
const consentData = ref<IAgentConsent | null>(null);
const guardianEditSSN = ref<string | null>(null);

const consentTitle = computed(() => consentData.value?.title ?? '');

const showDialog = computed({
	get: () => !!props.modelValue,
	set: (isVisible: boolean) => {
		if (!isVisible) {
			emit('update:modelValue', null);
		}
	},
});
const { width } = useDisplay();
const fullScreenModal = computed(() => width.value < 820);

const showEditGuardianAnswerModal = computed({
	get: () => !!guardianEditSSN.value,
	set: (visible: boolean) => {
		if (!visible) {
			guardianEditSSN.value = null;
		}
	},
});

const fetchChildConsent = async () => {
	if (!props.modelValue) {
		return;
	}

	isBusyLoadingFromServer.value = true;
	const fetchedConsentData = await store.dispatch('getAgentConsent', {
		childConsentRequest: {
			childSSNo: props.modelValue.childSSNo,
			templateGuid: props.modelValue.templateGuid,
		},
	});
	if (fetchedConsentData) {
		consentData.value = fetchedConsentData;
	}
	isBusyLoadingFromServer.value = false;
};

watch(
	() => props.modelValue,
	(newValue, oldValue) => {
		consentData.value = null;
		if (newValue && !oldValue) {
			fetchChildConsent();
		}
	}
);
</script>

<style scoped lang="scss">
.consent-agent-edit {
	.v-card-text {
		overflow-y: auto;
	}
	.back-btn {
		margin-top: -12px;
	}
	hr {
		margin: 16px 0;
		border: solid 1px $grey-lighten-3;
	}
	.v-col {
		padding-left: 0;
		padding-right: 0;
	}

	.linked-person {
		display: flex;
		align-items: center;
		border-bottom: solid 2px $grey-lighten-3;
		&:first-child {
			border-top: solid 2px $grey-lighten-3;
		}
		.name-status {
			flex: 1;
		}
		.v-chip {
			font-size: size(16);
			padding-left: 16px;
			padding-right: 16px;
			border: solid 1px $grey-lighten-4;
			background-color: $grey-lighten-2;
			height: auto;
			min-height: 30px;
			border-radius: 16px;

			:deep(.v-chip__content) {
				white-space: initial;
			}
			&.approved {
				color: #fff;
				background-color: $primary;
				border: none;
			}
			&.denied {
				color: #fff;
				background-color: $error;
				border: none;
			}
		}
	}
}
</style>
