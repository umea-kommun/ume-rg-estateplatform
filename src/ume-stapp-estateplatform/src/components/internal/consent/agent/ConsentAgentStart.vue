<template>
	<app-content
		:size="contentSize"
		class="consent-agent-start"
		:pageTitle="$t('component.internal.consentAgentStart.title')"
	>
		<consumer-tester />
		<base-back-button />
		<h1 class="my-3">
			{{ $t('component.internal.consentAgentStart.title') }}
		</h1>
		<p class="description mt-0">
			{{ $t('component.internal.consentAgentStart.description') }}
		</p>

		<student-filter
			@student-selected="selectedStudent = $event"
			:select-instruction="
				$t('component.internal.consentAgentStart.selectSchoolAndClass')
			"
		/>

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
		<v-row v-else-if="selectedStudent && childConsents?.length">
			<v-col class="pa-0 mt-3">
				<h2 class="mb-2 mt-0">
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
import { AppContentSize } from '@/models/Enums';
import { IRootState } from '@/models/Interfaces';
import { computed, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useStore } from 'vuex';
import ConsentAgentList from './ConsentAgentList.vue';
import ConsumerTester from '@/components/internal/shared/ConsumerTester.vue';
import StudentFilter from '../../shared/StudentFilter.vue';
import { IFilterStudent } from '@/models/schoolInterfaces';

const route = useRoute();
const store = useStore<IRootState>();

const isBusyFetchingConsents = ref(false);

const selectedStudent = ref<IFilterStudent | null>(null);
const childConsents = computed(() => store.state.consentAgentConsentList);

const childName = computed(() => {
	if (childConsents.value?.length) {
		return childConsents.value[0].childName;
	}
	return '';
});

async function fetchChildConsents(childSSN?: string) {
	if (!childSSN || isBusyFetchingConsents.value) {
		return;
	}
	isBusyFetchingConsents.value = true;
	await store.dispatch('getAgentChildConsentList', {
		childSSN,
	});
	isBusyFetchingConsents.value = false;
}

watch(selectedStudent, () => {
	fetchChildConsents(selectedStudent.value?.studentSsno);
});

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
