<template>
	<app-content
		:size="contentSize"
		class="consent-agent-edit"
		:pageTitle="$t('component.internal.consentAgentStart.title')"
	>
		<consumer-tester />
		<base-back-button />
		<h1>{{ consentTitle }}</h1>
		<hr aria-hidden="true" />
		<app-loading-spinner v-if="isBusyLoadingFromServer" :isVisible="true" />
		<div v-else-if="consentData">
			<v-row>
				<v-col>
					<p v-html="consentData.data"></p>
				</v-col>
			</v-row>
			<hr aria-hidden="true" />

			<h2 class="mt-10">
				{{ $t('component.internal.consentAgentEdit.guardianTitle') }}
			</h2>
			<v-alert class="mt-2" icon="info">
				{{ $t('component.internal.consentAgentEdit.handleConsentFor') }}
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
								$t('component.internal.consentAgentList.answer')
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
			/>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import { IAgentConsent, IRootState } from '@/models/Interfaces';
import AppContent from '@/components/app/AppContent.vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import { MyPagesRoutes } from '@/router/routes';
import { ref } from 'vue';
import { onMounted } from 'vue';
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useStore } from 'vuex';
import { AppContentSize, UserConsentStatus } from '@/models/Enums';
import { getConsentUserStatusText } from '@/utils/utils';
import ConsentHistory from '@/components/consent/ConsentHistory.vue';
import ConsumerTester from '@/components/internal/consent/consumer/ConsumerTester.vue';
import ConsentAgentGuardianEditModal from './ConsentAgentGuardianEditModal.vue';

const store = useStore<IRootState>();
const router = useRouter();
const route = useRoute();

const isBusyLoadingFromServer = ref<boolean>(true);
const consentData = ref<IAgentConsent>();
const guardianEditSSN = ref<string | null>(null);

const consentTitle = computed(() => {
	if (consentData.value) {
		return consentData.value.title;
	}
	return store.state.agent.activeEditConsent?.title ?? '';
});

const showEditGuardianAnswerModal = computed({
	get: () => !!guardianEditSSN.value,
	set: (visible: boolean) => {
		if (!visible) {
			guardianEditSSN.value = null;
		}
	},
});

const fetchChildConsent = async () => {
	const activeEditConsent = store.state.agent.activeEditConsent ?? null;
	if (!activeEditConsent) {
		router.replace({ name: MyPagesRoutes.InternalConsentAgentStart });
		return;
	}

	isBusyLoadingFromServer.value = true;
	const fetchedConsentData = await store.dispatch('getAgentConsent', {
		childConsentRequest: {
			childSSNo: activeEditConsent.childSSNo,
			templateGuid: activeEditConsent.templateGuid,
		},
	});
	if (fetchedConsentData) {
		consentData.value = fetchedConsentData;
	}
	isBusyLoadingFromServer.value = false;
};

onMounted(() => {
	fetchChildConsent();
});

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.consent-agent-edit {
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
