<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/feedback/RateFeedback.vue -->
<template>
	<div class="mt-14 d-flex justify-center">
		<v-card class="rate-feedback text-center">
			<div class="icon-wrap d-flex justify-center" aria-hidden="true">
				<v-avatar color="white" variant="elevated">
					<v-icon icon="stars" color="primary" :size="30" />
				</v-avatar>
			</div>

			<div class="loading-indicator">
				<v-progress-linear
					v-if="isBusySendingRating"
					:height="2"
					color="secondary"
					indeterminate
					aria-busy="true"
				/>
			</div>
			<v-card-text>
				<h3>{{ feedbackTitle }}</h3>
				<p class="mt-3 text-subtitle-1">
					{{ feedbackSubtitle || t('component.feedback.subtitle') }}
				</p>
				<div class="mt-3 mb-3 d-flex justify-center">
					<rating-stars
						v-model="rating"
						:rating-label="feedbackTitle"
					/>
				</div>

				<div
					v-if="
						rating !== null &&
						hasSubmittedRating &&
						!hasSubmittedComment
					"
				>
					<p class="mt-3 mb-6 text-subtitle-1">
						{{ t('component.feedback.thanksForRating') }}
					</p>

					<form>
						<v-textarea
							v-model="comment"
							variant="outlined"
							:label="t('component.feedback.commentLabel')"
							class="mt-2"
							color="primary"
							auto-grow
							:rows="2"
							:maxlength="500"
						/>
						<div
							class="mt-4 d-flex justify-space-between flex-wrap"
						>
							<p class="comment-help-text">
								{{ t('component.feedback.commentHelpText') }}
							</p>
							<div class="text-right flex-fill pl-4">
								<v-btn
									rounded="lg"
									type="submit"
									flat
									color="primary"
									@click="submitComment"
									:loading="isBusySendingComment"
									:disabled="isBusySendingComment || !comment"
								>
									{{ t('component.feedback.sendComment') }}
								</v-btn>
							</div>
						</div>
					</form>
				</div>
				<div v-else-if="hasSubmittedComment">
					<p class="mt-4 mb-3 text-subtitle-1">
						{{ t('component.feedback.thanksForComment') }}
					</p>
				</div>
			</v-card-text>
		</v-card>
	</div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import RatingStars from './RatingStars.vue';
import { IRootState } from '@/models/Interfaces';
import { useStore } from 'vuex';
import { DispatchType } from '@/models/Enums';
import { IFeedback } from '@/models/feedback/Interfaces';
import ErrorService from '@/utils/ErrorService';

const props = defineProps<{
	feedbackTitle: string;
	feedbackSubtitle?: string;
	//This list must match the validated categories in the backend (ValidFeedbackCategories)
	category: 'estateFaultReport' | 'estateOrder';
	additionalInfo?: Record<string, unknown>;
}>();

const { t } = useI18n();
const store = useStore<IRootState>();

const givenFeedback = computed(() => {
	return store.state.feedback?.submittedFeedback.find(
		(fb) => fb.category === props.category
	);
});

const rating = ref<number | null>(givenFeedback.value?.rating ?? null);
const comment = ref<string>(givenFeedback.value?.comment ?? '');
const hasSubmittedRating = ref(givenFeedback.value ? true : false);
const hasSubmittedComment = ref(givenFeedback.value?.comment ? true : false);

const isBusySendingRating = ref(false);
const submitRating = async () => {
	if (rating.value === null) {
		return;
	}

	const feedback: IFeedback = {
		category: props.category,
		rating: rating.value,
		additionalInfo: props.additionalInfo,
	};

	isBusySendingRating.value = true;
	try {
		await store.dispatch(DispatchType.FeedbackRate, feedback);
		hasSubmittedRating.value = true;

		if (hasSubmittedComment.value) {
			comment.value = '';
			hasSubmittedComment.value = false;
		}
	} catch (err) {
		rating.value = null;
		ErrorService.onError({ err });
	} finally {
		isBusySendingRating.value = false;
	}
};

watch(
	() => rating.value,
	(newRating) => {
		if (newRating !== null) {
			submitRating();
		}
	}
);

const isBusySendingComment = ref(false);
const submitComment = async () => {
	if (!comment.value.trim() || rating.value === null) {
		return;
	}

	const feedback: IFeedback = {
		category: props.category,
		rating: rating.value,
		comment: comment.value.trim(),
		additionalInfo: props.additionalInfo,
	};

	isBusySendingComment.value = true;
	try {
		await store.dispatch(DispatchType.FeedbackComment, feedback);
		hasSubmittedComment.value = true;
	} finally {
		isBusySendingComment.value = false;
	}
};
</script>

<style scoped lang="scss">
.rate-feedback {
	max-width: 500px;
	width: 100%;
	overflow: visible;
	margin-top: 20px;

	.icon-wrap {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;

		.v-avatar {
			margin-top: -20px;
		}
	}

	.loading-indicator {
		height: 16px;
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		z-index: -1;
		border-radius: $border-radius;
		overflow: hidden;
	}

	.v-card-text {
		margin-top: 20px;
		h3 {
			font-size: size(19);
		}
		.comment-help-text {
			font-size: 0.85rem;
			color: $grey-darken-3;
		}
	}
}
</style>
