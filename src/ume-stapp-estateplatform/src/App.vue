<!-- Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/App.vue @ 84b4a5dc -->
<template>
	<v-app id="app" :class="'route-' + routeName">
		<app-error-snackbar />

		<!-- Render any confirm dialogs -->
		<t-confirm-dialog />

		<!-- Display warning about automatic logout due to inactivity or log out message -->
		<auth-notification />

		<!-- Cookie Banner -->
		<t-cookie-banner :url-cookies-info="Config.VUE_APP_COOKIE_URL" />

		<!-- App Header -->
		<app-header :size="contentSize" />

		<!-- Sizes your content based upon application components -->
		<v-main id="main-content">
			<!-- Render component per route -->
			<div class="route-view-container">
				<app-error v-if="isFullPageError" />
				<router-view v-show="!isFullPageError" :size="contentSize" />
			</div>
		</v-main>
		<app-footer :size="contentSize" />
	</v-app>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router';
import AppErrorSnackbar from './components/app/AppErrorSnackbar.vue';
import AppError from './components/app/AppError.vue';
import AppHeader from '@/components/app/AppHeader.vue';
import AppFooter from '@/components/app/AppFooter.vue';
import AuthNotification from '@/components/auth/AuthNotification.vue';
import { AppContentSize } from '@/models/Enums';
import { computed } from 'vue';
import { TConfirmDialog, TCookieBanner } from '@turkos/components';
import Config from '@/utils/Config';
import { useStore } from 'vuex';
import { IRootState } from './models/Interfaces';

const store = useStore<IRootState>();
const route = useRoute();
const routeName = route.name?.toString();

const isFullPageError = computed(() => {
	return store.state.error?.errorPage?.visible;
});

const contentSize = computed(() => {
	return route.meta.contentSize as AppContentSize;
});
</script>

<style scoped lang="scss">
.route-view-container {
	// prevents footer from jumping up and down when navigating between routes
	-webkit-box-flex: 1;
	-ms-flex: 1 1 auto;
	flex: 1 1 auto;
	max-width: 100%;
	position: relative;
}
</style>
