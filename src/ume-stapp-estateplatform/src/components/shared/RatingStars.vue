<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/feedback/RatingStars.vue -->
<template>
	<div class="rating-stars" role="radiogroup" :aria-label="ratingLabel">
		<div
			v-for="n in 5"
			:key="n"
			class="rating-star"
			:class="{ selected: (rating ?? 0) >= n }"
			role="radio"
			ref="ratingInput"
			:title="t('component.feedback.ratingStars.starLabel', { n })"
			@click="rating = n"
			@keydown="onStarKeyDown($event, n)"
			:aria-label="t('component.feedback.ratingStars.starLabel', { n })"
			:aria-checked="rating === n"
			:tabindex="rating === null && n === 1 ? 0 : rating === n ? 0 : -1"
		>
			<v-icon
				:icon="(rating ?? 0) >= n ? 'star' : 'star_border'"
				:size="30"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { useTemplateRefsList } from '@vueuse/core';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	ratingLabel: string;
	modelValue: number | null;
}>();

const emit = defineEmits(['update:modelValue']);
const { t } = useI18n();

const rating = computed({
	get: () => props.modelValue,
	set: (value: number | null) => emit('update:modelValue', value),
});

const ratingInput = useTemplateRefsList<HTMLInputElement>();

const selectPrevious = (currentElement: HTMLInputElement) => {
	const index = ratingInput.value.indexOf(currentElement);
	if (index > 0) {
		ratingInput.value[index - 1].focus();
	} else if (index === 0) {
		ratingInput.value[ratingInput.value.length - 1].focus();
	}
};
const selectNext = (currentElement: HTMLInputElement) => {
	const index = ratingInput.value.indexOf(currentElement);
	if (index !== -1 && index < ratingInput.value.length - 1) {
		ratingInput.value[index + 1].focus();
	} else if (index === ratingInput.value.length - 1) {
		ratingInput.value[0].focus();
	}
};

const onStarKeyDown = (event: KeyboardEvent, starNumber: number) => {
	let stopDefault = false;
	switch (event.key) {
		case ' ':
		case 'Enter':
			rating.value = starNumber;
			stopDefault = true;
			break;
		case 'Up':
		case 'ArrowUp':
		case 'Left':
		case 'ArrowLeft':
			selectPrevious(event.target as HTMLInputElement);
			stopDefault = true;
			break;

		case 'Down':
		case 'ArrowDown':
		case 'Right':
		case 'ArrowRight':
			selectNext(event.target as HTMLInputElement);
			stopDefault = true;
			break;
	}
	if (stopDefault) {
		event.stopPropagation();
		event.preventDefault();
	}
};
</script>

<style scoped lang="scss">
.rating-stars {
	display: flex;
	gap: 5px;

	.rating-star {
		transition: background-color 0.2s;
		cursor: pointer;
		border-radius: 50px;
		padding: 4px;
		color: $grey-darken-2;

		&.selected {
			color: $secondary;
		}

		&:active {
			background-color: $grey-lighten-4;
		}
		&:focus-visible {
			outline: 2px solid $grey-darken-1;
			background-color: $grey-lighten-4;
		}
	}
}
</style>
