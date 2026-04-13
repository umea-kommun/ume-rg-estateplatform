<template>
	<div class="estate-order-step">
		<div
			class="estate-order-step__header"
			:class="{ 'has-skip': showSkip }"
		>
			<div class="estate-order-step__title">
				<h2 ref="title">
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
					class="regular-text ma-0"
				>
					{{ $t('component.internal.faultReport.changeAnswer') }}
				</v-btn>
			</div>
			<div class="estate-order-step__step">
				<v-btn
					v-if="showSkip"
					@click="emit('skip')"
					rounded="lg"
					variant="tonal"
					color="grey-darken-2"
					class="regular-text ma-0"
				>
					{{ $t('component.internal.faultReport.room.skip') }}
				</v-btn>
				<v-spacer />
				{{ step }}/{{ stepCount }}
			</div>
		</div>

		<!-- Content -->
		<slot></slot>
	</div>
</template>

<script setup lang="ts">
import { useTemplateRef } from 'vue';

defineProps<{
	title?: string;
	showClear?: boolean;
	showSkip?: boolean;
	step: number;
	stepCount: number;
}>();

const emit = defineEmits(['clear', 'skip']);

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
}
</style>
