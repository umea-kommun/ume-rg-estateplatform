import { describe, expect, test, vi, beforeEach, afterEach } from 'vitest';
import { effectScope, nextTick, ref } from 'vue';
import { useDialogHistory } from '../useDialogHistory';

/**
 * Minimal session history: a stack of states with a cursor, where back and
 * forward move the cursor and fire popstate, and pushState drops whatever was
 * ahead of the cursor.
 */
const fakeHistory = () => {
	let entries: unknown[] = [null];
	let index = 0;

	const move = (delta: number) => {
		const target = index + delta;
		if (target < 0 || target >= entries.length) {
			return;
		}
		index = target;
		window.dispatchEvent(new PopStateEvent('popstate'));
	};

	vi.spyOn(window.history, 'state', 'get').mockImplementation(
		() => entries[index]
	);
	vi.spyOn(window.history, 'pushState').mockImplementation((state) => {
		entries = [...entries.slice(0, index + 1), state];
		index = entries.length - 1;
	});
	vi.spyOn(window.history, 'back').mockImplementation(() => move(-1));

	return {
		back: () => move(-1),
		forward: () => move(1),
		length: () => entries.length,
		index: () => index,
	};
};

const runInScope = (fn: () => void) => {
	const scope = effectScope();
	scope.run(fn);
	return scope;
};

const openDialog = async (open: { value: boolean }) => {
	open.value = true;
	await nextTick();
};

describe('useDialogHistory', () => {
	let history: ReturnType<typeof fakeHistory>;

	beforeEach(() => {
		history = fakeHistory();
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	test('Opening pushes a history entry so back does not leave the page', async () => {
		const open = ref(false);
		runInScope(() => useDialogHistory(open));

		await openDialog(open);

		expect(history.length()).toBe(2);
		expect(history.index()).toBe(1);
	});

	test('Back navigation closes the dialog', async () => {
		const open = ref(false);
		runInScope(() => useDialogHistory(open));

		await openDialog(open);
		history.back();
		await nextTick();

		expect(open.value).toBe(false);
		expect(history.index()).toBe(0);
	});

	test('Forward navigation after back reopens the dialog', async () => {
		const open = ref(false);
		runInScope(() => useDialogHistory(open));

		await openDialog(open);
		history.back();
		await nextTick();
		history.forward();
		await nextTick();

		expect(open.value).toBe(true);
		expect(history.length()).toBe(2);
	});

	test('Closing from the UI pops the pushed entry without closing twice', async () => {
		const open = ref(false);
		runInScope(() => useDialogHistory(open));

		await openDialog(open);
		open.value = false;
		await nextTick();

		expect(history.index()).toBe(0);
		expect(window.history.back).toHaveBeenCalledTimes(1);
	});

	test('A closed dialog ignores back navigations it did not push', async () => {
		const open = ref(false);
		runInScope(() => useDialogHistory(open));

		history.back();
		await nextTick();

		expect(open.value).toBe(false);
		expect(window.history.back).not.toHaveBeenCalled();
	});

	test('A sibling dialog on the same page is left closed', async () => {
		const map = ref(false);
		const blueprint = ref(false);
		runInScope(() => {
			useDialogHistory(map);
			useDialogHistory(blueprint);
		});

		await openDialog(map);
		history.back();
		await nextTick();
		history.forward();
		await nextTick();

		expect(map.value).toBe(true);
		expect(blueprint.value).toBe(false);
	});

	test('Reopening after a back navigation pushes a new entry', async () => {
		const open = ref(false);
		runInScope(() => useDialogHistory(open));

		await openDialog(open);
		history.back();
		await nextTick();
		await openDialog(open);

		expect(history.length()).toBe(2);
		expect(history.index()).toBe(1);
		expect(window.history.pushState).toHaveBeenCalledTimes(2);
	});

	test('An unmounted dialog stops listening for history navigations', async () => {
		const open = ref(false);
		const scope = runInScope(() => useDialogHistory(open));

		await openDialog(open);
		scope.stop();
		history.back();
		await nextTick();

		expect(open.value).toBe(true);
	});
});
