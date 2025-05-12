<template>
	<v-data-table
		v-model:items-per-page="itemsPerPage"
		v-model:sort-by="sortBy"
		v-model:page="page"
		:headers="tableHeaders"
		:items="filteredStudents"
		:custom-key-sort="{
			name: (a: string, b: string) => a.localeCompare(b),
		}"
		class="mt-3 kvittens-summary-list"
	>
		<!-- Table headers -->
		<template v-slot:headers>
			<base-table-header
				v-model:sortBy="sortBy"
				:headers="tableHeaders"
				default-order="asc"
			/>
		</template>

		<!-- Kvittens item -->
		<template v-slot:item="{ item }">
			<tr class="kvittens-summary-item">
				<td class="name">
					{{ item.name }}
				</td>
				<td class="date-of-birth">
					{{ item.dateOfBirth }}
				</td>
				<td
					v-for="template in sortedTemplates"
					:key="template.id"
					:title="template.title"
					:label="template.title"
				>
					<kvittens-answer
						:status="asKvittensStatus(item[template.id])"
					></kvittens-answer>
				</td>
			</tr>
		</template>

		<!-- Pagination -->
		<template v-slot:bottom>
			<base-table-pagination
				class="mt-6"
				v-model:items-per-page="itemsPerPage"
				v-model:page="page"
				:number-of-items="filteredStudents.length"
			/>
		</template>

		<!-- No results -->
		<template v-slot:no-data>
			<v-alert icon="info" class="mt-6 d-flex">
				{{
					studentSearchFilter || answerFilter !== undefined
						? $t(
								'component.internal.kvittensSummary.noResultsMatchingFilter'
						  )
						: $t('component.internal.kvittensSummary.noResults')
				}}
			</v-alert>
		</template>
	</v-data-table>
</template>

<script setup lang="ts">
import { PropType, computed, ref } from 'vue';
import { ISortBy, ITableHeader } from '@/models/Interfaces';
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import BaseTablePagination from '@/components/base/baseTable/BaseTablePagination.vue';
import KvittensAnswer from '@/components/external/kvittens/KvittensAnswer.vue';
import {
	IKvittensSummaryStudent,
	IKvittensSummaryTemplate,
} from '@/models/kvittens/Interfaces';
import { useI18n } from 'vue-i18n';
import {
	KvittensStatus,
	KvittensSummaryAnswerFilter,
} from '@/models/kvittens/Enums';
import moment from 'moment';

const props = defineProps({
	students: {
		required: true,
		type: Array as PropType<IKvittensSummaryStudent[]>,
	},
	templates: {
		required: true,
		type: Array as PropType<IKvittensSummaryTemplate[]>,
	},
	studentSearchFilter: {
		type: String,
	},
	answerFilter: {
		type: Number as PropType<KvittensSummaryAnswerFilter>,
	},
});

const { t } = useI18n();

const page = ref(1);
const itemsPerPage = ref(50);
const sortBy = ref<ISortBy[]>([{ key: 'name', order: 'asc' }]);

const sortedTemplates = computed(() => {
	const templates = [...props.templates];
	return templates.sort((a, b) => a.id.localeCompare(b.id));
});

const tableHeaders = computed(() => {
	const headers: ITableHeader[] = [
		{
			title: t('component.internal.kvittensSummary.headers.name'),
			align: 'start',
			key: 'name',
		},
		{
			title: t('component.internal.kvittensSummary.headers.dateOfBirth'),
			align: 'start',
			key: 'dateOfBirth',
		},
	];
	// Add templates as table headers
	sortedTemplates.value.forEach((template) => {
		headers.push({
			title: template.shortTitle,
			description: template.title,
			align: 'start',
			key: template.id,
		});
	});

	return headers;
});

const asKvittensStatus = (status: string | KvittensStatus) => {
	return status as KvittensStatus;
};

const formatDateOfBirth = (dateOfBirth: string): string => {
	return moment(dateOfBirth).format('YYYY-MM-DD');
};

const filteredStudents = computed(() => {
	let students = props.students;

	// Text search filter
	if (props.studentSearchFilter) {
		students = students.filter(
			(student) =>
				student.name
					.toLowerCase()
					.indexOf(props.studentSearchFilter?.toLowerCase() ?? '') >
				-1
		);
	}

	if (props.answerFilter !== undefined) {
		switch (props.answerFilter) {
			case KvittensSummaryAnswerFilter.AllAnswered:
				students = students.filter((student) => {
					if (
						student.answers.length === sortedTemplates.value.length
					) {
						return student.answers.every(
							(answer) =>
								answer.status === KvittensStatus.Approved
						);
					}
					return false;
				});
				break;
			case KvittensSummaryAnswerFilter.HasUnanswered:
				students = students.filter((student) => {
					if (
						student.answers.length === sortedTemplates.value.length
					) {
						return student.answers.some(
							(answer) =>
								answer.status !== KvittensStatus.Approved
						);
					}
					return true;
				});
				break;
		}
	}

	const studentsWithKvittensAnswers = students.map((student) => {
		const studentWithKvittensAnswers: {
			[key: string]: string | KvittensStatus;
		} = {
			name: student.name,
			dateOfBirth: formatDateOfBirth(student.dateOfBirth),
		};

		sortedTemplates.value.forEach((template) => {
			const studentAnswer = student.answers.find(
				(answer) => answer.templateId === template.id
			);

			// If the student has an answer for this kvittens, put that in. Otherwise mark as unanswered
			studentWithKvittensAnswers[template.id] = studentAnswer
				? studentAnswer.status
				: KvittensStatus.NotAnswered;
		});
		return studentWithKvittensAnswers;
	});

	return studentsWithKvittensAnswers;
});
</script>

<style scoped lang="scss">
.kvittens-summary-list {
	.kvittens-summary-item,
	.v-alert {
		font-size: size(16);
	}

	@media only screen and (max-width: 900px) {
		.top-wrap {
			display: block;

			.select-wrap {
				max-width: none;
				padding-top: 0.5rem !important;
			}
		}
		:deep(table) {
			width: 100%;

			thead tr:not(.v-data-table-progress) {
				display: none;
			}
		}
		.kvittens-summary-item {
			display: flex;
			flex-wrap: wrap;

			border: solid 1px $grey-lighten-4;
			border-radius: $border-radius;
			box-shadow: 0px 3px 5px -2px rgba(0, 0, 0, 0.2);
			margin-bottom: 24px;

			.kvittens-answer {
				margin-top: 6px;
			}

			td {
				background-color: transparent;
				display: block;
				border-bottom: none !important;
				height: auto;
				padding: 6px 14px;
				width: 100%;

				&.name {
					font-weight: bold;
				}
				&.date-of-birth,
				&.name {
					font-size: size(20);
					display: inline-block;
					width: auto;
					margin-top: 10px;
				}

				&:last-child {
					margin-bottom: 10px;
				}

				&:before {
					display: block;
					color: $grey-darken-2;
					content: attr(label);
					font-weight: normal;
					font-size: size(16);
				}
			}
		}
	}
}
</style>
