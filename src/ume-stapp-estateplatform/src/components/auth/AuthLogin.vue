<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="app-start"
		:pageTitle="$t('component.BaseLoginMethods.title')"
	>
		<!-- Login to start -->
		<base-login-methods
			:clientState="clientState"
			:clientNames="clientNames"
			:cancelUrl="cancelUrl"
			:allClientsConfig="allClientsConfig"
		>
			<template v-slot:sideInfo>
				<v-card flat class="side-info mb-6">
					<h2 class="mb-3">{{ $t('app.auth.loginInfo.title') }}</h2>
					<p>
						{{ $t('app.auth.loginInfo.text') }}
					</p>
					<h3 class="mt-4 mb-1">
						{{
							$t('app.auth.loginInfo.externalFunctionalityTitle')
						}}
					</h3>
					<ul>
						<li class="ml-6">
							{{
								$t('app.auth.loginInfo.externalFunctionality1')
							}}
						</li>
					</ul>
				</v-card>
			</template>
		</base-login-methods>
	</app-content>
</template>
<script setup lang="ts">
import { ref, inject, computed, onMounted } from 'vue';
import { useStore } from 'vuex';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute, useRouter } from 'vue-router';
import IAuthManager from '@/plugins/auth/IAuthManager';
import { IRootState } from '@/models/Interfaces';
import { AppContentSize } from '@/models/Enums';
import IAuthClientConfig from '@/plugins/auth/IAuthClientConfig';

const store = useStore<IRootState>();
const route = useRoute();
const router = useRouter();
const isBusyLoadingFromServer = ref<boolean>(false);
const $auth = inject('$auth') as IAuthManager;

const user = computed(() => store.state.user);
const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const doLogin = ref(false);
const clientState = ref('');
const clientNames = ref('');
const cancelUrl = ref('');
const allClientsConfig = ref<IAuthClientConfig[]>([]);

function login(): void {
	if (!user.value.isAuthenticated) {
		isBusyLoadingFromServer.value = true;

		let comeBackUrl = '/';
		if (route.query.comeBack) {
			const { name, path } = router.resolve({
				path: route.query.comeBack.toString(),
			});
			// Set the comeBackUrl if it is valid
			if (name && path) {
				comeBackUrl = path;
			}
		}

		const tempLoginUrl =
			$auth.loginCitizen(comeBackUrl, ['AllLoginMethods']) ?? '';
		clientState.value = tempLoginUrl.split('&state=')[1].split('&')[0];
		clientNames.value = tempLoginUrl
			.split('&client_name=')[1]
			.split('&')[0];
		cancelUrl.value = tempLoginUrl
			.split('&frejaCancelUrl=')[1]
			.split('&')[0];
		allClientsConfig.value = $auth.getAllClientsConfig();

		doLogin.value = true;
		isBusyLoadingFromServer.value = false;
	}
}

onMounted(() => {
	login();
});
</script>
