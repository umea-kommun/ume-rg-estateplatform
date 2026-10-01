import { flushPromises, mount } from '@vue/test-utils';
import { CanceledError } from 'axios';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { defineComponent, reactive, watch } from 'vue';
import { useWorkOrders } from '../useWorkOrders';

const { get, post, state } = vi.hoisted(() => ({
	get: vi.fn(),
	post: vi.fn(),
	state: { user: { token: 'token' } },
}));
vi.mock('@/utils/httpClient', () => ({
	createHttpClient: () => ({ get, post }),
}));
vi.mock('@/Config', () => ({ default: { VUE_APP_ESTATE_SERVICE: '/api' } }));
vi.mock('@/store/store', async () => {
	const { reactive } = await import('vue');
	return { default: { state: reactive(state) } };
});

const saved = { id: 'one', status: 'Registrerad', statusCheckedAt: null };
const updated = {
	...saved,
	status: 'Pågår',
	statusCheckedAt: '2026-09-10T10:00:00Z',
};

function setup() {
	let result!: ReturnType<typeof useWorkOrders>;
	const wrapper = mount(
		defineComponent({
			setup() {
				result = useWorkOrders();
				return () => null;
			},
		})
	);
	return { wrapper, result };
}

describe('work order refresh', () => {
	beforeEach(() => {
		get.mockReset();
		post.mockReset();
		reactive(state).user.token = 'token';
		get.mockResolvedValue({ data: [saved] });
	});
	afterEach(() => {
		vi.useRealTimers();
	});

	test('waits briefly for a slow refresh, then shows saved data until it finishes', async () => {
		vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
		let finish!: (value: unknown) => void;
		post.mockReturnValue(
			new Promise((resolve) => {
				finish = resolve;
			})
		);
		const { result, wrapper } = setup();
		await flushPromises();
		await vi.advanceTimersByTimeAsync(499);
		expect(result.loading.value).toBe(true);
		expect(result.orders.value).toEqual([]);

		await vi.advanceTimersByTimeAsync(1);
		expect(result.loading.value).toBe(false);
		expect(result.refreshing.value).toBe(true);
		expect(result.orders.value[0].status).toBe('Registrerad');
		expect(post.mock.calls[0][2].params).toBeUndefined();

		finish({ data: { workOrders: [updated], outcome: 'refreshed' } });
		await flushPromises();
		expect(result.refreshing.value).toBe(false);
		expect(result.orders.value[0].status).toBe('Pågår');
		wrapper.unmount();
	});

	test('shows a quick refresh directly, never the saved order', async () => {
		const other = { ...saved, id: 'two' };
		get.mockResolvedValue({ data: [saved, other] });
		post.mockResolvedValue({
			data: { workOrders: [other, updated], outcome: 'refreshed' },
		});
		const shown: string[][] = [];
		const { result, wrapper } = setup();
		// Sync, so a saved list replaced in the same tick is still recorded.
		watch(result.orders, (orders) => shown.push(orders.map((o) => o.id)), {
			flush: 'sync',
		});
		await flushPromises();

		expect(result.loading.value).toBe(false);
		expect(shown).toEqual([['two', 'one']]);
		wrapper.unmount();
	});

	test('shows nothing and skips the refresh when there are no orders', async () => {
		get.mockResolvedValue({ data: [] });
		const { result, wrapper } = setup();
		await flushPromises();

		expect(result.loading.value).toBe(false);
		expect(result.orders.value).toEqual([]);
		expect(post).not.toHaveBeenCalled();
		wrapper.unmount();
	});

	test('retains saved statuses on failure and supports explicit retry', async () => {
		post.mockRejectedValueOnce(new Error('Unavailable'));
		const { result, wrapper } = setup();
		await flushPromises();
		expect(result.outcome.value).toBe('failed');
		expect(result.orders.value[0].status).toBe('Registrerad');
		post.mockResolvedValue({
			data: { workOrders: [updated], outcome: 'refreshed' },
		});
		await result.refresh();
		expect(post).toHaveBeenCalledTimes(2);
		expect(result.orders.value[0].status).toBe('Pågår');
		wrapper.unmount();
	});

	test('exposes a failed outcome without discarding the returned statuses', async () => {
		post.mockResolvedValue({
			data: { workOrders: [updated, saved], outcome: 'failed' },
		});
		const { result, wrapper } = setup();
		await flushPromises();
		expect(result.outcome.value).toBe('failed');
		expect(result.orders.value).toHaveLength(2);
		wrapper.unmount();
	});

	test('never shows notDue, on page open or after a button press', async () => {
		post.mockResolvedValue({
			data: { workOrders: [saved], outcome: 'notDue' },
		});
		const { result, wrapper } = setup();
		await flushPromises();
		expect(result.outcome.value).toBe('');

		await result.refresh();
		expect(result.outcome.value).toBe('');
		wrapper.unmount();
	});

	// Logging out aborts the signal, which is how axios rejects an outstanding request.
	test('clears user data on logout and ignores an outstanding response', async () => {
		vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
		post.mockImplementation(
			(_url: string, _body: unknown, config: { signal: AbortSignal }) =>
				new Promise((_resolve, reject) => {
					config.signal.addEventListener('abort', () =>
						reject(new CanceledError())
					);
				})
		);
		const { result, wrapper } = setup();
		await flushPromises();
		expect(result.refreshing.value).toBe(true);

		reactive(state).user.token = '';
		await flushPromises();
		// The wait that was running for the old user must not show its saved orders.
		await vi.advanceTimersByTimeAsync(500);

		expect(result.orders.value).toEqual([]);
		expect(result.outcome.value).toBe('');
		expect(result.refreshing.value).toBe(false);
		expect(result.loading.value).toBe(false);
		wrapper.unmount();
	});
});
