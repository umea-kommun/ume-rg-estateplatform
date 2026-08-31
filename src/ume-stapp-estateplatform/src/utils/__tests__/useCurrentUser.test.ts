import { beforeEach, describe, expect, test, vi } from 'vitest';
import { EstateOrderCategory } from '@/models/Enums';

const get = vi.fn();

vi.mock('@/utils/httpClient', () => ({
	createHttpClient: () => ({ get }),
}));

vi.mock('@/utils/Config', () => ({
	default: {
		VUE_APP_ESTATE_SERVICE: 'https://estateservice.test/api/v1.0',
	},
}));

const user: { isAuthenticated: boolean } = { isAuthenticated: true };
vi.mock('@/store/store', () => ({
	default: { state: { user } },
}));

type CurrentUser = ReturnType<
	typeof import('../useCurrentUser').useCurrentUser
>;

/**
 * The module caches the user in module scope, so every case needs a freshly
 * imported copy of it.
 */
async function loadWith(
	response: { workOrderTypes: string[] } | Error
): Promise<CurrentUser> {
	vi.resetModules();
	get.mockReset();
	if (response instanceof Error) {
		get.mockRejectedValue(response);
	} else {
		get.mockResolvedValue({
			data: {
				email: 'test@umea.se',
				fullName: 'Test Testsson',
				permissions: { workOrderTypes: response.workOrderTypes },
			},
		});
	}

	const { useCurrentUser } = await import('../useCurrentUser');
	const currentUser = useCurrentUser();
	await currentUser.loadCurrentUser();
	return currentUser;
}

describe('useCurrentUser', () => {
	beforeEach(() => {
		get.mockReset();
		user.isAuthenticated = true;
	});

	test('calls GET /me', async () => {
		await loadWith({ workOrderTypes: [] });

		expect(get).toHaveBeenCalledWith(
			'https://estateservice.test/api/v1.0/me'
		);
	});

	test('exposes identity so callers need not decode the token', async () => {
		const currentUser = await loadWith({ workOrderTypes: [] });

		expect(currentUser.email.value).toBe('test@umea.se');
		expect(currentUser.fullName.value).toBe('Test Testsson');
	});

	test('allows a work order type the API returned', async () => {
		const currentUser = await loadWith({
			workOrderTypes: ['errorReport', 'spaceRequirement'],
		});

		expect(
			currentUser.canCreateWorkOrderType(
				EstateOrderCategory.SpaceRequirement
			)
		).toBe(true);
	});

	test('denies a work order type the API withheld', async () => {
		const currentUser = await loadWith({ workOrderTypes: ['errorReport'] });

		expect(
			currentUser.canCreateWorkOrderType(
				EstateOrderCategory.SpaceRequirement
			)
		).toBe(false);
	});

	test('fails closed when the request fails', async () => {
		const currentUser = await loadWith(new Error('network down'));

		expect(
			currentUser.canCreateWorkOrderType(
				EstateOrderCategory.SpaceRequirement
			)
		).toBe(false);
		expect(currentUser.email.value).toBeNull();
	});

	test('loads only once', async () => {
		const currentUser = await loadWith({ workOrderTypes: [] });
		await currentUser.loadCurrentUser();

		expect(get).toHaveBeenCalledTimes(1);
	});

	test('retries after a failed attempt instead of staying empty', async () => {
		// Fail closed must not mean "hidden for the rest of the session".
		const currentUser = await loadWith(new Error('network down'));
		expect(currentUser.loaded.value).toBe(false);

		get.mockResolvedValue({
			data: {
				email: 'test@umea.se',
				fullName: 'Test Testsson',
				permissions: { workOrderTypes: ['spaceRequirement'] },
			},
		});
		await currentUser.loadCurrentUser();

		expect(get).toHaveBeenCalledTimes(2);
		expect(
			currentUser.canCreateWorkOrderType(
				EstateOrderCategory.SpaceRequirement
			)
		).toBe(true);
	});

	test('skips the request when the user is not signed in', async () => {
		vi.resetModules();
		get.mockReset();
		user.isAuthenticated = false;

		const { useCurrentUser } = await import('../useCurrentUser');
		const currentUser = useCurrentUser();
		await currentUser.loadCurrentUser();

		expect(get).not.toHaveBeenCalled();
		expect(currentUser.loaded.value).toBe(false);
	});

	test('loads after signing in, once authenticated', async () => {
		vi.resetModules();
		get.mockReset();
		user.isAuthenticated = false;

		const { useCurrentUser } = await import('../useCurrentUser');
		const currentUser = useCurrentUser();
		await currentUser.loadCurrentUser();

		user.isAuthenticated = true;
		get.mockResolvedValue({
			data: {
				email: 'test@umea.se',
				fullName: 'Test Testsson',
				permissions: { workOrderTypes: ['spaceRequirement'] },
			},
		});
		await currentUser.loadCurrentUser();

		expect(
			currentUser.canCreateWorkOrderType(
				EstateOrderCategory.SpaceRequirement
			)
		).toBe(true);
	});
});
