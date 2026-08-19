<!-- Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/components/auth/AuthCallback.vue @ 84b4a5dc -->
<template>
	<div class="auth-callback">
		<app-loading-spinner :isVisible="true"></app-loading-spinner>
	</div>
</template>
<script setup lang="ts">
import { onMounted, inject } from 'vue';
import { useRouter } from 'vue-router';
import { useStore } from 'vuex';
import IAuthManager from '@/plugins/auth/IAuthManager';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { IRootState } from '@/models/Interfaces';
import Config from '@/Config';
import { EstateRoutes } from '@/router/routes';

const store = useStore<IRootState>();
const $auth = inject('$auth') as IAuthManager;
const router = useRouter();

onMounted(async () => {
	if (store.state.user.isAuthenticated) {
		window.location.href = '/';
	} else {
		const urlParams = new URLSearchParams(window.location.search);
		const state = urlParams.get('state') || '';
		const code = urlParams.get('code') || '';

		try {
			const afterLoginPath = await $auth.handleLoginCallbackAsync(
				state,
				code
			);

			// If no specific route specified, redirect to fitting page
			if (afterLoginPath === '/') {
				switch (store.state.user.authClientName) {
					case Config.VUE_APP_AUTH_PUBLIC_AD_CLIENT_NAME:
						// Redirect to internal start page
						router.push({ name: EstateRoutes.Search });
						break;
					case Config.VUE_APP_AUTH_PUBLIC_FREJA_CLIENT_NAME:
					case Config.VUE_APP_AUTH_PUBLIC_CLIENT_NAME:
					case Config.VUE_APP_AUTH_PUBLIC_ELEVAD_CLIENT_NAME:
						// Redirect to external start page
						router.push({ name: EstateRoutes.Search });
						break;
				}
			} else {
				router.push({ path: afterLoginPath });
			}
		} catch (err) {
			console.error(err);
			// user has to manually logout at this point
			// or this needs to be taken care of in "handleLoginCallbackAsync"
			router.push({ path: '/?logoutReason=tokenerror' });
		}
	}
});
</script>
