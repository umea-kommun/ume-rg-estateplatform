<template>
	<div class="estate-order-step">
		<div
			class="estate-order-step__header"
			:class="{ 'has-skip': showSkip }"
		>
			<div class="estate-order-step__title">
				<h2 ref="title" class="ma-0">
					<slot name="title">
						{{ title }}
					</slot>
				</h2>
				<slot name="header-btn"></slot>
				<v-btn
					v-if="showClear"
					@click="emit('clear')"
					rounded="lg"
					variant="tonal"
					size="small"
					color="grey-darken-2"
				>
					{{ $t('component.faultReport.changeAnswer') }}
				</v-btn>
			</div>
			<div v-if="showSkip || hasCounter" class="estate-order-step__step">
				<v-btn
					v-if="showSkip"
					@click="emit('skip')"
					rounded="lg"
					variant="tonal"
					color="grey-darken-2"
				>
					{{ $t('component.faultReport.room.skip') }}
				</v-btn>
				<v-spacer />
				<span v-if="hasCounter">{{ step }}/{{ stepCount }}</span>
			</div>
		</div>

		<!-- Content -->
		<slot></slot>
	</div>
</template>

<script setup lang="ts">
import { computed, useTemplateRef } from 'vue';

const props = defineProps<{
	title?: string;
	showClear?: boolean;
	showSkip?: boolean;
	// Optional: when both are provided a "step/stepCount" counter is shown (the
	// numbered fault-report / order flows). Omitted for flows where order carries
	// no information (space requirement), which use plain section headers instead.
	step?: number;
	stepCount?: number;
}>();

const emit = defineEmits(['clear', 'skip']);

const hasCounter = computed(
	() => props.step != null && props.stepCount != null
);

const titleRef = useTemplateRef('title');

defineExpose({
	title: titleRef,
});
</script>
<style lang="scss" scoped>
.estate-order-step {
	padding-bottom: 24px;
	border-bottom: solid 1px #f2f2f2;

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
	}
	&__step {
		flex: 0 1 auto;
		font-size: size(14);
		color: $grey-darken-1;
		display: flex;
		gap: 8px;

		align-items: center;
	}

	.v-btn--size-small {
		font-size: size(14);
	}
}
</style>
