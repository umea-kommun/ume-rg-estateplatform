<template>
	<img
		v-bind="$attrs"
		class="building-image"
		:src="src"
		:alt="props.alt ?? $t('component.buildingDetails.buildingImage')"
		@load="loaded = true"
		v-show="loaded"
	/>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

const props = defineProps<{
	src: string;
	alt?: string;
	imageWidth?: number;
}>();

const src = computed(() => `${props.src}?w=${props.imageWidth ?? 300}`);

const loaded = ref(false);
</script>
