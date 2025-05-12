<template>
	<v-data-table
		v-model:items-per-page="itemsPerPage"
		v-model:sort-by="sortBy"
		v-model:page="page"
		:headers="headers"
		:items="consents"
		item-value="guid"
		class="mt-3 consent-agent-list"
	>
		<!-- Table headers -->
		<template v-slot:headers>
			<base-table-header v-model:sortBy="sortBy" :headers="headers" />
		</template>

		<!-- Consent row -->
		<template v-slot:item="{ item }: { item: IChildConsent }">
			<tr class="consent-item">
				<td :label="headers[0].title" class="pt-5 pb-5">
					{{ item.title }}
				</td>
				<td :label="headers[2].title">
					<v-chip
						:class="{
							approved:
								item.consentStatus === ConsentStatus.Approved,
							denied: item.consentStatus === ConsentStatus.Denied,
						}"
						variant="outlined"
					>
						{{ getConsentStatusText(item.consentStatus) }}
					</v-chip>
				</td>
				<td style="width: 5%">
					<v-btn
						variant="flat"
						color="primary"
						@click="openConsent(item)"
					>
						{{ $t('component.internal.consentAgentList.answer') }}
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
				:number-of-items="consents.length"
			/>
		</template>
	</v-data-table>
</template>

<script setup lang="ts">
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import BaseTablePagination from '@/components/base/baseTable/BaseTablePagination.vue';
import { ConsentStatus, MutationType } from '@/models/Enums';
import {
	IChildConsent,
	IRootState,
	ISortBy,
	ITableHeader,
} from '@/models/Interfaces';
import { MyPagesRoutes } from '@/router/routes';
import { getConsentStatusText } from '@/utils/utils';
import { PropType, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useStore } from 'vuex';

defineProps({
	consents: {
		type: Array as PropType<IChildConsent[]>,
		required: true,
	},
});

const { t } = useI18n();
const store = useStore<IRootState>();
const router = useRouter();

const page = ref(1);
const itemsPerPage = ref(10);

const sortBy = ref<ISortBy[]>([{ key: 'consentStatus', order: 'desc' }]);

const headers: ITableHeader[] = [
	{
		title: t('component.consentStart.table.titel'),
		align: 'start',
		key: 'titel',
	},
	{
		title: t('component.consentStart.table.status'),
		align: 'end',
		key: 'consentStatus',
	},
	{
		title: '',
		align: 'end',
		key: 'besvara',
		sortable: false,
	},
];

const openConsent = (consent: IChildConsent) => {
	store.commit(MutationType.OpenConsentAgentEdit, consent);
	router.push({
		name: MyPagesRoutes.InternalConsentAgentEdit,
	});
};
</script>

<style scoped lang="scss">
.consent-agent-list {
	.consent-item .v-chip {
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

	@media only screen and (max-width: 700px) {
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
		.consent-item {
			display: flex;
			flex-wrap: wrap;
			font-size: size(16);

			border: solid 1px $grey-lighten-4;
			border-radius: $border-radius;
			box-shadow: 0px 3px 5px -2px rgba(0, 0, 0, 0.2);
			margin-bottom: 24px;

			.v-chip {
				background-color: transparent !important;
				padding: 0;
				white-space: break-spaces;
				height: auto;
				border: 0;
				color: $black !important;
			}

			td {
				background-color: transparent;
				display: block;
				border-bottom: none !important;
				height: auto;
				padding: 14px;
				width: 100% !important;

				.v-btn {
					margin: 0;
					width: 100%;
					padding-top: 14px;
					padding-bottom: 14px;
					height: auto;
				}

				&:before {
					display: block;
					content: attr(label);
					font-weight: bold;
				}
			}
		}
	}
}
</style>
