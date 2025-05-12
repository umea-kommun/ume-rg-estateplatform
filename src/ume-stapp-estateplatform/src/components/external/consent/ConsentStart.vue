<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="consent-start"
		:pageTitle="$t('component.consentStart.title')"
	>
		<base-back-button
			:to="{ name: MyPagesRoutes.AppStart, replace: true }"
		/>
		<div v-if="!isBusyLoadingFromServer">
			<v-row class="top-wrap" justify="end">
				<v-col class="pa-0" alignSelf="center">
					<h1>{{ $t('component.consentStart.title') }}</h1>
				</v-col>
				<v-col
					class="select-wrap pa-0"
					v-if="guardianChildren.length > 1"
				>
					<BaseSelectList
						id="listOfChildren"
						v-model="selectedValue"
						:items="guardianChildren"
						:label="$t('component.consentStart.selectListTitle')"
						box
					/>
				</v-col>
			</v-row>
			<v-row class="mt-2">
				<v-alert
					v-if="
						!consentItems.length &&
						selectedValue === showAllItem.value
					"
					icon="warning"
				>
					{{ $t('component.consentStart.noResults') }}
				</v-alert>
				<v-alert
					v-else-if="!consentItems.length && selectedValue"
					icon="warning"
				>
					{{ $t('component.consentStart.noFilterResults') }}
				</v-alert>
				<v-data-table
					v-else
					v-model:items-per-page="itemsPerPage"
					v-model:sort-by="sortBy"
					v-model:page="page"
					:headers="headers"
					:items="consentItems"
					item-value="guid"
					class="mt-3"
					:loading="isBusyRefetchingList && !showConsentModal"
					:loading-text="$t('component.consentStart.reloadingText')"
				>
					<!-- Table headers -->
					<template v-slot:headers>
						<base-table-header
							v-model:sortBy="sortBy"
							:headers="headers"
						/>
					</template>

					<template v-slot:loader>
						<div class="progress-wrap mt-6 mb-4" aria-live="polite">
							<v-progress-circular
								color="primary"
								rounded
								indeterminate
								aria-busy="true"
							></v-progress-circular>
						</div>
					</template>

					<!-- Consent row -->
					<template v-slot:item="{ item }: { item: IConsentItem }">
						<tr class="consent-item">
							<td :label="headers[0].title" class="pt-5 pb-5">
								{{ item.titel }}
							</td>
							<td :label="headers[1].title">
								{{ item.person }}
							</td>
							<td :label="headers[2].title">
								<v-chip
									:class="{
										approved:
											item.status ===
											ConsentStatus.Approved,
										denied:
											item.status ===
											ConsentStatus.Denied,
									}"
									variant="outlined"
								>
									{{ getConsentStatusText(item.status) }}
								</v-chip>
							</td>
							<td style="width: 5%">
								<v-btn
									:variant="
										item.consentIsActive &&
										item.userStatus ===
											UserConsentStatus.NotAnswered
											? 'flat'
											: 'outlined'
									"
									color="primary"
									@click="
										consentModalProps = {
											childSSNo: item.childSSNo,
											templateGuid: item.templateGuid,
										}
									"
									:title="getNavigateButtonText(item)"
								>
									{{ getNavigateButtonText(item) }}
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
							:number-of-items="consentItems.length"
						/>
					</template>
				</v-data-table>
			</v-row>
			<consent-edit-modal
				v-if="showConsentModal && consentModalProps"
				v-model="showConsentModal"
				:templateGuid="consentModalProps.templateGuid"
				:childSSNo="consentModalProps.childSSNo"
				@answerChanged="refetchListData"
			/>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute } from 'vue-router';
import {
	AppContentSize,
	ConsentStatus,
	UserConsentStatus,
} from '@/models/Enums';
import store from '@/store/store';
import { DispatchType } from '@/models/Enums';
import { computed } from 'vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import BaseSelectList from '@/components/base/BaseSelectList.vue';
import BaseTablePagination from '@/components/base/baseTable/BaseTablePagination.vue';
import BaseTableHeader from '@/components/base/baseTable/BaseTableHeader.vue';
import { getConsentStatusText } from '@/utils/utils';
import { useI18n } from 'vue-i18n';
import { IItem, ISortBy, ITableHeader } from '@/models/Interfaces';
import ConsentEditModal from './ConsentEditModal.vue';
import { MyPagesRoutes } from '@/router/routes';

const route = useRoute();
const isBusyLoadingFromServer = ref<boolean>(false);
const isBusyRefetchingList = ref(false);

const { t } = useI18n();

const page = ref(1);
const itemsPerPage = ref(10);

const sortBy = ref<ISortBy[]>([{ key: 'status', order: 'desc' }]);

const headers: ITableHeader[] = [
	{
		title: t('component.consentStart.table.titel'),
		align: 'start',
		key: 'titel',
	},
	{
		title: t('component.consentStart.table.person'),
		align: 'start',
		key: 'person',
	},
	{
		title: t('component.consentStart.table.status'),
		align: 'end',
		key: 'status',
	},
	{
		title: '',
		align: 'end',
		key: 'besvara',
		sortable: false,
	},
];

interface IConsentItem {
	titel: string;
	person: string;
	status: ConsentStatus;
	userStatus: UserConsentStatus;
	consentIsActive: boolean;
	templateGuid: string;
	childSSNo: string;
}

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

const showAllItem: IItem = {
	title: t('component.consentStart.showAll'),
	value: '-showAllItems-',
};
const selectedValue = ref(showAllItem.value);
const guardianChildren = computed(() => {
	if (store.state.guardianUser !== null) {
		const children = store.state.guardianUser.children;
		const showAllElement: IItem[] = [showAllItem];
		return showAllElement.concat(children);
	} else return [];
});
const consentItems = computed(() => {
	const childItem = guardianChildren.value.find(
		(child) => child.value === selectedValue.value
	);

	if (store.state.childConsentList) {
		const x = store.state.childConsentList
			.map<IConsentItem>((element) => {
				return {
					titel: element.title,
					person: element.childName,
					status: element.consentStatus,
					userStatus: element.userStatus,
					consentIsActive: element.isActive,
					templateGuid: element.templateGuid,
					childSSNo: element.childSSNo,
				};
			})
			.filter(
				(element) =>
					!selectedValue.value ||
					selectedValue.value === showAllItem.value ||
					element.person === childItem?.title
			);

		return x;
	} else return [];
});

const getNavigateButtonText = (item: IConsentItem): string => {
	if (!item?.consentIsActive) {
		return t('component.consentStart.table.show');
	}
	switch (item.userStatus) {
		case UserConsentStatus.Approved:
		case UserConsentStatus.Rejected:
			return t('component.consentStart.table.change');
		case UserConsentStatus.NotAnswered:
			return t('component.consentStart.table.answer');
		default:
			return 'ERROR';
	}
};

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.consent-start {
	.progress-wrap {
		display: flex;
		justify-content: center;
		align-items: center;
		width: 100%;
	}
	.headers {
		display: inline-flex;
		align-items: center;
		font-size: size(14);
		color: $grey-darken-1;

		&.start {
			width: 100%;
		}

		.sort-icon {
			opacity: 0;
			transition: all 0.2s ease;
			margin-left: 6px;
		}
		&.sortable {
			cursor: pointer;
		}
		&.sortable:hover .sort-icon {
			opacity: 0.5;
		}
		&.sortable.sorted .sort-icon {
			opacity: 1;
		}
		&.sortable.sorted.asc .sort-icon {
			transform: rotate(180deg);
		}
	}
	td {
		.v-btn {
			text-transform: none;
			letter-spacing: normal;
			font-size: size(16);
			width: 100%;

			&--variant-flat {
				color: $white !important;
				border-color: transparent;
			}
		}
	}
	.top-wrap .select-wrap {
		max-width: 33%;
	}
	.v-chip {
		font-size: size(16);
		padding-left: 16px;
		padding-right: 16px;
		border: solid 1px $grey-lighten-4;
		background-color: $grey-lighten-2;
		height: auto;
		border-radius: 12px;

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

			border: solid 1px $grey-lighten-4;
			border-radius: $border-radius;
			box-shadow: 0px 3px 5px -2px rgba(0, 0, 0, 0.2);
			margin-bottom: 24px;

			.v-chip {
				background-color: transparent;
				padding: 0;
				white-space: break-spaces;
				height: auto;
				border: 0;
				color: $black;
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
.consent-start.app-content {
	:deep(.v-container) {
		padding-top: calc($site-content-vertical-padding - 20px);
	}
}
</style>
