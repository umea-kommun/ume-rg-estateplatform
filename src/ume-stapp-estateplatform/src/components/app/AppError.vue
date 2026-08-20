<template>
	<app-content
		:size="AppContentSize.Narrow"
		class="app-error"
		:pageTitle="$t('component.external.kvittensStart.title')"
	>
		<base-back-button />
		<div v-if="error">
			<v-icon icon="error_outline" class="mb-4 mt-8" :size="48" />
			<h1 class="mt-2">
				{{ errorTitle }}
			</h1>
			<p class="mt-2" v-if="errorMessage">
				{{ errorMessage }}
			</p>
			<div class="d-flex justify-center mt-4 ga-4">
				<v-btn
					target="_BLANK"
					v-if="!errorPage?.hideReport"
					:href="errorReportUrl"
				>
					{{ t('component.appError.reportError') }}
				</v-btn>
				<v-btn
					:to="{
						name: MyPagesRoutes.AppStart,
					}"
				>
					{{ t('app.nav.startPage') }}
				</v-btn>
			</div>
			<div class="time mt-4">
				{{
					t('component.appError.time', {
						time: moment().format('YYYY-MM-DD HH:mm:ss'),
					})
				}}
			</div>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import AppContent from './AppContent.vue';
import BaseBackButton from '../base/BaseBackButton.vue';
import { computed } from 'vue';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { useI18n } from 'vue-i18n';
import { AppContentSize } from '@/models/Enums';
import { MyPagesRoutes } from '@/router/routes';
import Config from '@/Config';
import moment from 'moment';

const store = useStore<IRootState>();
const { t } = useI18n();

const errorReportUrl = computed(() => Config.VUE_APP_ERROR_REPORT_URL);

const error = computed(() => {
	return store.state.error;
});

const errorPage = computed(() => error.value?.errorPage);

const errorTitle = computed(() => {
	if (errorPage.value?.title) {
		return errorPage.value.title;
	} else if (errorPage.value?.titleKey) {
		return t(errorPage.value.titleKey);
	}
	return t('app.error.general');
});

const errorMessage = computed(() => {
	if (errorPage.value?.message) {
		return errorPage.value.message;
	} else if (errorPage.value?.messageKey) {
		return t(errorPage.value.messageKey);
	} else if (!errorPage.value?.hideReport) {
		// Only show default error message if report button is visible (since it urges the user to report)
		return t('component.appError.defaultMessage');
	}
	return null;
});
</script>

<style scoped lang="scss">
.app-error {
	text-align: center;
	p {
		font-size: size(18);
	}
	.time {
		font-size: size(14);
		color: $grey-lighten-5;
	}
}
</style>
