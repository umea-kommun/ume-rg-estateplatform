<template>
	<app-content
		:isLoading="isBusyLoadingFromServer"
		class="consent-start"
		:pageTitle="$t('component.consentStart.title')"
	>
		<base-back-button
			:to="{ name: MyPagesRoutes.AppStart, replace: true }"
		/>
		<div class="d-flex flex-wrap justify-space-between top-wrap">
			<h1 class="my-1">{{ $t('component.consentStart.title') }}</h1>
			<div
				v-if="guardianChildren.length > 1"
				class="d-flex justify-end align-center mb-2 filter-on-child"
			>
				<v-select
					v-model="selectedChildSSNo"
					:items="guardianChildren"
					clearable
					:label="$t('component.consentStart.selectListTitle')"
					variant="outlined"
					density="comfortable"
					color="primary"
					item-title="name"
					item-value="socialSecurityNumber"
					class="mt-2"
					rounded="lg"
					hide-details
				/>
			</div>
		</div>
		<non-folkbokford-alert :children="guardianChildren" />
		<v-alert
			v-if="!consentItems.length && !selectedChildSSNo"
			icon="warning"
			rounded="lg"
			class="mt-6"
		>
			{{ $t('component.consentStart.noResults') }}
		</v-alert>
		<v-alert
			v-else-if="!consentItems.length && selectedChildSSNo"
			icon="warning"
			rounded="lg"
			class="mt-6"
		>
			{{ $t('component.consentStart.noFilterResults') }}
		</v-alert>
		<div v-if="unansweredConsents.length" class="mb-12">
			<h2 class="mt-6 mb-5">
				{{
					$t('component.consentStart.titleUnanswered', {
						count: unansweredConsents.length,
					})
				}}
			</h2>
			<consent-list-item
				v-for="consent in unansweredConsents"
				:key="consent.templateGuid + consent.childSSNo"
				:consent="consent"
				@open="openConsent(consent)"
			/>
		</div>
		<div v-if="answeredConsents.length" class="mb-12">
			<h2 class="mt-6 mb-5">
				{{
					$t('component.consentStart.titleAnswered', {
						count: answeredConsents.length,
					})
				}}
			</h2>
			<consent-list-item
				v-for="consent in answeredConsents"
				:key="consent.templateGuid + consent.childSSNo"
				:consent="consent"
				@open="openConsent(consent)"
			/>
		</div>
		<div v-if="inactiveConsents.length" class="mb-12">
			<h2 class="mt-6 mb-5">
				{{
					$t('component.consentStart.titleInactive', {
						count: inactiveConsents.length,
					})
				}}
			</h2>
			<consent-list-item
				v-for="consent in inactiveConsents"
				:key="consent.templateGuid + consent.childSSNo"
				:consent="consent"
				@open="openConsent(consent)"
			/>
		</div>
		<consent-edit-modal
			v-if="showConsentModal && consentModalProps"
			v-model="showConsentModal"
			:templateGuid="consentModalProps.templateGuid"
			:childSSNo="consentModalProps.childSSNo"
			@answerChanged="refetchListData"
		/>

		<rate-feedback
			v-if="Array.isArray(consentItems) && consentItems.length > 0"
			:feedback-title="$t('component.consentStart.feedbackTitle')"
			category="consents"
			:additionalInfo="{ numberOfConsents: consentItems.length }"
		/>
	</app-content>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import { UserConsentStatus } from '@/models/Enums';
import store from '@/store/store';
import { DispatchType } from '@/models/Enums';
import { computed } from 'vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import { IChildConsent } from '@/models/Interfaces';
import ConsentEditModal from './ConsentEditModal.vue';
import { MyPagesRoutes } from '@/router/routes';
import RateFeedback from '@/components/feedback/RateFeedback.vue';
import ConsentListItem from './ConsentListItem.vue';
import NonFolkbokfordAlert from '../common/NonFolkbokfordAlert.vue';

const isBusyLoadingFromServer = ref<boolean>(false);
const isBusyRefetchingList = ref(false);

onMounted(async () => {
	// Fetch needed data
	isBusyLoadingFromServer.value = true;
	await Promise.all([
		store.dispatch(DispatchType.GetChildren),
		store.dispatch(DispatchType.GetConsentList),
	]);
	isBusyLoadingFromServer.value = false;
});

const refetchCallId = ref(0);
const refetchListData = async () => {
	const id = Math.random();
	refetchCallId.value = id;

	isBusyRefetchingList.value = true;
	await store.dispatch(DispatchType.GetConsentList);
	if (refetchCallId.value === id) {
		/** If the user quickly change their answer multiple times, we don't want to stop showing the loader
		    until all the refetch requests are done. If the id has changed, another call is ongoing */
		isBusyRefetchingList.value = false;
	}
};

const consentModalProps = ref<{
	templateGuid: string;
	childSSNo: string;
} | null>(null);

const showConsentModal = computed({
	get: () => !!consentModalProps.value,
	set: (show) => {
		if (!show) {
			consentModalProps.value = null;
		}
	},
});

const openConsent = (consent: IChildConsent) => {
	consentModalProps.value = {
		templateGuid: consent.templateGuid,
		childSSNo: consent.childSSNo,
	};
};

const selectedChildSSNo = ref<string | null>(null);
const guardianChildren = computed(
	() => store.state.guardianUser?.children ?? []
);
const consentItems = computed(() => {
	if (store.state.childConsentList) {
		return store.state.childConsentList.filter(
			(element) =>
				!selectedChildSSNo.value ||
				element.childSSNo === selectedChildSSNo.value
		);
	} else {
		return [];
	}
});

const unansweredConsents = computed(() =>
	consentItems.value.filter(
		(consent) =>
			consent.userStatus === UserConsentStatus.NotAnswered &&
			consent.isActive
	)
);

const answeredConsents = computed(() =>
	consentItems.value.filter(
		(consent) =>
			(consent.userStatus === UserConsentStatus.Approved ||
				consent.userStatus === UserConsentStatus.Rejected) &&
			consent.isActive
	)
);

const inactiveConsents = computed(() =>
	consentItems.value.filter((consent) => !consent.isActive)
);
</script>

<style scoped lang="scss">
.consent-start {
	.top-wrap {
		h1 {
			font-size: size(38);
		}
		.filter-on-child {
			min-width: 200px;
			:deep(.v-field__input .v-select__selection) {
				padding: 0 !important;
			}
		}

		@media only screen and (max-width: 700px) {
			h1 {
				width: 100%;
			}
			.filter-on-child {
				flex: 1;
				margin: 10px 0;
				padding-bottom: 10px;
			}
		}
	}
}
.consent-start.app-content {
	:deep(.v-container) {
		padding-top: calc($site-content-vertical-padding - 20px);
	}
}
</style>
