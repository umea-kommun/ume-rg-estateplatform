import axios from 'axios';
import { onBeforeUnmount, ref, watch } from 'vue';
import Config from '@/Config';
import store from '@/store/store';
import { createHttpClient } from '@/utils/httpClient';

export interface WorkOrderListItem {
	id: string;
	workOrderType: string | null;
	workOrderNumber: string | null;
	buildingId: number | null;
	buildingName: string | null;
	/** Null when the building is unknown; fall back to `buildingName`. */
	buildingPopularName: string | null;
	/** Only a presence flag: null when the building has no images. */
	buildingImageUrl: string | null;
	roomName: string | null;
	/** `Indoor`, `Outdoor` or null. */
	location: string | null;
	description: string;
	syncStatus: string;
	status: string | null;
	statusCategory: string | null;
	displayStatus: WorkOrderDisplayStatus | null;
	statusCheckedAt: string | null;
	performedDescription: string | null;
	performedDescriptionAt: string | null;
	createdAt: string;
	submittedAt: string | null;
	lastChangedAt: string;
}

// The API serializes enums camelCase (Program.cs), so these mirror the C# members in camelCase.

/** Mirrors WorkOrderDisplayStatus. The card shows `sent` while this is null. */
export type WorkOrderDisplayStatus =
	| 'received'
	| 'inProgress'
	| 'closed'
	| 'onHold'
	| 'materialOrdered'
	| 'sentToContractor';

/** Mirrors WorkOrderRefreshOutcome. */
export type WorkOrderRefreshOutcome =
	| 'refreshed'
	| 'notDue'
	| 'failed'
	| 'disabled';

interface RefreshResult {
	workOrders: WorkOrderListItem[];
	outcome: WorkOrderRefreshOutcome;
}

const httpClient = createHttpClient({ baseURL: Config.VUE_APP_ESTATE_SERVICE });

/** How long the list waits for the refresh before showing the saved orders. */
const RefreshWaitMs = 500;

/** Loads saved orders and refreshes them, waiting briefly so the list rarely reorders in view. */
export function useWorkOrders() {
	const orders = ref<WorkOrderListItem[]>([]);
	const loading = ref(false);
	const refreshing = ref(false);
	const loadFailed = ref(false);
	const outcome = ref<WorkOrderRefreshOutcome | ''>('');
	let controller = new AbortController();

	function requestConfig() {
		return {
			headers: { Authorization: 'Bearer ' + store.state.user.token },
			signal: controller.signal,
		};
	}

	// Aborted requests reject, so a late response never lands after logout.
	// NotDue is not shown: the saved statuses are simply still current.
	// Resolves true when the orders were replaced.
	async function refresh() {
		if (refreshing.value || !store.state.user.token) return false;
		refreshing.value = true;
		outcome.value = '';
		try {
			const { data } = await httpClient.post<RefreshResult>(
				'/workorders/sync',
				null,
				requestConfig()
			);
			orders.value = data.workOrders;
			outcome.value = data.outcome === 'notDue' ? '' : data.outcome;
			return true;
		} catch (error) {
			if (!axios.isCancel(error)) outcome.value = 'failed';
			return false;
		} finally {
			refreshing.value = false;
		}
	}

	// A quick refresh is shown directly; a slow or failed one shows the saved orders
	// first, and a late refresh then replaces them.
	async function load() {
		if (loading.value || !store.state.user.token) return;
		const { signal } = controller;
		loading.value = true;
		loadFailed.value = false;
		try {
			const { data } = await httpClient.get<WorkOrderListItem[]>(
				'/workorders',
				requestConfig()
			);
			const refreshed = data.length ? refresh() : Promise.resolve(false);
			const replaced = await Promise.race([
				refreshed,
				new Promise<false>((resolve) =>
					setTimeout(() => resolve(false), RefreshWaitMs)
				),
			]);
			// The user changed while waiting: the watcher has already reset the state.
			if (signal.aborted) return;
			if (!replaced) orders.value = data;
			loading.value = false;
			await refreshed;
		} catch (error) {
			loading.value = false;
			if (!axios.isCancel(error)) loadFailed.value = true;
		}
	}

	watch(
		() => store.state.user.token,
		() => {
			controller.abort();
			controller = new AbortController();
			orders.value = [];
			loading.value = false;
			refreshing.value = false;
			loadFailed.value = false;
			outcome.value = '';
			void load();
		},
		{ immediate: true }
	);
	onBeforeUnmount(() => controller.abort());

	return { orders, loading, refreshing, loadFailed, outcome, load, refresh };
}
