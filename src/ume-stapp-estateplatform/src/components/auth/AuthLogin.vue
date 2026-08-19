<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/auth/AuthLogin.vue -->
<template>
	<app-content :size="contentSize" :isLoading="true" />
</template>
<script setup lang="ts">
import { ref, inject, computed, onMounted } from 'vue';
import { useStore } from 'vuex';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute, useRouter } from 'vue-router';
import IAuthManager from '@/plugins/auth/IAuthManager';
import { IRootState } from '@/models/Interfaces';
import { AppContentSize } from '@/models/Enums';

const store = useStore<IRootState>();
const route = useRoute();
const router = useRouter();
const $auth = inject('$auth') as IAuthManager;

const user = computed(() => store.state.user);
const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

/**
 * The portal is internal-only, so there is no login-method picker: this page
 * exists purely to bounce the user at IDProxy and come back to where they were.
 */
function login(): void {
	if (user.value.isAuthenticated) {
		return;
	}

	let comeBackUrl = '/';
	if (route.query.comeBack) {
		// TODO: comeBackUrl should be able to include route params, not only path
		const { name, path } = router.resolve({
			path: route.query.comeBack.toString(),
		});
		// Set the comeBackUrl if it is valid
		if (name && path && !path.startsWith('//')) {
			comeBackUrl = path;
		}
	}

	$auth.loginInternal(comeBackUrl);
}

onMounted(() => {
	login();
});
</script>
