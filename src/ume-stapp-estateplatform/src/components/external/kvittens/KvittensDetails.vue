<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="kvittens-details"
		:pageTitle="
			(kvittens ? kvittens.title + ' - ' : '') +
			$t('component.external.kvittensStart.title')
		"
	>
		<base-back-button />
		<div v-if="kvittens">
			<h1 class="mb-4 mt-2">{{ kvittens.title }}</h1>
			<div class="kvittens-text" v-html="kvittens.text"></div>
			<kvittens-gdpr-modal
				v-model="showGdprModal"
				:gdpr-text="kvittens.gpdrText"
			/>
			<div class="d-flex justify-center">
				<v-btn
					@click="showGdprModal = true"
					class="mt-3 mb-3 gdpr-button"
				>
					{{ $t('component.external.kvittensDetails.gdprButton') }}
				</v-btn>
			</div>
			<hr class="mb-8 mt-6" aria-hidden="true" />
			<v-checkbox
				:label="kvittens.confirmText"
				id="user-agreed"
				color="primary"
				v-model="userAgreed"
				:disabled="userHasAnswered"
				:error="!userAgreed && userAgreedError"
				aria-describedby="user-agreed-error"
			/>
			<div id="user-agreed-error">
				<div v-if="!userAgreed && userAgreedError" class="mt-5">
					<v-icon icon="error" aria-hidden="true" class="mr-1" />
					{{
						$t('component.external.kvittensDetails.userAgreedError')
					}}
				</div>
			</div>

			<div
				v-if="isBusySendingToServer"
				class="d-flex justify-center flex-wrap mt-12 mb-12"
			>
				<v-progress-circular
					:width="3"
					color="green"
					indeterminate
					aria-busy="true"
				></v-progress-circular>
			</div>
			<div
				v-else-if="!userHasAnswered"
				class="response-buttons d-flex justify-center flex-wrap mt-10 ga-4"
			>
				<v-btn
					flat
					variant="outlined"
					:to="{ name: MyPagesRoutes.KvittensStart, replace: true }"
				>
					{{ $t('component.external.kvittensDetails.cancel') }}
				</v-btn>
				<v-btn color="primary" flat @click="sendInAnswer">
					{{ $t('component.external.kvittensDetails.sendIn') }}
				</v-btn>
			</div>

			<div
				v-else
				class="user-has-answered mt-10 d-flex justify-space-between align-center flex-wrap"
			>
				<div class="d-flex align-center">
					<v-icon
						icon="check_circle_outline"
						color="primary"
						:size="32"
					/>
					<b class="ml-3">{{
						$t(
							'component.external.kvittensDetails.thankYouForAnswer'
						)
					}}</b>
				</div>
				<v-btn
					v-if="nextKvittensToAnswer"
					color="primary"
					append-icon="arrow_forward"
					size="large"
					:to="{
						name: MyPagesRoutes.KvittensDetails,
						params: {
							localId: nextKvittensToAnswer.localId,
						},
						replace: true,
					}"
					>{{
						$t(
							'component.external.kvittensDetails.answerNextKvittens'
						)
					}}</v-btn
				>
			</div>

			<div
				v-if="
					kvittensNeedsMultipleAnswers ||
					kvittensSentInAnswersCount === kvittens.linkedPersons.length
				"
			>
				<hr class="mt-8" aria-hidden="true" />
				<h2 class="mt-8 mb-4">
					{{
						kvittensSentInAnswersCount ===
						kvittens.linkedPersons.length
							? $t(
									'component.external.kvittensDetails.everyoneHasAnswered'
							  )
							: $t(
									'component.external.kvittensDetails.requiresMultipleAnswers'
							  )
					}}
				</h2>
				<v-card
					v-for="linkedPerson in kvittens.linkedPersons"
					:key="linkedPerson.socialSecurityNumber"
					class="mb-4 mt-2"
				>
					<v-card-text class="d-flex align-center flex-wrap">
						<span class="mr-4">
							<b>{{ linkedPerson.name }}</b>
							<span
								v-if="
									linkedPerson.socialSecurityNumber ===
									store.state.user.socialSecurityNumber
								"
								class="ml-1"
							>
								{{
									$t('component.external.kvittensDetails.you')
								}}
							</span>
						</span>
						<kvittens-user-answer
							:user-has-answered="linkedPerson.userHasAnswered"
						/>
					</v-card-text>
				</v-card>
			</div>
			<div v-if="kvittens.history.length">
				<h2 class="mb-4 mt-10">
					{{ $t('component.external.kvittensDetails.history.title') }}
				</h2>
				<kvittens-history :history="kvittens.history" />
			</div>
			<div
				v-if="userHasAnswered"
				class="d-flex justify-center flex-wrap mt-8"
			>
				<base-back-button />
			</div>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import KvittensUserAnswer from '@/components/external/kvittens/KvittensUserAnswer.vue';
import KvittensHistory from '@/components/external/kvittens/KvittensHistory.vue';
import KvittensGdprModal from '@/components/external/kvittens/KvittensGdprModal.vue';
import { useRoute, useRouter } from 'vue-router';
import { AppContentSize, DispatchType, MutationType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { MyPagesRoutes } from '@/router/routes';
import { IKvittensDetails } from '@/models/kvittens/Interfaces';

const props = defineProps({
	localId: {
		type: String,
		required: true,
	},
});

const store = useStore<IRootState>();
const route = useRoute();
const router = useRouter();
const isBusyLoadingFromServer = ref<boolean>(false);
const isBusySendingToServer = ref<boolean>(false);
const showGdprModal = ref(false);

const kvittens = ref<IKvittensDetails | null>(null);

const userAgreed = ref(false);
const userAgreedError = ref(false);

const userHasAnswered = computed(
	() =>
		kvittens.value?.linkedPersons.find(
			(linkedPerson) =>
				linkedPerson.socialSecurityNumber ===
				store.state.user.socialSecurityNumber
		)?.userHasAnswered
);
const kvittensNeedsMultipleAnswers = computed(
	() => (kvittens.value?.linkedPersons.length ?? 0) > 1
);

const kvittensSentInAnswersCount = computed(
	() =>
		kvittens.value?.linkedPersons.filter(
			(linkedPerson) => linkedPerson.userHasAnswered
		).length
);

const nextKvittensToAnswer = computed(() => {
	return store.state.kvittens?.kvittensList?.find(
		(kvittens) =>
			kvittens.localId !== props.localId &&
			kvittens.linkedPersons.find(
				(linkedPerson) =>
					linkedPerson.socialSecurityNumber ===
						store.state.user.socialSecurityNumber &&
					linkedPerson.userHasAnswered === false
			)
	);
});

const sendInAnswer = async () => {
	if (kvittens.value && !userHasAnswered.value) {
		if (!userAgreed.value) {
			userAgreedError.value = true;
			document.getElementById('user-agreed')?.focus();
		} else {
			isBusySendingToServer.value = true;
			const updatedKvittens = await store.dispatch(
				DispatchType.SaveKvittensAnswer,
				{
					personSSNo: kvittens.value.personSSNo,
					templateId: kvittens.value.templateId,
				}
			);

			if (updatedKvittens) {
				kvittens.value = updatedKvittens;
			}

			store.commit(MutationType.UpdateAnswerInKvittensList, {
				localId: props.localId,
				linkedPersonSSN: store.state.user.socialSecurityNumber,
				hasAnswered: true,
			});
			isBusySendingToServer.value = false;
		}
	}
};

const loadKvittens = async () => {
	isBusyLoadingFromServer.value = true;

	// Reset data
	kvittens.value = null;
	userAgreed.value = false;
	userAgreedError.value = false;

	// Find the kvittens with the matching localId in state
	// We use the localId since we don't want to put SSN in the url and the real id is not unique
	const kvittensInStore = store.state.kvittens?.kvittensList?.find(
		(k) => k.localId === props.localId
	);
	if (kvittensInStore) {
		kvittens.value = await store.dispatch(DispatchType.GetKvittensDetails, {
			personSSNo: kvittensInStore.personSSNo,
			templateId: kvittensInStore.templateId,
		});
		if (userHasAnswered.value) {
			userAgreed.value = true;
		}
	} else {
		// Go back to kvittens start (router.back if possible to not break history)
		const routes = router.getRoutes();
		const previousRoutePath = router.options.history.state.back;
		const previousRoute = routes.find(
			(route) => route.path === previousRoutePath
		);
		if (previousRoute?.name === MyPagesRoutes.KvittensStart) {
			router.back();
		} else {
			router.replace({
				name: MyPagesRoutes.KvittensStart,
			});
		}
	}
	isBusyLoadingFromServer.value = false;
};

watch(() => props.localId, loadKvittens, { immediate: true });

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.kvittens-details {
	h2 {
		font-size: size(20);
	}
	hr {
		border: solid 1px $grey-lighten-3;
	}
	:deep(.kvittens-text) {
		h2 {
			font-size: size(20);
			margin-top: 10px;
		}
		p {
			margin: 10px 0 20px;
		}
		ul,
		ol {
			list-style-position: outside;
			margin: 10px 0 20px;

			li {
				margin-bottom: 5px;
			}
		}
	}
	:deep(.gdpr-button .v-btn__content) {
		white-space: normal;
	}
	:deep(.v-checkbox) {
		.v-selection-control {
			align-items: start;
		}
		.v-label {
			opacity: 1;
		}
	}
	.response-buttons .v-btn {
		font-size: size(16);
		height: auto;
		padding: 10px 30px;
	}
	#user-agreed-error {
		color: $error;
		& > div {
			display: flex;
			align-items: center;
		}
	}
	.user-has-answered {
		background-color: $grey-lighten-3;
		padding: 16px;
		border-radius: $border-radius;
		row-gap: 16px;
		column-gap: 16px;
		a {
			color: $white !important;
			:deep(.v-icon) {
				margin-top: 4px;
			}
		}
		@media screen and (max-width: 700px) {
			& > * {
				flex: auto;
			}
		}
	}
}
.kvittens-details.app-content {
	:deep(.v-container) {
		padding-top: calc($site-content-vertical-padding - 20px);
	}
}
</style>
