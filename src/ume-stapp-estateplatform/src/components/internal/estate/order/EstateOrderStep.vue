<template>
	<div class="estate-order-step">
		<div class="estate-order-step__header">
			<div class="estate-order-step__title">
				<h2 ref="title">
					<slot name="title">
						{{ title }}
					</slot>
				</h2>
				<v-btn
					v-if="showClear"
					@click="emit('clear')"
					rounded="xl"
					variant="tonal"
					size="small"
					color="grey-darken-2"
					class="regular-text ma-0 ml-2"
				>
					{{ $t('component.internal.faultReport.changeAnswer') }}
				</v-btn>
			</div>
			<div class="estate-order-step__step">
				<v-btn
					v-if="showSkip"
					@click="emit('skip')"
					rounded="xl"
					variant="tonal"
					color="grey-darken-2"
					class="regular-text"
				>
					{{ $t('component.internal.faultReport.room.skip') }}
				</v-btn>
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
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16px;
	}
	&__title {
		display: flex;
		align-items: center;
	}
	&__step {
		font-size: size(14);
		color: $grey-darken-1;
	}
}
</style>
