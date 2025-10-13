<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyFetchingSchoolsAndClasses"
		class="kvittens-summary"
		:pageTitle="$t('component.appHeader.title.internalKvittens')"
	>
		<base-back-button />
		<consumer-tester />
		<div>
			<h1 class="mt-2 mb-4">
				{{ $t('component.internal.kvittensSummary.title') }}
			</h1>
			<div class="filter">
				<div class="d-flex">
					<v-select
						v-model="selectedSchoolRefId"
						:items="schools"
						:label="
							$t('component.internal.kvittensSummary.schoolLabel')
						"
						:disabled="schools.length === 0"
						itemTitle="name"
						itemValue="refId"
						variant="outlined"
						density="comfortable"
						color="primary"
						hide-details
					/>
					<v-select
						v-model="selectedClassRefId"
						:items="classesInSelectedSchool"
						:label="
							$t('component.internal.kvittensSummary.classLabel')
						"
						itemTitle="name"
						itemValue="refId"
						variant="outlined"
						density="comfortable"
						color="primary"
						hide-details
						:disabled="!selectedSchoolRefId"
					/>
				</div>
				<div class="d-flex justify-end">
					<v-text-field
						v-model="studentSearch"
						:label="
							$t(
								'component.internal.kvittensSummary.searchStudent'
							)
						"
						density="comfortable"
						variant="outlined"
						prepend-inner-icon="search"
						color="primary"
						hide-details
						:disabled="!selectedClassRefId"
					/>
					<v-select
						:label="
							$t(
								'component.internal.kvittensSummary.filterOnAnswer'
							)
						"
						v-model="studentAnswerFilter"
						:items="answerFilterOptions"
						clearable
						density="comfortable"
						variant="outlined"
						color="primary"
						hide-details
						:disabled="!selectedClassRefId"
					/>
				</div>
			</div>
			<v-alert
				v-if="
					!isBusyFetchingSchoolsAndClasses &&
					(schools.length === 0 || classes.length === 0)
				"
				icon="info"
				class="mt-6"
			>
				{{ $t('component.internal.kvittensSummary.noAccess') }}
			</v-alert>
			<v-alert
				v-else-if="!selectedSchoolRefId || !selectedClassRefId"
				icon="info"
				class="mt-6"
			>
				{{
					$t(
						'component.internal.kvittensSummary.selectSchoolAndClass'
					)
				}}
			</v-alert>
			<app-loading-spinner
				v-else-if="isBusyFetchingSummary"
				:isVisible="true"
			/>
			<kvittens-summary-list
				v-show="
					selectedSchoolRefId &&
					selectedClassRefId &&
					!isBusyFetchingSummary
				"
				:templates="templates"
				:students="students"
				:student-search-filter="studentSearch"
				:answer-filter="studentAnswerFilter"
			/>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import KvittensSummaryList from './KvittensSummaryList.vue';
import { useRoute } from 'vue-router';
import { AppContentSize } from '@/models/Enums';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import { onMounted } from 'vue';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { DispatchType } from '@/models/Enums';
import { KvittensSummaryAnswerFilter } from '@/models/kvittens/Enums';
import ConsumerTester from '@/components/internal/shared/ConsumerTester.vue';
import {
	IKvittensFilterClass,
	IKvittensFilterSchool,
	IKvittensSummary,
	IKvittensSummaryStudent,
	IKvittensSummaryTemplate,
} from '@/models/kvittens/Interfaces';
import { useI18n } from 'vue-i18n';

const route = useRoute();
const store = useStore<IRootState>();
const { t } = useI18n();

const isBusyFetchingSchoolsAndClasses = ref<boolean>(false);
const isBusyFetchingSummary = ref<boolean>(false);

const selectedSchoolRefId = ref<string | null>(null);
const schools = ref<IKvittensFilterSchool[]>([]);
const selectedClassRefId = ref<string | null>(null);
const classes = ref<IKvittensFilterClass[]>([]);
const studentSearch = ref('');
const studentAnswerFilter = ref<KvittensSummaryAnswerFilter>();

const templates = ref<IKvittensSummaryTemplate[]>([]);
const students = ref<IKvittensSummaryStudent[]>([]);

const answerFilterOptions = [
	{
		title: t('component.internal.kvittensSummary.filter.allAnswered'),
		value: KvittensSummaryAnswerFilter.AllAnswered,
	},
	{
		title: t('component.internal.kvittensSummary.filter.hasUnanswered'),
		value: KvittensSummaryAnswerFilter.HasUnanswered,
	},
];

const classesInSelectedSchool = computed(() => {
	return classes.value
		.filter(
			(classGroup) => classGroup.schoolRefId === selectedSchoolRefId.value
		)
		.sort((a, b) => a.name.localeCompare(b.name));
});

const fetchSummary = async () => {
	if (selectedSchoolRefId.value && selectedClassRefId.value) {
		isBusyFetchingSummary.value = true;
		const summaryResult: IKvittensSummary = await store.dispatch(
			DispatchType.GetKvittensSummary,
			{
				classRefId: selectedClassRefId.value,
			}
		);
		templates.value = summaryResult.templates;
		students.value = summaryResult.students;

		isBusyFetchingSummary.value = false;
	}
};

watch(selectedSchoolRefId, (newSelectedSchool) => {
	selectedClassRefId.value = null;

	if (newSelectedSchool && classesInSelectedSchool.value.length === 1) {
		// If user only has access to one class in the school, preselect it
		selectedClassRefId.value = classesInSelectedSchool.value[0].refId;
	}
});

watch(selectedClassRefId, (newSelectedClass) => {
	if (newSelectedClass) {
		fetchSummary();
	}
});

onMounted(async () => {
	isBusyFetchingSchoolsAndClasses.value = true;

	const { schools: fetchedSchools, groups: fetchedClasses } =
		await store.dispatch(DispatchType.GetKvittensFilterGroups);
	schools.value = fetchedSchools.sort(
		(a: IKvittensFilterSchool, b: IKvittensFilterSchool) =>
			a.name.localeCompare(b.name)
	);
	classes.value = fetchedClasses;

	if (schools.value.length === 1) {
		// If user only has access to one school, preselect it
		selectedSchoolRefId.value = schools.value[0].refId;
	}

	isBusyFetchingSchoolsAndClasses.value = false;
});

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.kvittens-summary {
	.filter {
		width: 100%;
		display: flex;
		flex-wrap: wrap;
		justify-content: space-between;
		gap: 16px;
		> div {
			flex: 1;
			gap: 16px;

			@media only screen and (max-width: 400px) {
				flex-wrap: wrap;
			}
		}

		:deep(.v-input) {
			max-width: 200px;
			min-width: 140px;
			@media only screen and (max-width: 700px) {
				flex: 1;
				min-width: none;
				max-width: none;
			}
			.v-field__input .v-select__selection {
				padding: 0 !important;
			}
		}
	}
}
.kvittens-summary.app-content {
	:deep(.v-container) {
		padding-top: calc($site-content-vertical-padding - 20px);
	}
}
</style>
