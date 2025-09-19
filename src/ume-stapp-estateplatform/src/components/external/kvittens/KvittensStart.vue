<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="kvittens-start"
		:pageTitle="$t('component.external.kvittensStart.title')"
	>
		<base-back-button
			:to="{ name: MyPagesRoutes.AppStart, replace: true }"
		/>
		<div class="d-flex flex-wrap justify-space-between top-wrap">
			<h1>{{ $t('component.external.kvittensStart.title') }}</h1>
			<div
				v-if="children.length"
				class="d-flex justify-end align-center mb-2 filter-on-child"
			>
				<v-select
					v-model="filterOnChildSSNo"
					:items="children"
					clearable
					:label="
						$t('component.external.kvittensStart.filterOnChild')
					"
					variant="outlined"
					density="comfortable"
					color="primary"
					item-title="name"
					item-value="socialSecurityNumber"
					hide-details
				/>
			</div>
		</div>
		<p>
			{{ $t('component.external.kvittensStart.description') }}
		</p>

		<non-folkbokford-alert :children="children" />
		<v-alert
			v-if="!kvittensList.length && filterOnChildSSNo"
			icon="warning"
			class="mt-6"
		>
			{{ $t('component.external.kvittensStart.noResultsUsingFilter') }}
		</v-alert>
		<v-alert v-else-if="!kvittensList.length" icon="warning" class="mt-6">
			{{ $t('component.external.kvittensStart.noResults') }}
		</v-alert>

		<div v-if="unansweredKvittensList.length" class="mb-12">
			<h2 class="mt-6 mb-5">
				{{
					$t('component.external.kvittensStart.titleUnanswered', {
						count: unansweredKvittensList.length,
					})
				}}
			</h2>
			<kvittens-list-item
				v-for="kvittens in unansweredKvittensList"
				:key="kvittens.localId"
				:kvittens="kvittens"
				:kvittensForMultiplePersons="kvittensForMultiplePersons"
			/>
		</div>

		<div v-if="answeredKvittensList.length">
			<h2 class="mt-6 mb-5">
				{{
					$t('component.external.kvittensStart.titleAnswered', {
						count: answeredKvittensList.length,
					})
				}}
			</h2>
			<kvittens-list-item
				v-for="kvittens in answeredKvittensList"
				:key="kvittens.localId"
				:kvittens="kvittens"
				:kvittensForMultiplePersons="kvittensForMultiplePersons"
			/>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import { useRoute } from 'vue-router';
import { AppContentSize } from '@/models/Enums';
import store from '@/store/store';
import { DispatchType } from '@/models/Enums';
import { computed } from 'vue';
import KvittensListItem from '@/components/external/kvittens/KvittensListItem.vue';
import { MyPagesRoutes } from '@/router/routes';
import NonFolkbokfordAlert from '@/components/external/common/NonFolkbokfordAlert.vue';

const route = useRoute();
const isBusyLoadingFromServer = ref<boolean>(false);
const filterOnChildSSNo = ref<string>();

const kvittensList = computed(() => {
	const unfilteredKvittensList = store.state.kvittens?.kvittensList ?? [];
	if (filterOnChildSSNo.value) {
		return unfilteredKvittensList.filter(
			(kvittens) => kvittens.personSSNo === filterOnChildSSNo.value
		);
	}
	return unfilteredKvittensList;
});
const children = computed(() => store.state.guardianUser?.children ?? []);

const kvittensForMultiplePersons = computed(() => {
	if (kvittensList.value?.length) {
		const kvittensPersonSSNo = kvittensList.value[0].personSSNo;
		return !kvittensList.value.every(
			(kvittens) => kvittens.personSSNo === kvittensPersonSSNo
		);
	}
	return false;
});

const unansweredKvittensList = computed(() =>
	kvittensList.value.filter((kvittens) =>
		kvittens.linkedPersons.find(
			(linkedPerson) =>
				linkedPerson.socialSecurityNumber ===
					store.state.user.socialSecurityNumber &&
				!linkedPerson.userHasAnswered
		)
	)
);

const answeredKvittensList = computed(() =>
	kvittensList.value.filter((kvittens) =>
		kvittens.linkedPersons.find(
			(linkedPerson) =>
				linkedPerson.socialSecurityNumber ===
					store.state.user.socialSecurityNumber &&
				linkedPerson.userHasAnswered
		)
	)
);

onMounted(async () => {
	isBusyLoadingFromServer.value = true;
	await Promise.all([
		store.dispatch(DispatchType.GetChildren),
		store.dispatch(DispatchType.GetKvittensList),
	]);
	isBusyLoadingFromServer.value = false;
});

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.kvittens-start {
	.top-wrap {
		h1 {
			font-size: size(38);
		}
		.filter-on-child {
			min-width: 200px;
			:deep(.v-field__input .v-select__selection) {
				padding: 0 !important;
			}
		}

		@media only screen and (max-width: 700px) {
			h1 {
				width: 100%;
			}
			.filter-on-child {
				flex: 1;
				margin: 10px 0;
				padding-bottom: 10px;
			}
		}
	}
	.kvittens-list-item {
		margin-bottom: 16px;
	}
}
.kvittens-start.app-content {
	:deep(.v-container) {
		padding-top: calc($site-content-vertical-padding - 20px);
	}
}
</style>
