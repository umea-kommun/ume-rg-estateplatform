<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="app-start"
		:pageTitle="$t('component.appStart.title')"
	>
		<div v-if="!isBusyLoadingFromServer">
			<v-row>
				<v-col cols="12" class="pa-0">
					<h1>
						{{
							$t('component.appStart.welcomeMessage', {
								name: user.firstName,
							})
						}}
					</h1>
				</v-col>
			</v-row>
			<v-row>
				<p class="mb-6">
					{{ $t('component.appStart.description') }}
				</p>
			</v-row>
			<v-row>
				<v-col class="pa-0 action-cards">
					<v-card
						:to="{ name: MyPagesRoutes.ConsentStart }"
						class="action-card"
					>
						<v-card-title>
							{{ $t('component.appStart.consent.title') }}
							<v-icon icon="arrow_forward"></v-icon>
						</v-card-title>

						<v-card-text class="mt-4 mb-2">
							<span v-if="unansweredConsents != null">
								{{
									$t(
										'component.appStart.consent.unreadConsents1'
									)
								}}
								<b>{{ unansweredConsents }}</b>
								{{
									$tc(
										'component.appStart.consent.unreadConsents2',
										unansweredConsents
									)
								}}
							</span>
							<span v-else>
								{{
									$t(
										'component.appStart.consent.unreadConsentsNotLoaded'
									)
								}}
							</span>
						</v-card-text>
					</v-card>
					<v-card
						:to="{ name: MyPagesRoutes.KvittensStart }"
						class="action-card"
					>
						<v-card-title>
							{{ $t('component.appStart.kvittens.title') }}
							<v-icon icon="arrow_forward"></v-icon>
						</v-card-title>

						<v-card-text class="mt-4 mb-2">
							<span v-if="unansweredKvittens != null">
								{{
									$t(
										'component.appStart.kvittens.unansweredKvittens1'
									)
								}}
								<b>{{ unansweredKvittens }}</b>
								{{
									$tc(
										'component.appStart.kvittens.unansweredKvittens2',
										unansweredKvittens
									)
								}}
							</span>
							<span v-else>
								{{
									$t(
										'component.appStart.kvittens.unansweredKvittensNotLoaded'
									)
								}}
							</span>
						</v-card-text>
					</v-card>
				</v-col>
			</v-row>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useStore } from 'vuex';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute } from 'vue-router';
import { IRootState } from '@/models/Interfaces';
import {
	AppContentSize,
	DispatchType,
	UserConsentStatus,
} from '@/models/Enums';
import { MyPagesRoutes } from '@/router/routes';

const store = useStore<IRootState>();
const route = useRoute();
const isBusyLoadingFromServer = ref<boolean>(false);

const user = computed(() => {
	return store.state.user;
});

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const unansweredConsents = computed(() => {
	const consents = store.state.childConsentList;
	if (consents) {
		let unanswered = 0;
		consents.forEach((consent) => {
			if (consent.userStatus === UserConsentStatus.NotAnswered) {
				unanswered++;
			}
		});
		return unanswered;
	}
	return null;
});

const unansweredKvittens = computed(() => {
	const kvittensList = store.state.kvittens?.kvittensList;
	if (kvittensList) {
		let unanswered = 0;
		kvittensList.forEach((kvittens) => {
			const answered = kvittens.linkedPersons.find(
				(linkedPerson) =>
					linkedPerson.socialSecurityNumber ===
						store.state.user.socialSecurityNumber &&
					linkedPerson.userHasAnswered
			);
			if (!answered) {
				unanswered++;
			}
		});
		return unanswered;
	}
	return null;
});

onMounted(async () => {
	isBusyLoadingFromServer.value = true;
	await Promise.allSettled([
		store.dispatch(DispatchType.GetConsentList, { hideError: true }),
		store.dispatch(DispatchType.GetKvittensList, { hideError: true }),
	]);
	isBusyLoadingFromServer.value = false;
});
</script>

<style scoped lang="scss">
.app-start {
	.action-cards {
		display: flex;
		flex-wrap: wrap;
		gap: 16px;

		.action-card {
			margin-bottom: 20px;
			flex-basis: 32%;

			@media only screen and (max-width: 1050px) {
				flex-basis: 48%;
			}
			@media only screen and (max-width: 700px) {
				flex-basis: 100%;
			}
			.v-card-title {
				background-color: $primary;
				color: $white;
				display: flex;
				justify-content: space-between;
				align-items: center;
				font-size: size(18);
				white-space: break-spaces;

				.v-icon {
					margin-left: 20px;
				}
			}
			.v-card-text {
				font-size: size(16);
			}
		}
	}
}
</style>
