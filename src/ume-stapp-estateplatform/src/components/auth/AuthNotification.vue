<!-- Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/components/auth/AuthNotification.vue @ 84b4a5dc -->
<template>
	<div class="auth-notification">
		<!-- Show snackbar if user have been idle for too long -->
		<v-snackbar
			v-model="isCountingDown"
			location="top"
			:timeout="-1"
			color="info"
			multi-line
			role="alert"
		>
			{{ $t('app.auth.loggingOut', { time: countDownText }) }}
		</v-snackbar>

		<!-- Tell the user that we have logged out -->
		<v-snackbar
			:model-value="showLoggedOutMessage"
			location="top"
			color="info"
			multi-line
			role="alert"
			:timeout="getSnackbarTimeout()"
		>
			{{ loggedOutMessage }}
			<template v-slot:actions>
				<v-btn variant="text" @click="showLoggedOutMessage = false">
					{{ $t('app.nav.close') }}
				</v-btn>
			</template>
		</v-snackbar>
	</div>
</template>
<script setup lang="ts">
import { computed, inject, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import { useIdle } from '@vueuse/core';
import Config from '@/Config';
import { IRootState } from '@/models/Interfaces';
import IAuthManager from '@/plugins/auth/IAuthManager';
import { useRoute } from 'vue-router';

const inactivityTimeLimit =
	(Config.VUE_APP_INACTIVITY_TIME_LIMIT as number) || 60;

const route = useRoute();
const store = useStore<IRootState>();
const $auth = inject('$auth') as IAuthManager;

const logoutTimeLimit: number = 5;
const countDownTimer = ref();
const countDownText = ref<string>('');
const isCountingDown = ref<boolean>(false);
const user = computed(() => store.state.user);
const { t } = useI18n();

const { idle } = useIdle(inactivityTimeLimit * 60 * 1000);

function getHumanReadableTimeLeft(secondsTotal: number): string {
	const minutes: number = Math.ceil(secondsTotal / 60);
	return minutes.toString() + ' ' + t('app.time.minute', minutes);
}

watch(idle, (isIdle) => {
	if (isIdle && user.value.isAuthenticated) {
		isCountingDown.value = true;
		let secondsLeftUntilLogout = logoutTimeLimit * 60;
		secondsLeftUntilLogout--;
		countDownText.value = getHumanReadableTimeLeft(secondsLeftUntilLogout);
		countDownTimer.value = setInterval(() => {
			secondsLeftUntilLogout--;
			if (secondsLeftUntilLogout < 1) {
				clearInterval(countDownTimer.value);
				$auth.logoutRedirectingToStartPage(
					user.value.authClientName,
					'logoutReason=idle'
				);
			} else {
				countDownText.value = getHumanReadableTimeLeft(
					secondsLeftUntilLogout
				);
			}
		}, 1000);
	} else if (countDownTimer.value) {
		isCountingDown.value = false;
		clearInterval(countDownTimer.value);
	}
});

const loggedOutMessage = computed(() => {
	switch (route.query.logoutReason) {
		case 'manual':
			return t('app.auth.loggedOutManual');
		case 'tokenerror':
			return t('app.auth.loggedOutTokenError');
		case 'idle':
			return t('app.auth.loggedOutIdleText');
		case 'sessionExpired':
			return t('app.auth.loggedOutExpiredSessionText');
		default:
			return '';
	}
});
const showLoggedOutMessage = ref(false);
watch(loggedOutMessage, () => {
	showLoggedOutMessage.value = !!loggedOutMessage.value;
});
function getSnackbarTimeout(): number {
	return parseInt(Config.VUE_APP_SNACKBAR_TIMEOUT, 10);
}
</script>
