<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="internal-start"
		:pageTitle="$t('component.appStart.title')"
	>
		<div v-if="!isBusyLoadingFromServer">
			<v-row row class="pa-0">
				<v-col class="pa-0">
					<h1>
						{{
							$t('component.internal.internalStart.title', {
								name: store.state.user.firstName,
							})
						}}
					</h1>
					<p class="mt-4">
						{{ $t('component.internal.internalStart.description') }}
					</p>
				</v-col>
			</v-row>
			<v-row
				v-if="isConsentConsumer || isConsentTemplateAdmin"
				class="mt-4"
			>
				<v-col cols="12" class="pa-0">
					<hr class="mb-4" />
					<h2 class="mb-4">
						{{
							$t('component.internal.internalStart.consent.title')
						}}
					</h2>
					<v-btn
						v-if="isConsentConsumer"
						variant="outlined"
						size="x-large"
						prependIcon="playlist_add_check"
						:to="{
							name: MyPagesRoutes.InternalConsentConsumerList,
						}"
					>
						{{
							$t(
								'component.internal.internalStart.consent.goToConsumer'
							)
						}}
					</v-btn>
					<v-btn
						v-if="isConsentConsumer"
						variant="outlined"
						size="x-large"
						prependIcon="post_add"
						:to="{
							name: MyPagesRoutes.InternalConsentAgentStart,
						}"
					>
						{{
							$t(
								'component.internal.internalStart.consent.goToAgent'
							)
						}}
					</v-btn>
					<v-btn
						v-if="isConsentTemplateAdmin"
						variant="outlined"
						size="x-large"
						prependIcon="text_snippet"
						:to="{
							name: MyPagesRoutes.InternalConsentTemplateList,
						}"
					>
						{{
							$t(
								'component.internal.internalStart.consent.goToTemplateAdmin'
							)
						}}
					</v-btn>
				</v-col>
			</v-row>
			<v-row class="mt-4" v-if="isKvittensConsumer">
				<v-col cols="12" class="pa-0">
					<hr class="mb-4" />
					<h2 class="mb-4">
						{{
							$t(
								'component.internal.internalStart.kvittens.title'
							)
						}}
					</h2>
					<v-btn
						variant="outlined"
						size="x-large"
						prependIcon="playlist_add_check"
						:to="{
							name: MyPagesRoutes.InternalKvittensSummary,
						}"
					>
						{{
							$t(
								'component.internal.internalStart.kvittens.goToKvittens'
							)
						}}
					</v-btn>
					<v-btn
						variant="outlined"
						size="x-large"
						prependIcon="post_add"
						:to="{
							name: MyPagesRoutes.InternalKvittensAgent,
						}"
					>
						{{
							$t(
								'component.internal.internalStart.kvittens.goToKvittensAgent'
							)
						}}
					</v-btn>
				</v-col>
			</v-row>
			<v-row class="mt-4" v-if="isPasswordConsumer">
				<v-col cols="12" class="pa-0">
					<hr class="mb-4" />
					<h2 class="mb-4">
						{{
							$t(
								'component.internal.internalStart.passwords.title'
							)
						}}
					</h2>
					<v-btn
						variant="outlined"
						size="x-large"
						prependIcon="playlist_add_check"
						:to="{
							name: MyPagesRoutes.InternalDefaultPassword,
						}"
					>
						{{
							$t(
								'component.internal.internalStart.passwords.goToPassword'
							)
						}}
					</v-btn>
				</v-col>
			</v-row>
			<v-row class="mt-4" v-if="isEstateEnabled">
				<v-col cols="12" class="pa-0">
					<hr class="mb-4" />
					<h2 class="mb-2">
						{{
							$t('component.internal.internalStart.estate.title')
						}}
					</h2>
					<p class="mb-4">
						{{
							$t(
								'component.internal.internalStart.estate.description'
							)
						}}
					</p>
					<v-btn
						variant="outlined"
						size="x-large"
						prependIcon="domain"
						:to="{
							name: EstateRoutes.Search,
						}"
					>
						{{
							$t(
								'component.internal.internalStart.estate.goToEstateSearch'
							)
						}}
					</v-btn>
				</v-col>
			</v-row>
		</div>
		<rate-feedback
			:feedback-title="
				$t('component.internal.internalStart.feedbackTitle')
			"
			:feedback-subtitle="
				$t('component.internal.internalStart.feedbackSubtitle')
			"
			category="internalPagesOverview"
		/>
	</app-content>
</template>
<script setup lang="ts">
import { ref, computed } from 'vue';
import { useStore } from 'vuex';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute } from 'vue-router';
import { IRootState } from '@/models/Interfaces';
import { AppContentSize } from '@/models/Enums';
import { EstateRoutes, MyPagesRoutes } from '@/router/routes';
import Config from '@/Config';
import RateFeedback from '@/components/feedback/RateFeedback.vue';

const store = useStore<IRootState>();
const route = useRoute();
const isBusyLoadingFromServer = ref<boolean>(false);

const user = computed(() => store.state.user);
const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

// Check if the user is a consumer, we could make these into a global vuex getter later (if it is used a lot)
const isConsentConsumer = computed(() => {
	if (user.value.groups) {
		return user.value.groups.includes(
			Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID
		);
	}
	return false;
});

const isConsentTemplateAdmin = computed(() => {
	if (user.value.groups) {
		return user.value.groups.includes(
			Config.VUE_APP_AUTH_GROUP_CONSENT_TEMPLATE_ID
		);
	}
	return false;
});

const isKvittensConsumer = computed(() => {
	if (user.value.groups) {
		return user.value.groups.includes(
			Config.VUE_APP_AUTH_GROUP_KVITTENS_CONSUMER_ID
		);
	}
	return false;
});

const isPasswordConsumer = computed(() => {
	if (user.value.groups) {
		return user.value.groups.includes(
			Config.VUE_APP_AUTH_GROUP_PASSWORD_CONSUMER_ID
		);
	}
	return false;
});
const isEstateEnabled = computed(() => {
	return Config.VUE_APP_ESTATE_ENABLED === 'true';
});
</script>
<style scoped lang="scss">
.internal-start {
	hr {
		border: solid 1px $grey-lighten-3;
	}
	.v-alert {
		border-radius: $border-radius;
		:deep(.v-alert-title) {
			margin-bottom: 8px;
		}
	}
	.v-btn {
		letter-spacing: normal;
		text-transform: none;
		margin: 0;
		margin-bottom: 14px;
		margin-right: 14px;
	}
}
</style>
