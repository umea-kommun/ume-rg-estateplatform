import { describe, expect, test, vi } from 'vitest';

const get = vi.fn();

vi.mock('@/utils/httpClient', () => ({
	createHttpClient: () => ({ get }),
}));

vi.mock('@/utils/Config', () => ({
	default: {
		VUE_APP_ESTATE_SERVICE: 'https://estateservice.test/api/v1.0',
	},
}));

type Flags = ReturnType<typeof import('../useFeatureFlags').useFeatureFlags>;

/**
 * The module caches its flag state in module scope, so every case needs a
 * freshly imported copy of it.
 */
async function loadWith(features: string[] | Error): Promise<Flags> {
	vi.resetModules();
	get.mockReset();
	if (features instanceof Error) {
		get.mockRejectedValue(features);
	} else {
		get.mockResolvedValue({ data: features });
	}

	const { useFeatureFlags } = await import('../useFeatureFlags');
	const flags = useFeatureFlags();
	await flags.loadFeatures();
	return flags;
}

describe('useFeatureFlags', () => {
	test('Calls the features endpoint', async () => {
		await loadWith(['EstateService']);

		expect(get).toHaveBeenCalledWith(
			'https://estateservice.test/api/v1.0/features'
		);
	});

	test('Flag returned by the server is enabled, regardless of casing', async () => {
		const flags = await loadWith(['ErrorReport']);

		expect(flags.isEnabled('errorreport')).toBe(true);
	});

	test('Flag not returned by the server is disabled', async () => {
		const flags = await loadWith(['EstateService']);

		expect(flags.isEnabled('ErrorReport')).toBe(false);
	});

	test('Unreachable endpoint fails open', async () => {
		// A flag we cannot read must not hide a shipped feature. Per-user
		// permissions take the opposite stance, see useCurrentUser.
		const flags = await loadWith(new Error('network'));

		expect(flags.isEnabled('ErrorReport')).toBe(true);
	});

	test('Loads only once', async () => {
		const flags = await loadWith(['EstateService']);
		await flags.loadFeatures();

		expect(get).toHaveBeenCalledTimes(1);
	});
});
