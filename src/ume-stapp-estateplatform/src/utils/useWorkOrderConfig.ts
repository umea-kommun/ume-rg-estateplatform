// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/src/utils/useWorkOrderConfig.ts
import { computed, onMounted, ref } from 'vue';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { DispatchType } from '@/models/Enums';

interface WorkOrderFileConfig {
	maxFileCount: number;
	maxFileSizeBytes: number;
	allowedContentTypes: string[];
}

/**
 * Fetches work order file upload config from the backend on mount.
 * Falls back to sensible defaults if the fetch fails.
 */
export function useWorkOrderConfig() {
	const store = useStore<IRootState>();
	const config = ref<WorkOrderFileConfig | null>(null);

	const maxFiles = computed(() => config.value?.maxFileCount ?? 10);

	const maxSizeMb = computed(() =>
		config.value
			? Math.floor(config.value.maxFileSizeBytes / (1024 * 1024))
			: 10
	);

	const accept = computed(() => {
		if (!config.value) return '.pdf,image/*';
		return config.value.allowedContentTypes
			.map((t) => (t.includes('/') ? t : `.${t}`))
			.join(',');
	});

	onMounted(async () => {
		try {
			config.value = await store.dispatch(
				DispatchType.GetWorkOrderConfig
			);
		} catch {
			// Use defaults if config fetch fails
		}
	});

	return { maxFiles, maxSizeMb, accept };
}
