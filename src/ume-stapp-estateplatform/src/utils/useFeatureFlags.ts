import { ref, readonly } from 'vue';
import Axios from 'axios';
import Config from '@/utils/Config';

const features = ref<Set<string>>(new Set());
const loaded = ref(false);
const loading = ref(false);
const failed = ref(false);

async function loadFeatures(): Promise<void> {
	if (loaded.value || loading.value) return;
	loading.value = true;

	try {
		const response = await Axios.get<string[]>(
			`${Config.VUE_APP_ESTATE_SERVICE}/features`
		);
		features.value = new Set(response.data.map((f: string) => f.toLowerCase()));
		failed.value = false;
	} catch {
		features.value = new Set();
		failed.value = true;
	} finally {
		loaded.value = true;
		loading.value = false;
	}
}

function isEnabled(feature: string): boolean {
	// Fail open: if the feature endpoint is unreachable, treat everything as enabled
	if (failed.value) return true;
	return features.value.has(feature.toLowerCase());
}

export function useFeatureFlags() {
	return {
		loaded: readonly(loaded),
		loadFeatures,
		isEnabled,
	};
}
