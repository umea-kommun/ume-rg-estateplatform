import { ref, readonly, computed } from 'vue';
import { createHttpClient } from '@/utils/httpClient';
import Config from '@/utils/Config';
import { EstateOrderCategory } from '@/models/Enums';
import store from '@/store/store';

/**
 * The signed-in user as the API sees them (GET /me): identity plus what they are
 * allowed to do, so nothing here has to decode the token.
 *
 * Separate from useFeatureFlags on purpose: feature flags are environment-wide and
 * fail open (a blip should not hide a shipped feature), permissions are per-user and
 * fail closed (a blip should not walk the user into a form the API will reject).
 * The API stays the enforcement point either way - this only gates what we show.
 */
interface CurrentUserResponse {
	email: string | null;
	fullName: string | null;
	permissions: {
		workOrderTypes: string[];
	};
}

const httpClient = createHttpClient();

const email = ref<string | null>(null);
const fullName = ref<string | null>(null);
const allowedWorkOrderTypes = ref<Set<string>>(new Set());
const loaded = ref(false);
const loading = ref(false);

function reset(): void {
	email.value = null;
	fullName.value = null;
	allowedWorkOrderTypes.value = new Set();
}

/**
 * Loads once per session. A failed attempt is not remembered as loaded, so the
 * next navigation retries it - fail closed must not mean "hidden for the rest of
 * the session because one request lost the network".
 *
 * /me needs a token, so an unauthenticated caller is a no-op rather than a 401
 * that would leave the user without permissions right after signing in. The token
 * goes on the request the same way store/actions.ts does it - createHttpClient
 * adds App Insights headers only, no auth.
 */
async function loadCurrentUser(): Promise<void> {
	if (loaded.value || loading.value) return;
	if (!store.state.user?.isAuthenticated) return;

	loading.value = true;

	try {
		const response = await httpClient.get<CurrentUserResponse>(
			`${Config.VUE_APP_ESTATE_SERVICE}/me`,
			{
				headers: {
					Authorization: 'Bearer ' + store.state.user.token,
				},
			}
		);
		email.value = response.data.email ?? null;
		fullName.value = response.data.fullName ?? null;
		allowedWorkOrderTypes.value = new Set(
			(response.data.permissions?.workOrderTypes ?? []).map((t) =>
				t.toLowerCase()
			)
		);
		loaded.value = true;
	} catch {
		// Fail closed for this attempt, but stay retryable.
		reset();
	} finally {
		loading.value = false;
	}
}

function canCreateWorkOrderType(type: EstateOrderCategory | string): boolean {
	return allowedWorkOrderTypes.value.has(type.toLowerCase());
}

export function useCurrentUser() {
	return {
		loaded: readonly(loaded),
		email: computed(() => email.value),
		fullName: computed(() => fullName.value),
		loadCurrentUser,
		canCreateWorkOrderType,
	};
}
