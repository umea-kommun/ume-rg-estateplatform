<template>
	<v-data-table
		v-model:items-per-page="itemsPerPage"
		v-model:sort-by="sortBy"
		v-model:page="page"
		:headers="headers"
		:items="filteredTemplates"
		class="consent-consumer-list-table mt-3"
	>
		<!-- Table headers -->
		<template v-slot:headers>
			<base-table-header v-model:sortBy="sortBy" :headers="headers" />
		</template>

		<!-- Form item -->
		<template v-slot:item="{ item }: { item: IFilteredConsumerTemplate }">
			<tr class="consent-template-item">
				<td :label="headers[0].title">
					{{ item.title }}
				</td>
				<td :label="headers[1].title">
					<v-chip
						v-for="group in item.groups.slice(
							0,
							SHOW_GROUPS_PER_TEMPLATE
						)"
						:key="group.refId"
						variant="outlined"
						class="mr-1 mb-1"
						:class="{
							selected: selectedGroupId === group.refId,
						}"
						>{{ group.title }}</v-chip
					>
					<v-chip
						v-if="item.groups.length > SHOW_GROUPS_PER_TEMPLATE"
						class="mr-1 mb-1 additional-groups"
						variant="text"
					>
						{{
							$t(
								'component.internal.consentConsumerList.plusXOtherGroups',
								{
									count:
										item.groups.length -
										SHOW_GROUPS_PER_TEMPLATE,
								}
							)
						}}
					</v-chip>
				</td>
				<td :label="headers[2].title">
					{{ moment(item.period.start).format('Do MMMM YYYY') }}
					-
					{{ moment(item.period.end).format('Do MMMM YYYY') }}
				</td>
				<td class="open-button-wrap">
					<v-btn
						v-if="item.groups.length === 1 || selectedGroupId"
						:to="{
							name: MyPagesRoutes.InternalConsentConsumerDetails,
							params: {
								templateGuid: item.guid,
								groupId:
									item.groups[0].refId ?? selectedGroupId,
							},
						}"
						class="ma-0"
						color="primary"
						variant="flat"
						>{{ $t('component.internal.consentConsumerList.open') }}
					</v-btn>
					<v-btn
						v-else
						class="ma-0"
						color="primary"
						variant="flat"
						@click="emit('selectGroupToOpen', item.guid)"
						>{{ $t('component.internal.consentConsumerList.open') }}
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
</template>

<script setup lang="ts">
import {
	IFilteredConsumerTemplate,
	ISortBy,
	ITableHeader,
} from '@/models/Interfaces';
import { PropType, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { MyPagesRoutes } from '@/router/routes';
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import BaseTablePagination from '@/components/base/baseTable/BaseTablePagination.vue';
import moment from 'moment';

defineProps({
	filteredTemplates: {
		type: Array as PropType<IFilteredConsumerTemplate[]>,
		required: true,
	},
	selectedGroupId: {
		type: String as PropType<string | null>,
	},
});

const emit = defineEmits(['selectGroupToOpen']);
const { t } = useI18n();

const SHOW_GROUPS_PER_TEMPLATE = 5;

const page = ref(1);
const itemsPerPage = ref(10);
const sortBy = ref<ISortBy[]>([{ key: 'title', order: 'asc' }]);

const headers: ITableHeader[] = [
	{
		title: t('component.internal.consentConsumerList.headers.title'),
		align: 'start',
		key: 'title',
	},
	{
		title: t('component.internal.consentConsumerList.headers.groups'),
		align: 'start',
		key: 'groups',
	},
	{
		title: t('component.internal.consentConsumerList.headers.period'),
		align: 'start',
		key: 'period',
	},
	{ title: '', key: 'actions', sortable: false },
];
</script>

<style scoped lang="scss">
.consent-consumer-list-table {
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

			&.additional-groups {
				border: none;
				background-color: #fff;
			}
			&.selected {
				outline: dashed 1px $primary;
			}
		}
		.open-button-wrap {
			text-align: right;
			.v-btn {
				color: $white !important;
				min-width: 102px;
				font-size: size(16);
			}
			.v-list {
				text-align: left;
			}
		}
	}

	@media only screen and (max-width: 700px) {
		:deep(table) {
			width: 100%;

			thead {
				display: none;
			}
		}
		.consent-template-item {
			display: flex;
			flex-wrap: wrap;

			border: solid 1px $grey-lighten-3;
			border-radius: $border-radius;
			background-color: $grey-lighten-2;
			margin-bottom: 18px;

			td {
				background-color: transparent;
				display: block;
				border-bottom: none !important;
				height: auto;
				padding: 14px;
				width: 100%;

				&:before {
					display: block;
					content: attr(label);
					font-weight: bold;
				}
			}

			.v-chip {
				background-color: $white;
			}

			.open-button-wrap {
				.v-btn {
					width: 100%;
					min-height: 46px;
				}
			}
		}
	}
}
</style>
