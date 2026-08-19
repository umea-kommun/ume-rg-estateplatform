<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/base/BaseAutoLinkText.vue -->
<template>
	<template v-for="(t, i) in tokens" :key="i">
		<span v-if="t.type === 'text'">{{ t.value }}</span>
		<a
			v-else
			:href="t.href"
			:target="t.kind === 'url' ? '_blank' : undefined"
			:rel="t.kind === 'url' ? 'noopener noreferrer' : undefined"
			:aria-label="linkAriaLabel"
		>
			{{ t.value }}
		</a>
	</template>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { linkify } from '@/utils/linkifyText';

const props = defineProps<{
	text: string | null | undefined;
	linkAriaLabel?: string;
}>();

const tokens = computed(() => (props.text ? linkify(props.text) : []));
</script>
