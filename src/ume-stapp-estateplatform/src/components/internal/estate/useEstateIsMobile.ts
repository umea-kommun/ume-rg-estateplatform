import { computed } from 'vue';
import { useMediaQuery } from '@vueuse/core';

export function useEstateIsMobile() {
	const threshold = computed(
		() =>
			getComputedStyle(document.documentElement)
				.getPropertyValue('--estate-mobile-threshold')
				.trim() || '900px'
	);
	return useMediaQuery(computed(() => `(max-width: ${threshold.value})`));
}
