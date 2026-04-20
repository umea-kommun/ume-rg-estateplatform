<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="consent-consumer-details"
		:pageTitle="
			templateWithConsents?.title ??
			$t('component.internal.consentConsumerDetails.title')
		"
	>
		<consumer-tester />
		<base-back-button />
		<div v-if="!isBusyLoadingFromServer && templateWithConsents">
			<v-row>
				<v-col class="pa-0">
					<h1 class="my-0">
						<span class="mr-3 mb-3">{{
							templateWithConsents?.title ??
							$t(
								'component.internal.consentConsumerDetails.title'
							)
						}}</span>
						<v-chip variant="outlined">{{
							templateWithConsents.group.title
						}}</v-chip>
					</h1>
				</v-col>
			</v-row>
			<v-row class="ma-0">
				<v-col class="pt-0">
					<div v-html="templateWithConsents?.text"></div>
				</v-col>
			</v-row>
			<hr class="mb-4 mt-4" />
			<v-row>
				<v-col class="pa-0">
					<h2 class="my-0">
						{{
							$t(
								'component.internal.consentConsumerDetails.persons'
							)
						}}
					</h2>
				</v-col>
			</v-row>
			<v-row>
				<v-col class="pa-0">
					<v-text-field
						id="search"
						v-model="searchValue"
						:label="
							$t(
								'component.internal.consentConsumerDetails.searchForPersons'
							)
						"
						prependInnerIcon="search"
						density="comfortable"
						color="primary"
						clearable
						hide-details
					/>
				</v-col>
			</v-row>
			<v-row>
				<v-col class="pa-0">
					<v-alert
						v-if="!templateWithConsents?.consents.length"
						icon="warning"
					>
						{{
							$t(
								'component.internal.consentConsumerDetails.noResults',
								[templateWithConsents?.group.title]
							)
						}}
					</v-alert>
					<v-alert
						v-else-if="!filteredConsents.length"
						icon="warning"
					>
						{{
							$t(
								'component.internal.consentConsumerDetails.noSearchResults'
							)
						}}
					</v-alert>
					<!-- Display list of templates-->
					<v-data-table
						v-else
						:items-per-page="-1"
						:page="1"
						v-model:sort-by="sortBy"
						:headers="headers"
						:items="filteredConsents"
					>
						<!-- Table headers -->
						<template v-slot:headers>
							<base-table-header
								v-model:sortBy="sortBy"
								:headers="headers"
							/>
						</template>

						<!-- Form item -->
						<template
							v-slot:item="{ item }: { item: IFilteredConsent }"
						>
							<tr class="consent-template-item">
								<td>
									{{ item.name }}
								</td>
								<td>
									<v-chip
										variant="outlined"
										class="status-chip"
										:class="{
											approved:
												item.status ===
												ConsentStatus.Approved,
											denied:
												item.status ===
												ConsentStatus.Denied,
											pending:
												item.status ===
												ConsentStatus.Pending,
										}"
									>
										{{ statusText(item.status) }}
									</v-chip>
								</td>
							</tr>
						</template>
						<template v-slot:bottom></template>
					</v-data-table>
				</v-col>
			</v-row>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute } from 'vue-router';
import { AppContentSize, ConsentStatus, DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState, ISortBy, ITableHeader } from '@/models/Interfaces';
import { useI18n } from 'vue-i18n';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import ConsumerTester from '@/components/internal/shared/ConsumerTester.vue';

const route = useRoute();
const store = useStore<IRootState>();
const { t } = useI18n();

const contentSize = ref(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const props = defineProps({
	templateGuid: {
		type: String,
		required: true,
	},
	groupId: {
		type: String,
		required: true,
	},
});

const isBusyLoadingFromServer = ref<boolean>(false);
const searchValue = ref('');
const sortBy = ref<ISortBy[]>([
	{ key: 'status', order: 'desc' },
	{ key: 'name', order: 'asc' },
]);

const headers: ITableHeader[] = [
	{
		title: t('component.internal.consentConsumerDetails.headers.name'),
		align: 'start',
		key: 'name',
	},
	{
		title: t('component.internal.consentConsumerDetails.headers.consent'),
		align: 'start',
		key: 'status',
	},
];

const templateWithConsents = computed(
	() => store.state.consumer.templateWithConsents
);

interface IFilteredConsent {
	consentGuid?: string;
	name: string;
	status: ConsentStatus;
}

const filteredConsents = computed<IFilteredConsent[]>(() => {
	if (templateWithConsents.value?.consents) {
		let consents = templateWithConsents.value.consents;

		if (searchValue.value) {
			consents = consents.filter((consent) => {
				return (
					consent.name
						.toLowerCase()
						.indexOf(searchValue.value.toLowerCase()) > -1
				);
			});
		}

		return consents;
	}
	return [];
});

const statusText = (status: ConsentStatus): string => {
	switch (status) {
		case ConsentStatus.Approved:
			return t(
				'component.internal.consentConsumerDetails.status.approved'
			);
		case ConsentStatus.Denied:
			return t('component.internal.consentConsumerDetails.status.denied');
		case ConsentStatus.Pending:
			return t(
				'component.internal.consentConsumerDetails.status.pending'
			);
		case ConsentStatus.New:
			return t('component.internal.consentConsumerDetails.status.new');
	}
	return '';
};

onMounted(async () => {
	isBusyLoadingFromServer.value = true;
	await store.dispatch(DispatchType.GetConsentConsumerTemplateWithConsents, {
		templateGuid: props.templateGuid,
		groupId: props.groupId,
	});

	isBusyLoadingFromServer.value = false;
});
</script>
<style scoped lang="scss">
.consent-consumer-details {
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
	h1 {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		span {
			margin-right: 10px;
			margin-bottom: 10px;
		}
		.v-chip {
			font-size: size(16);
			border: solid 1px $grey-lighten-4;
			background-color: $grey-lighten-2;
		}
	}
	.v-row {
		.v-col {
			&:first-child {
				padding-left: 0;
			}
			&:last-child {
				padding-right: 0;
			}

			.status-chip {
				font-size: size(16);
				padding-left: 16px;
				padding-right: 16px;
				border: solid 1px $grey-lighten-4;
				background-color: $grey-lighten-2;
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

	hr {
		border: solid 1px $grey-lighten-3;
	}
	&.app-content {
		:deep(.v-container) {
			padding-top: calc($site-content-vertical-padding - 20px);
		}
	}
}
</style>
