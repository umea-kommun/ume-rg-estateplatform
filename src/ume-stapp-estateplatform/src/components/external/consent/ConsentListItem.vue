<template>
	<v-card class="consent-list-item mb-4" :elevation="2">
		<div class="content flex-fill">
			<v-card-title>
				{{ consent.title }}
			</v-card-title>
			<v-card-text>
				<div>
					<v-chip
						class="consent-user-answer"
						:class="{
							approved:
								consent.userStatus ===
								UserConsentStatus.Approved,
							rejected:
								consent.userStatus ===
								UserConsentStatus.Rejected,
						}"
						variant="outlined"
					>
						{{ userStatusText }}
					</v-chip>
				</div>
				<div
					:title="
						$t('component.consentStart.consentForName', {
							name: consent.childName,
						})
					"
				>
					<v-icon icon="person" />{{ consent.childName }}
				</div>
				<div>
					<v-icon icon="info_outline" />
					{{ consentStatus }}
				</div>
			</v-card-text>
		</div>
		<div class="d-flex justify-end align-center pa-4 flex-1-1">
			<v-btn
				flat
				color="primary"
				class="ma-0 regular-text"
				:variant="
					userHasAnswered || !consent.isActive
						? 'outlined'
						: undefined
				"
				@click="$emit('open', consent)"
			>
				{{
					userHasAnswered || !consent.isActive
						? $t('component.consentStart.actionOpen')
						: $t('component.consentStart.actionAnswer')
				}}
			</v-btn>
		</div>
	</v-card>
</template>

<script setup lang="ts">
import { ConsentStatus, UserConsentStatus } from '@/models/Enums';
import { IChildConsent, IRootState } from '@/models/Interfaces';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';

const props = defineProps<{
	consent: IChildConsent;
}>();

defineEmits<{
	open: [consent: IChildConsent];
}>();

const { t } = useI18n();
const store = useStore<IRootState>();

const userIsConsentTarget = computed(() => {
	return props.consent.childSSNo === store.state.user?.socialSecurityNumber;
});

const userHasAnswered = computed(() => {
	return (
		props.consent.userStatus === UserConsentStatus.Approved ||
		props.consent.userStatus === UserConsentStatus.Rejected
	);
});

const userStatusText = computed(() => {
	if (props.consent.userStatus === UserConsentStatus.Approved) {
		return t('component.consentStart.userStatus.approved');
	} else if (props.consent.userStatus === UserConsentStatus.Rejected) {
		return t('component.consentStart.userStatus.rejected');
	} else {
		return t('component.consentStart.userStatus.unanswered');
	}
});

const consentStatus = computed(() => {
	if (props.consent.consentStatus === ConsentStatus.Approved) {
		if (userIsConsentTarget.value) {
			return t('component.consentStart.status.approvedAdult');
		} else {
			return t('component.consentStart.status.approved');
		}
	} else if (props.consent.consentStatus === ConsentStatus.Denied) {
		if (userIsConsentTarget.value) {
			return t('component.consentStart.status.rejectedAdult');
		} else {
			return t('component.consentStart.status.rejected');
		}
	} else if (props.consent.consentStatus === ConsentStatus.Pending) {
		if (props.consent.userStatus === UserConsentStatus.NotAnswered) {
			return t('component.consentStart.status.pendingYourAnswer');
		} else {
			return t('component.consentStart.status.pendingOther');
		}
	} else {
		return t('component.consentStart.status.unanswered');
	}
});
</script>
<style scoped lang="scss">
.consent-list-item {
	color: $black;
	display: flex;
	flex-wrap: wrap;

	.v-card-title {
		font-size: size(18);
		font-weight: bold;
		white-space: normal;
	}
	.content {
		max-width: 100%;
	}

	.v-card-text {
		display: flex;
		font-size: size(16);
		flex-wrap: wrap;
		gap: 16px;
		& > div {
			display: flex;
			align-items: center;
			gap: 8px;
		}

		@media only screen and (max-width: 700px) {
			padding: 8px 16px;
			& > div {
				width: 100%;
			}
		}
	}

	.consent-user-answer {
		height: auto;
		padding: 2px 12px;
		font-size: size(16);
		color: $black;
		border: solid 1px $grey-lighten-5;
		background-color: $grey-lighten-3;

		&.approved {
			color: $white;
			background-color: $primary;
			border-color: $primary;
		}
		&.rejected {
			color: $white;
			background-color: $error;
			border-color: $error;
		}
	}

	.v-btn {
		font-size: size(16);
		height: auto;
		padding: 12px 26px;
	}
}
</style>
