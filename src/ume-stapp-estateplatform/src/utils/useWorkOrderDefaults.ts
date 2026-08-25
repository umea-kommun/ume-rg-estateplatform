// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/src/utils/useWorkOrderDefaults.ts
import { onMounted, Ref } from 'vue';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { DispatchType } from '@/models/Enums';

interface WorkOrderDefaults {
	notifierPhone: string | null;
}

/**
 * Prefills the notifier phone field with the value from the user's most recent
 * work order submission. Only fills when the field is still empty, so it never
 * overrides a query-param or user-entered value. Fails silently.
 */
export function useWorkOrderDefaults(contactPhone: Ref<string>) {
	const store = useStore<IRootState>();

	onMounted(async () => {
		if (contactPhone.value) return;

		try {
			const defaults: WorkOrderDefaults = await store.dispatch(
				DispatchType.GetWorkOrderDefaults
			);

			if (defaults?.notifierPhone && !contactPhone.value) {
				contactPhone.value = defaults.notifierPhone;
			}
		} catch {
			// No prefill if the fetch fails
		}
	});
}
