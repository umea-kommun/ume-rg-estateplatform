import { ref, shallowRef } from 'vue';
import store from '@/store/store';
import { DispatchType, EstateType } from '@/models/Enums';
import { IEstateSearchResultEntry } from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';

// Shared across every star button and favorite list in the app, so toggling a
// favorite anywhere (map info card, search hit, building page) is reflected
// everywhere without a remount.
const entries = shallowRef<IEstateSearchResultEntry[]>([]);
// Explicit per-node state. Populated from the server on load, and written
// directly on toggle so buttons update before the request resolves.
const states = ref(new Map<string, boolean>());
const isLoaded = ref(false);
const isFetching = ref(false);
const failedToFetch = ref(false);

let inFlight: Promise<void> | null = null;

const stateKey = (type: EstateType, id: number) => `${type}:${id}`;

const fetchFavorites = (): Promise<void> => {
	if (inFlight) {
		return inFlight;
	}

	isFetching.value = true;
	inFlight = (async () => {
		try {
			const result: IEstateSearchResultEntry[] = await store.dispatch(
				DispatchType.GetFavorites
			);
			entries.value = result ?? [];
			// Rebuilt from scratch: anything missing is no longer a favorite.
			states.value = new Map(
				entries.value.map((entry) => [
					stateKey(entry.type, entry.id),
					true,
				])
			);
			isLoaded.value = true;
			failedToFetch.value = false;
		} catch (err) {
			failedToFetch.value = true;
			ErrorService.onError({
				err,
				hidden: true,
			});
		} finally {
			isFetching.value = false;
			inFlight = null;
		}
	})();

	return inFlight;
};

const setState = (type: EstateType, id: number, value: boolean) => {
	const next = new Map(states.value);
	next.set(stateKey(type, id), value);
	states.value = next;
};

/**
 * The favorite state of a single node. Falls back to the value the server sent
 * with the node itself (search hits, building details) until the favorite list
 * has been loaded or the node has been toggled.
 */
const isFavorite = (
	type: EstateType,
	id: number,
	fallback = false
): boolean => {
	const state = states.value.get(stateKey(type, id));
	if (state !== undefined) {
		return state;
	}
	return isLoaded.value ? false : fallback;
};

/**
 * Adds or removes a favorite. The local state is updated first and rolled back
 * if the request fails, so the star responds immediately.
 */
const setFavorite = async (
	type: EstateType,
	id: number,
	value: boolean
): Promise<void> => {
	const previous = isFavorite(type, id);
	setState(type, id, value);

	try {
		await store.dispatch(
			value ? DispatchType.SetFavorite : DispatchType.UnsetFavorite,
			{ id, type }
		);
	} catch (err) {
		setState(type, id, previous);
		throw err;
	}

	// The list needs the full entry (name, image, ancestors), which only the
	// favorites endpoint returns. Refreshed in the background — the state above
	// already carries the change.
	if (isLoaded.value) {
		fetchFavorites();
	}
};

export function useFavorites() {
	return {
		entries,
		isLoaded,
		isFetching,
		failedToFetch,
		fetchFavorites,
		isFavorite,
		setFavorite,
	};
}
