<template>
	<app-content
		:size="contentSize"
		:pageTitle="$t('component.pageNotFound.pageTitle')"
		class="not-found-page"
	>
		<div class="text-center">
			<h1 class="mt-2 mb-4">{{ $t('component.pageNotFound.title') }}</h1>
			<p>{{ $t('component.pageNotFound.text') }}</p>

			<v-btn
				flat
				color="primary"
				class="mt-4"
				:to="{ name: startPageRoute }"
				>{{ $t('app.nav.startPage') }}</v-btn
			>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { ref, computed } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute } from 'vue-router';
import { AppContentSize } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import Config from '@/Config';
import { MyPagesRoutes } from '@/router/routes';

const route = useRoute();
const store = useStore<IRootState>();

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const user = computed(() => store.state.user);
const startPageRoute = computed(() => {
	switch (user.value.authClientName) {
		case Config.VUE_APP_AUTH_PUBLIC_AD_CLIENT_NAME:
			// Redirect to internal start page
			return MyPagesRoutes.InternalStart;
		default:
			// Redirect to external start page
			return MyPagesRoutes.AppStart;
	}
});
</script>

<style scoped lang="scss">
.not-found-page {
	.v-btn {
		font-size: size(16);
		text-transform: none;
		letter-spacing: normal;
		color: $white !important;
	}
}
</style>
