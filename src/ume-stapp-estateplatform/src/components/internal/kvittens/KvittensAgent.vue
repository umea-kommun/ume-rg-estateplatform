<template>
	<app-content
		:size="contentSize"
		:isLoading="false"
		class="kvittens-agent"
		:pageTitle="t('component.appHeader.title.internalKvittens')"
	>
		<base-back-button />
		<consumer-tester />
		<h1>{{ t('component.internal.kvittensAgent.title') }}</h1>
		<p class="description">
			{{ t('component.internal.kvittensAgent.description') }}
		</p>

		<student-filter @student-selected="selectedStudent = $event" />
		<app-loading-spinner
			v-if="isBusyFetchingKvittensList"
			:isVisible="isBusyFetchingKvittensList"
		/>
		<v-alert v-else-if="!selectedStudent" icon="info" class="mt-6">
			{{ t('component.internal.kvittensAgent.selectSchoolAndClass') }}
		</v-alert>
		<v-alert
			v-else-if="!kvittensListForSelectedStudent.length"
			icon="info"
			class="mt-6"
		>
			{{ t('component.internal.kvittensAgent.noResults') }}
		</v-alert>
		<div class="student" v-else-if="selectedStudent != null">
			<v-card
				:title="
					t('component.internal.kvittensAgent.kvittensTitle', {
						childName: selectedStudent.name,
					})
				"
				:subtitle="studentDateOfBirth"
			>
				<v-data-table
					v-model:sort-by="sortBy"
					:items="kvittensListForSelectedStudent"
					item-value="templateId"
					class="mt-0"
					items-per-page="-1"
					hide-default-footer
					hide-no-data
				>
					<!-- Table headers -->
					<template v-slot:headers="header">
						<base-table-header
							v-model:sortBy="sortBy"
							:headers="headers"
							default-order="asc"
							:header-slot-props="header"
						/>
					</template>
					<template v-slot:item="row">
						<tr class="kvittens-row">
							<td class="title pt-5 pb-5">
								{{ row.item.title }}
							</td>
							<td>
								<kvittens-answer
									:status="row.item.status"
								></kvittens-answer>
							</td>
							<td class="text-right">
								<v-btn
									variant="outlined"
									color="primary"
									class="register-btn regular-text"
									@click="
										openKvittensDetail = {
											studentSsno: row.item.personSSNo,
											templateId: row.item.templateId,
										}
									"
								>
									{{
										t(
											'component.internal.kvittensAgent.registerButton'
										)
									}}
								</v-btn>
							</td>
						</tr>
					</template>
				</v-data-table>
			</v-card>
			<kvittens-details-dialog
				v-model="openKvittensDetail"
			></kvittens-details-dialog>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useStore } from 'vuex';
import { useI18n } from 'vue-i18n';
import { IRootState, ISortBy, ITableHeader } from '@/models/Interfaces';
import { AppContentSize, DispatchType } from '@/models/Enums';
import KvittensDetailsDialog from './KvittensDetailsDialog.vue';
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import ConsumerTester from '@/components/internal/shared/ConsumerTester.vue';
import KvittensAnswer from '@/components/external/kvittens/KvittensAnswer.vue';
import StudentFilter from '../shared/StudentFilter.vue';
import { IFilterStudent } from '@/models/schoolInterfaces';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import ErrorService from '@/utils/ErrorService';

const { t } = useI18n();
const route = useRoute();
const store = useStore<IRootState>();

const sortBy = ref<ISortBy[]>([{ key: 'name', order: 'asc' }]);
const openKvittensDetail = ref<{
	studentSsno: string;
	templateId: string;
} | null>(null);

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const isBusyFetchingKvittensList = ref(false);
const selectedStudent = ref<IFilterStudent | null>(null);

const kvittensListForSelectedStudent = computed(
	() => store.state.kvittens?.kvittensAgentList ?? []
);
const studentDateOfBirth = computed(() => {
	const s = selectedStudent.value?.studentSsno ?? '';
	return `${s.substring(0, 4)}-${s.substring(4, 6)}-${s.substring(6, 8)}`;
});

const headers: ITableHeader[] = [
	{
		title: t('component.internal.kvittensAgent.headers.receiptTitle'),
		align: 'start',
		key: 'title',
	},
	{
		title: t('component.internal.kvittensAgent.headers.receiptStatus'),
		align: 'start',
		key: 'status',
	},
	{
		title: t(
			'component.internal.kvittensAgent.headers.registerSingleReceiptButton'
		),
		align: 'start',
		key: 'registerSingleReceiptButton',
	},
];

async function fetchKvittensList() {
	isBusyFetchingKvittensList.value = true;
	try {
		await store.dispatch(
			DispatchType.GetAgentKvittensList,
			selectedStudent.value?.studentSsno
		);
	} catch (err) {
		ErrorService.onError({ err });
	} finally {
		isBusyFetchingKvittensList.value = false;
	}
}

watch(selectedStudent, () => {
	fetchKvittensList();
});
</script>

<style scoped lang="scss">
.kvittens-agent {
	@media only screen and (max-width: 650px) {
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
		.kvittens-row {
			display: flex;
			flex-wrap: wrap;

			border-top: solid 1px $grey-lighten-4;
			margin-bottom: 12px;

			td {
				display: block;
				border-bottom: none !important;
				height: auto;
				width: 100%;

				.kvittens-answer {
					width: 100%;
					justify-content: center;
				}

				.v-btn {
					width: 100%;
					margin: 12px 0;
				}

				&.title {
					font-weight: bold;
					font-size: size(16);
				}
			}
		}
	}
}
</style>
