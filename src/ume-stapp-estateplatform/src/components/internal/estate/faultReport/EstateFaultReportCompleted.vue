<template>
	<div class="estate-fault-report-completed">
		<div
			class="success-wrap d-flex justify-center align-center flex-column pa-4 text-center"
		>
			<base-animated-success class="my-8" />
			<h1 class="">
				{{ $t('component.internal.faultReport.completed.title') }}
			</h1>
			<p class="my-2">
				{{ $t('component.internal.faultReport.completed.description') }}
			</p>
			<v-btn
				class="mt-4"
				color="primary"
				:to="{
					name: MyPagesRoutes.InternalStart,
				}"
			>
				{{ $t('component.internal.faultReport.completed.goHome') }}
			</v-btn>
		</div>
		<hr class="mt-6" />

		<rate-feedback
			category="estateFaultReport"
			:feedback-title="
				$t('component.internal.faultReport.completed.feedbackTitle')
			"
			class="mt-6"
		/>
	</div>
</template>

<script setup lang="ts">
import { MyPagesRoutes } from '@/router/routes';
import BaseAnimatedSuccess from '@/components/base/BaseAnimatedSuccess.vue';
import RateFeedback from '@/components/feedback/RateFeedback.vue';
</script>

<style scoped lang="scss">
.estate-fault-report-completed {
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
