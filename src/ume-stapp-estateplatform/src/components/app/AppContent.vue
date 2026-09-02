<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/app/AppContent.vue -->
<template>
	<div class="app-content" :class="props.size">
		<div class="alert-wrap" v-if="showWarningMessage">
			<v-alert
				v-model="showWarningMessage"
				icon="warning"
				color="error"
				class="ma-0"
				closable
			>
				{{ warningMessage }}
			</v-alert>
		</div>
		<v-container :class="{ 'with-breadcrumb': $slots.breadcrumbs }">
			<div class="breadcrumbs-wrap" v-if="!isLoading">
				<slot name="breadcrumbs"></slot>
			</div>
			<!-- Spinner while loading content  -->
			<app-loading-spinner :isVisible="isLoading"></app-loading-spinner>
			<!-- Render slot content -->
			<template v-if="!isLoading">
				<slot></slot>
			</template>
		</v-container>
	</div>
</template>

<script setup lang="ts">
import { computed, PropType } from 'vue';
import { AppContentSize, MutationType } from '@/models/Enums';
import { useI18n } from 'vue-i18n';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { useTitle } from '@vueuse/core';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import Config from '@/Config';
import { useEnvironmentBadge } from '@/utils/useEnvironmentBadge';

const props = defineProps({
	size: {
		type: String as PropType<AppContentSize>,
		default: AppContentSize.Default,
	},
	breadcrumb: {
		type: Boolean,
		default: true,
	},
	isLoading: {
		type: Boolean,
		default: false,
	},
	errorMessage: {
		type: String,
		default: '',
	},
	pageTitle: {
		type: String,
		default: '',
	},
});

const { t } = useI18n();
const store = useStore<IRootState>();
const { environmentPrefix } = useEnvironmentBadge();

/**
 * Get full page title using value from property pageTitle
 */
const fullPageTitle = computed(() => {
	if (!props.pageTitle) {
		return (
			environmentPrefix.value +
			t('app.title').toString() +
			' - ' +
			t('app.title').toString()
		);
	}
	return (
		environmentPrefix.value +
		props.pageTitle +
		' - ' +
		t('app.title').toString()
	);
});
// Automatically updates page title when fullPageTitle changes
useTitle(fullPageTitle);

// Warning message
const warningMessage = computed(() => {
	if (
		store.state.user.authClientName ===
			Config.VUE_APP_AUTH_INTERNAL_CLIENT_NAME &&
		!store.state.user.socialSecurityNumber
	) {
		return t('component.appContent.warning.adUserMissingSSN');
	}
	return '';
});
const showWarningMessage = computed({
	get: () =>
		!!warningMessage.value && store.state.hideWarningMessage !== false,
	set: (show: boolean) => {
		store.commit(MutationType.HideWarningMessage, show);
	},
});
</script>

<style scoped lang="scss">
#main-content {
	background: none;
}
.app-content {
	.v-container,
	.alert-wrap {
		padding: $site-content-vertical-padding $site-horizontal-padding;
		max-width: $site-max-width;
	}
	.alert-wrap {
		margin: 0 auto;
		padding-top: 12px;
		padding-bottom: 0;
		.v-alert {
			background-color: $error !important;
			border-radius: $border-radius;
			padding: 10px;
			align-items: center;
			:deep(.v-alert__prepend) {
				align-self: center;
				margin-left: 6px;
			}
		}
		color: #fff;
	}

	&.Size-Narrow {
		.v-container {
			max-width: $site-max-width-narrow;
		}
	}
	&.Size-FullWidth {
		.v-container {
			max-width: none;

			padding: 0;
		}
	}

	.v-container.with-breadcrumb {
		padding-top: calc($site-content-vertical-padding - 24px);
		.breadcrumbs-wrap {
			margin-bottom: 8px;
		}
	}
}
</style>
