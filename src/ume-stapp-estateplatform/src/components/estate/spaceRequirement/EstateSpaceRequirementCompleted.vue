<template>
	<div class="estate-space-requirement-completed">
		<div
			class="success-wrap d-flex justify-center align-center flex-column pa-4 text-center"
		>
			<base-animated-success class="my-8" />
			<h1 class="">
				{{ $t('component.spaceRequirement.completed.title') }}
			</h1>
			<p class="my-2">
				{{
					$t(
						'component.spaceRequirement.completed.description'
					)
				}}
			</p>
			<v-btn
				class="mt-4"
				color="primary"
				:to="{
					name: EstateRoutes.Search,
				}"
			>
				{{ $t('component.spaceRequirement.completed.goHome') }}
			</v-btn>
		</div>
		<hr class="mt-6" />

		<!--
			Reuses the "estateOrder" feedback category on purpose: the backend
			ValidFeedbackCategories list is frozen and space requirement is an order
			variant, so a separate category would require a cross-repo backend change.
		-->
		<rate-feedback
			category="estateOrder"
			:feedback-title="
				$t(
					'component.spaceRequirement.completed.feedbackTitle'
				)
			"
			class="mt-6"
		/>
	</div>
</template>

<script setup lang="ts">
import { EstateRoutes } from '@/router/routes';
import BaseAnimatedSuccess from '@/components/shared/BaseAnimatedSuccess.vue';
import RateFeedback from '@/components/shared/RateFeedback.vue';
</script>

<style scoped lang="scss">
.estate-space-requirement-completed {
	hr {
		border: none;
		border-top: solid 1px #f2f2f2;
	}
	.success-wrap {
		a {
			color: #fff !important; // Override global link color
		}
	}
}
</style>
