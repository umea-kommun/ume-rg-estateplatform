<template>
	<app-content
		:size="contentSize"
		class="consent-agent-start"
		:pageTitle="$t('component.internal.consentAgentStart.title')"
	>
		<consumer-tester />
		<base-back-button />
		<h1>{{ $t('component.internal.consentAgentStart.title') }}</h1>
		<p class="description">
			{{ $t('component.internal.consentAgentStart.description') }}
		</p>

		<v-row class="mt-6">
			<v-col class="pa-0">
				<consent-agent-search @child-search="fetchChildConsents" />
			</v-col>
		</v-row>

		<v-row v-if="isBusyFetchingConsents">
			<v-col class="pa-0">
				<app-loading-spinner :isVisible="true" />
			</v-col>
		</v-row>
		<v-row v-else-if="childConsents?.length === 0">
			<v-col class="pa-0">
				<v-alert icon="info">{{
					$t('component.internal.consentAgentStart.noResult')
				}}</v-alert>
			</v-col>
		</v-row>
		<v-row v-else-if="childConsents?.length">
			<v-col class="pa-0 mt-3">
				<h2>
					{{
						$t(
							'component.internal.consentAgentStart.consentsTitle',
							{ childName }
						)
					}}
				</h2>
				<consent-agent-list :consents="childConsents" />
			</v-col>
		</v-row>
	</app-content>
</template>

<script setup lang="ts">
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import ConsentAgentSearch from '@/components/internal/consent/agent/ConsentAgentSearch.vue';
import { AppContentSize } from '@/models/Enums';
import { IChildConsent, IRootState } from '@/models/Interfaces';
import { computed, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useStore } from 'vuex';
import ConsentAgentList from './ConsentAgentList.vue';
import ConsumerTester from '@/components/internal/consent/consumer/ConsumerTester.vue';

const route = useRoute();
const store = useStore<IRootState>();

const isBusyFetchingConsents = ref(false);

const childConsents = ref<IChildConsent[] | null>(null);

const childName = computed(() => {
	if (childConsents.value?.length) {
		return childConsents.value[0].childName;
	}
	return '';
});

async function fetchChildConsents(childSSN: string) {
	if (!childSSN || isBusyFetchingConsents.value) {
		return;
	}
	isBusyFetchingConsents.value = true;
	childConsents.value = await store.dispatch('getAgentChildConsentList', {
		childSSN,
	});
	isBusyFetchingConsents.value = false;
}

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.consent-agent-start {
	.description {
		max-width: 700px;
	}
	.child-consent-title {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	:deep(.v-btn) {
		&:not(.back-btn) {
			text-transform: none;
			letter-spacing: normal;
			font-size: size(16);
		}
	}

	&.app-content {
		:deep(.v-container) {
			padding-top: calc($site-content-vertical-padding - 20px);
		}
	}
}
</style>
