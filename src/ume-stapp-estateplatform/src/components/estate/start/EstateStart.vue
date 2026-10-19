<template>
	<app-content
		class="estate-start estate-default"
		:size="AppContentSize.Wide"
		:pageTitle="$t('component.appHeader.title.default')"
	>
		<estate-start-hero />

		<turkos-banners class="mt-4" />

		<turkos-release-banner
			class="mt-4"
			:to="{ name: EstateRoutes.ReleaseNotes }"
		/>

		<estate-start-actions />

		<estate-ongoing-errands />

		<section class="favorites">
			<favorite-list />
		</section>

		<rate-feedback
			category="estatePortal"
			:feedback-title="$t('component.estatePortal.feedbackTitle')"
			class="feedback"
		/>
	</app-content>
</template>

<script setup lang="ts">
import AppContent from '@/components/app/AppContent.vue';
import {
	TurkosBanners,
	TurkosReleaseBanner,
} from '@turkos/controlcenter-client';
import EstateStartHero from './EstateStartHero.vue';
import EstateStartActions from './EstateStartActions.vue';
import EstateOngoingErrands from './EstateOngoingErrands.vue';
import FavoriteList from '../favorite/FavoriteList.vue';
import RateFeedback from '@/components/shared/RateFeedback.vue';
import { AppContentSize } from '@/models/Enums';
import '@/themes/estate.scss';
import { EstateRoutes } from '@/router/routes';
</script>

<style lang="scss" scoped>
.estate-start {
	overflow-x: clip;

	// The hero is a full-bleed banner, so it sits flush under the header
	// instead of below the standard top padding.
	:deep(.v-container) {
		padding-top: 0;
	}

	.favorites {
		margin-top: 56px;

		:deep(.favorite-list > div > div:last-child) {
			display: grid;
			grid-template-columns: 1fr 1fr;
			gap: 16px;
			margin-top: 16px;
		}

		:deep(.estate-search-result-item) {
			height: 100%;
			margin-bottom: 0;
		}
	}

	.feedback {
		margin-top: 96px;
	}

	@media only screen and (max-width: $estate-mobile-threshold) {
		.favorites :deep(.favorite-list > div > div:last-child) {
			grid-template-columns: 1fr;
		}
	}
}
</style>
