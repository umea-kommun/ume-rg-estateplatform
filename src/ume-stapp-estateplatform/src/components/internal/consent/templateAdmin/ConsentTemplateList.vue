<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="consent-template-admin"
		:pageTitle="$t('component.internal.consentTemplateAdmin.title')"
	>
		<base-back-button />
		<div v-if="!isBusyLoadingFromServer">
			<v-row>
				<v-col class="pa-0">
					<h1 class="my-3">
						{{
							$t('component.internal.consentTemplateAdmin.title')
						}}
					</h1>
				</v-col>
			</v-row>
			<v-row class="filter-wrap">
				<v-col class="pl-0 search-bar">
					<v-text-field
						id="search"
						v-model="searchValue"
						color="primary"
						:label="
							$t('component.internal.consentTemplateAdmin.search')
						"
						prepend-inner-icon="search"
						density="comfortable"
						hide-details
						clearable
					/>
				</v-col>
				<v-col>
					<v-btn
						class="ma-0"
						variant="outlined"
						:append-icon="
							showFilter ? 'expand_less' : 'expand_more'
						"
						@click="toggleFilter"
						size="large"
					>
						{{
							$t(
								'component.internal.consentTemplateAdmin.filterButton'
							)
						}}
					</v-btn>
				</v-col>
				<v-col class="pr-0">
					<v-btn
						:to="{
							name: MyPagesRoutes.InternalConsentTemplateEdit,
							params: { templateGuid: ConsentTemplateGuid.New },
						}"
						class="ma-0 create-button"
						color="primary"
						size="large"
						prepend-icon="add"
						flat
					>
						{{
							$t('component.internal.consentTemplateAdmin.create')
						}}
					</v-btn>
				</v-col>
			</v-row>
			<v-row v-show="showFilter" class="filter-row mt-4">
				<template-group-select
					id="group"
					v-model="selectedGroups"
					:label="
						$t(
							'component.internal.consentTemplateAdmin.filterGroup'
						)
					"
					:add-label="
						$t(
							'component.internal.consentTemplateAdmin.addFilterGroup'
						)
					"
					:modal-add-btn-label="
						$t(
							'component.internal.consentTemplateAdmin.addFilterBtn'
						)
					"
				>
					<template v-slot:empty>
						<span class="text-grey-darken-2">
							{{
								$t(
									'component.internal.consentTemplateAdmin.filterGroupEmpty'
								)
							}}
						</span>
					</template>
				</template-group-select>
			</v-row>
			<v-alert
				v-if="!filteredTemplates.length"
				icon="warning"
				class="mt-4"
			>
				{{ $t('component.internal.consentTemplateAdmin.noResults') }}
			</v-alert>
			<!-- Display list of templates-->
			<v-data-table
				v-else
				v-model:items-per-page="itemsPerPage"
				v-model:sort-by="sortBy"
				v-model:page="page"
				:headers="headers"
				:items="filteredTemplates"
				:custom-key-sort="{
					title: (a: string, b: string) => a.localeCompare(b),
				}"
				class="mt-3"
			>
				<!-- Table headers -->
				<template v-slot:headers>
					<base-table-header
						v-model:sortBy="sortBy"
						:headers="headers"
					/>
				</template>

				<!-- Form item -->
				<template v-slot:item="{ item }: { item: ITemplateItem }">
					<tr class="consent-template-item">
						<td>
							{{ item.title }}
						</td>
						<td>
							<v-chip
								v-for="group in item.groups"
								:key="group.refId"
								variant="outlined"
								class="mr-1 mb-1"
								>{{ group.name }}</v-chip
							>
						</td>
						<td>
							<span v-if="item.periodStart && item.periodEnd">
								{{ `${item.periodStart} - ${item.periodEnd}` }}
							</span>
							<span v-else-if="item.periodStart">
								{{
									$t(
										'component.internal.consentTemplateAdmin.period.indefinitely',
										{ date: item.periodStart }
									)
								}}
							</span>
							<span v-else-if="item.periodEnd">
								{{
									$t(
										'component.internal.consentTemplateAdmin.period.until',
										{ date: item.periodEnd }
									)
								}}
							</span>
							<span
								v-else
								class="font-italic text-medium-emphasis"
								>{{
									$t(
										'component.internal.consentTemplateAdmin.period.none'
									)
								}}</span
							>
						</td>
						<td
							:class="{
								'status-published':
									item.status ===
									ConsentTemplateStatus.Published,
							}"
						>
							{{
								$t(
									'component.internal.consentTemplateAdmin.status.' +
										item.status
								)
							}}
						</td>
						<td class="open-button-wrap">
							<v-btn
								:to="{
									name: MyPagesRoutes.InternalConsentTemplateEdit,
									params: { templateGuid: item.guid },
								}"
								class="ma-0"
								color="primary"
								variant="outlined"
								>{{
									item.status ===
									ConsentTemplateStatus.Published
										? $t(
												'component.internal.consentTemplateAdmin.open'
										  )
										: $t(
												'component.internal.consentTemplateAdmin.edit'
										  )
								}}
							</v-btn>
						</td>
					</tr>
				</template>

				<!-- Pagination -->
				<template v-slot:bottom>
					<base-table-pagination
						class="mt-6"
						v-model:items-per-page="itemsPerPage"
						v-model:page="page"
						:number-of-items="filteredTemplates.length"
					/>
				</template>
			</v-data-table>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useStore } from 'vuex';
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import BaseTablePagination from '@/components/base/baseTable/BaseTablePagination.vue';
import { useRoute } from 'vue-router';
import {
	IRootState,
	IConsentTemplateGroup,
	ISortBy,
	ITableHeader,
	ITemplateConnection,
} from '@/models/Interfaces';
import {
	AppContentSize,
	DispatchType,
	ConsentTemplateStatus,
	ConsentTemplateGuid,
} from '@/models/Enums';
import BaseTextBox from '@/components/base/BaseTextBox.vue';
import { useI18n } from 'vue-i18n';
import { MyPagesRoutes } from '@/router/routes';
import TemplateGroupSelect from './TemplateGroupSelect.vue';
import moment from 'moment';

const { t } = useI18n();
const store = useStore<IRootState>();
const route = useRoute();
const isBusyLoadingFromServer = ref<boolean>(false);

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const searchValue = ref('');
const selectedGroups = ref<IConsentTemplateGroup[]>([]);

const page = ref(1);
const itemsPerPage = ref(10);
const sortBy = ref<ISortBy[]>([{ key: 'period', order: 'desc' }]);
const showFilter = ref(false);

const headers: ITableHeader[] = [
	{
		title: t('component.internal.consentTemplateAdmin.headers.title'),
		align: 'start',
		key: 'title',
	},
	{
		title: t('component.internal.consentTemplateAdmin.headers.groups'),
		align: 'start',
		key: 'groups',
	},
	{
		title: t('component.internal.consentTemplateAdmin.headers.period'),
		align: 'start',
		key: 'period',
	},
	{
		title: t('component.internal.consentTemplateAdmin.headers.status'),
		align: 'start',
		key: 'status',
	},
	{ title: '', key: 'actions', sortable: false },
];

interface ITemplateItem {
	guid?: string;
	title: string;
	groups: ITemplateConnection[];
	period: string;
	periodStart: string;
	periodEnd: string;
	status: ConsentTemplateStatus;
}

const templateItems = computed<ITemplateItem[]>(() => {
	if (store.state.consentTemplates?.length) {
		return store.state.consentTemplates.map((template) => {
			const periodStart = template.publishedDate
				? moment(template.publishedDate).format('L')
				: '';
			const periodEnd = template.expireDate
				? moment(template.expireDate).format('L')
				: '';
			return {
				guid: template.guid,
				title: template.title,
				groups: template.templateConnections,
				period:
					(template.publishedDate ?? '') +
					' - ' +
					(template.expireDate ?? ''),
				periodStart,
				periodEnd,
				status: template.status,
			};
		});
	} else {
		return [];
	}
});

const selectedGroupRefIds = computed(() =>
	selectedGroups.value.map((group) => group.refId)
);

const filteredTemplates = computed(() => {
	let templates = templateItems.value;
	if (searchValue.value) {
		templates = templates.filter((template) => {
			return (
				template.title
					.toLowerCase()
					.indexOf(searchValue.value.toLowerCase()) > -1
			);
		});
	}

	if (selectedGroupRefIds.value?.length) {
		templates = templates.filter((template) => {
			let templateHasGroup = false;
			template.groups.forEach((templateGroup) => {
				if (
					selectedGroupRefIds.value.indexOf(templateGroup.refId) > -1
				) {
					templateHasGroup = true;
				}
			});
			return templateHasGroup;
		});
	}

	return templates;
});

const toggleFilter = (): void => {
	if (showFilter.value) {
		showFilter.value = false;
		selectedGroups.value = [];
	} else {
		showFilter.value = true;
	}
};

onMounted(async () => {
	isBusyLoadingFromServer.value = true;
	await store.dispatch(DispatchType.GetConsentTemplates);
	isBusyLoadingFromServer.value = false;
});
</script>
<style scoped lang="scss">
.consent-template-admin {
	:deep(th) {
		&:first-child {
			padding-left: 0;
		}
		&:last-child {
			padding-right: 0;
		}
	}
	:deep(td) {
		&:first-child {
			padding-left: 0;
		}
		&:last-child {
			padding-right: 0;
		}
	}

	.v-btn:not(.back-btn) {
		text-transform: none;
		letter-spacing: normal;
	}

	.filter-wrap {
		margin-top: 0;
		flex-wrap: nowrap;
		align-items: flex-end;
		.v-col {
			align-items: center;

			&.search-bar {
				flex: auto;
				width: 100%;
			}

			:deep(.help-and-error-wrap) {
				margin-bottom: 0;
			}

			:deep(.v-btn--variant-outlined) {
				border-color: $grey-darken-1;
				.v-icon {
					margin-top: 4px;
				}
			}

			.v-btn {
				height: size(48);
			}

			.create-button {
				color: $white !important;
			}
		}
	}
	.filter-row {
		background-color: $grey-lighten-2;
		padding: 8px 16px;
		padding-bottom: 0px;
		border-radius: $border-radius;
	}

	.consent-template-item {
		td {
			padding-top: 14px;
			padding-bottom: 14px;

			&.status-published {
				color: $primary;
			}
		}
		.v-chip {
			font-size: size(16);
			border: solid 1px $grey-lighten-4;
			background-color: $grey-lighten-2;
		}
		.open-button-wrap {
			text-align: right;
			.v-btn {
				min-width: 102px;
				font-size: size(16);
			}
		}
	}
	&.app-content {
		:deep(.v-container) {
			padding-top: calc($site-content-vertical-padding - 20px);
		}
	}
}
</style>
