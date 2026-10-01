<template>
	<div
		ref="root"
		class="estate-order-step"
		:class="{
			'my-8': !rail,
			'has-rail': rail,
			'is-last': rail && isLast,
			'is-upcoming': isUpcoming,
			'is-completed': isCompleted,
			'is-skipped': isSkipped,
			'is-current': isCurrent && hasCounter,
		}"
	>
		<!-- Vertical progress rail (opt-in, numbered flows only): a marker beside
		each step title with a connecting line that turns green once the step is
		completed or skipped. Purely decorative - the step counter and titles carry
		the accessible state - so it is hidden from assistive tech and on mobile. -->
		<div v-if="rail" class="estate-order-step__rail" aria-hidden="true">
			<span class="estate-order-step__marker">
				<!-- The check/dash pops in when the step resolves; it's the small
				reward for completing a step. Enter-only - clearing a step just
				removes it. Reduced-motion neutralises the transition below. -->
				<transition name="estate-step-marker">
					<v-icon
						v-if="isCompleted || isSkipped"
						:icon="isCompleted ? 'check' : 'remove'"
						:size="14"
					/>
				</transition>
			</span>
			<span v-if="!isLast" class="estate-order-step__line" />
		</div>

		<div class="estate-order-step__body">
			<div
				class="estate-order-step__header"
				:class="{ 'has-skip': showSkip && !isUpcoming }"
			>
				<div class="estate-order-step__title">
					<h2 ref="title" class="ma-0">
						<slot name="title">
							{{ title }}
						</slot>
					</h2>
					<template v-if="!isUpcoming">
						<slot name="header-btn"></slot>
						<v-btn
							v-if="showClear"
							@click="emit('clear')"
							rounded="lg"
							variant="outlined"
							color="grey-darken-2"
						>
							{{ $t('component.faultReport.changeAnswer') }}
						</v-btn>
					</template>
				</div>
				<div
					v-if="(showSkip && !isUpcoming) || hasCounter"
					class="estate-order-step__step"
				>
					<v-btn
						v-if="showSkip && !isUpcoming"
						@click="emit('skip')"
						rounded="lg"
						variant="outlined"
						color="grey-darken-2"
					>
						{{ $t('component.faultReport.room.skip') }}
					</v-btn>
					<v-spacer />
					<span v-if="hasCounter">{{
						$t('component.faultReport.stepCounter', {
							step,
							stepCount,
						})
					}}</span>
				</div>
			</div>

			<!-- Content: hidden for upcoming steps so the header reads as a preview.
			Wrapped so its height changes (collapsing to a summary, revealing the
			next input) tween smoothly and neighbouring steps flow up/down instead
			of jumping. -->
			<auto-height>
				<slot v-if="!isUpcoming"></slot>
			</auto-height>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, useTemplateRef } from 'vue';
import AutoHeight from '@/components/shared/AutoHeight.vue';

const props = withDefaults(
	defineProps<{
		title?: string;
		showClear?: boolean;
		showSkip?: boolean;
		// Optional: when both are provided a "step/stepCount" counter is shown (the
		// numbered fault-report / order flows). Omitted for flows where order carries
		// no information (space requirement), which use plain section headers instead.
		step?: number;
		stepCount?: number;
		// Step lifecycle for the numbered flows: 'completed' collapses to the
		// selected value, 'current' is the active step, 'skipped' is a resolved
		// step the user passed without choosing (optional/not applicable), and
		// 'upcoming' is greyed and hides its content. Defaults to 'current' so
		// untouched callers (space requirement) render exactly as before.
		state?: 'completed' | 'current' | 'skipped' | 'upcoming';
		// Opt-in vertical progress rail down the left of the steps. Only the
		// state-driven fault-report flow uses it; other callers (order, space
		// requirement) render exactly as before.
		rail?: boolean;
	}>(),
	{
		state: 'current',
	}
);

const emit = defineEmits(['clear', 'skip']);

const hasCounter = computed(
	() => props.step != null && props.stepCount != null
);

const isCompleted = computed(() => props.state === 'completed');
const isCurrent = computed(() => props.state === 'current');
const isUpcoming = computed(() => props.state === 'upcoming');
const isSkipped = computed(() => props.state === 'skipped');

// The last step ends the rail, so it draws a marker but no trailing line.
const isLast = computed(
	() => hasCounter.value && props.step === props.stepCount
);

const titleRef = useTemplateRef('title');
const rootRef = useTemplateRef('root');

defineExpose({
	title: titleRef,
	root: rootRef,
});
</script>
<style lang="scss" scoped>
.estate-order-step {
	&__header {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		gap: 1rem;
		&.has-skip {
			@media (max-width: 650px) {
				grid-template-columns: 1fr;
			}
		}
	}

	&__title {
		display: flex;
		align-items: center;

		flex: 1 1 20rem;
		min-width: 0;
		flex-wrap: wrap;
		gap: 12px;

		color: $grey-darken-3;

		h2 {
			font-size: 1.25rem;
			line-height: 1.4;
			display: flex;
			align-items: center;
			gap: 6px;
			// Ease the "you are here" accent in/out instead of snapping as the
			// step becomes current or completes.
			transition: color 0.15s ease;
		}
	}
	&__status {
		flex: 0 0 auto;
	}
	&__step {
		flex: 0 1 auto;
		font-size: size(14);
		color: $grey-darken-1;
		display: flex;
		gap: 8px;

		align-items: center;
		transition: color 0.15s ease;
	}
	.v-btn--size-small {
		font-size: size(14);
	}

	// Active step gets a subtle accent so it reads as "you are here".
	&.is-current {
		.estate-order-step__title h2 {
			color: $primary;
		}
		.estate-order-step__step {
			color: $primary;
		}
	}

	// Upcoming steps are visible but clearly not yet reachable.
	&.is-upcoming {
		.estate-order-step__header {
			background-color: $grey-lighten-3;
			padding: 14px;
			border-radius: $border-radius;
		}
		.estate-order-step__title h2 {
			font-weight: 500;
			color: $grey-darken-2;
		}
	}

	// --- Vertical progress rail -------------------------------------------------
	&.has-rail {
		display: flex;
		align-items: stretch;
		gap: 16px;
	}

	&__rail {
		flex: 0 0 20px;
		display: flex;
		flex-direction: column;
		align-items: center;
	}

	&__marker {
		flex: 0 0 auto;
		// Fill the dot green as the step completes, in step with the check pop.
		transition:
			background-color 0.2s ease,
			border-color 0.2s ease;
		// Nudge down so the marker centres on the step title's first line.
		margin-top: 4px;
		width: 18px;
		height: 18px;
		box-sizing: border-box;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		color: #fff;
		// Default = upcoming: a hollow grey dot not yet reached.
		background: $grey-lighten-2;
		border: 2px solid $grey-lighten-4;
	}

	&__line {
		flex: 1 1 auto;
		width: 2px;
		// 6px gap below the circle; end 2px short of the step boundary so the next
		// circle (4px into its step) gets a matching 6px gap above it.
		margin-top: 6px;
		margin-bottom: 2px;
		border-radius: 1px;
		// Grey by default; quietly crossfades to green once the step behind it is
		// completed/skipped (see below). A plain colour fade rather than a
		// directional wipe - the marker carries the reward beat, the line just
		// settles to "behind you" without pulling the eye to the margin.
		background: $grey-lighten-4;
		transition: background-color 0.2s ease;
	}

	&__body {
		flex: 1 1 auto;
		min-width: 0;
	}
	&.has-rail &__body {
		// Spacing between steps lives here so the rail line can stretch across it.
		padding-bottom: 2rem;
	}
	&.has-rail.is-last &__body {
		padding-bottom: 0;
	}

	// Completed: filled green marker with a check; the line below it fades green.
	&.has-rail.is-completed {
		.estate-order-step__marker {
			background: $primary;
			border-color: $primary;
		}
		.estate-order-step__line {
			background: $primary;
		}
	}

	// Skipped: passed without a real choice - grey check, but the line still fades
	// green because the step is behind us.
	&.has-rail.is-skipped {
		.estate-order-step__marker {
			background: $primary;
			border-color: $primary;
		}
		.estate-order-step__line {
			background: $primary;
		}
	}

	// Current: green ring with a solid dot - active, not yet done.
	&.has-rail.is-current {
		.estate-order-step__marker {
			background: #fff;
			border-color: $primary;
			// box-shadow: 0 0 0 4px rgba($primary, 0.12);
		}
	}

	// Upcoming headers are wrapped in a padded grey preview box, which pushes the
	// title down - offset the marker by that padding (4px base + 14px box padding)
	// so it stays aligned with the title.
	&.has-rail.is-upcoming {
		.estate-order-step__marker {
			position: relative;
			margin-top: 18px;

			// The 18px offset would leave a gap above the marker where the incoming
			// line ends; bridge it with a grey connector
			&::before {
				content: '';
				position: absolute;
				bottom: 24px;
				left: 50%;
				transform: translateX(-50%);
				width: 2px;
				height: 15px;
				background: $grey-lighten-4;
			}
		}
	}

	// The rail is decorative reinforcement; drop it on mobile and fall back to the
	// original stacked spacing.
	@media only screen and (max-width: $estate-mobile-threshold) {
		&.has-rail {
			display: block;
			margin: 2rem 0;
			&.mt-0 {
				margin-top: 0 !important;
			}
		}
		&__rail {
			display: none;
		}
		&.has-rail &__body {
			padding-bottom: 0;
		}
	}

	// Check/dash pop-in for the rail marker. Enter-only: it scales up from the
	// centre as the step resolves; removal is instant.
	.estate-step-marker-enter-active {
		transition:
			transform 0.15s ease-out,
			opacity 0.15s ease-out;
	}
	.estate-step-marker-enter-from {
		transform: scale(0.4);
		opacity: 0;
	}

	// Motion here is confirmation, not content - drop it entirely for users who
	// ask for reduced motion. End states (green fill, visible marker) still apply.
	@media (prefers-reduced-motion: reduce) {
		&__title h2,
		&__step,
		&__marker,
		&__line,
		.estate-step-marker-enter-active {
			transition: none;
		}
	}
}
</style>
