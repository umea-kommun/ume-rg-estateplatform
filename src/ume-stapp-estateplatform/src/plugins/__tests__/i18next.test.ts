import { beforeEach, describe, expect, test, vi } from 'vitest';

describe('i18next', () => {
	beforeEach(() => {
		vi.resetModules();
		localStorage.clear();
		sessionStorage.clear();
		document.documentElement.lang = 'sv';
	});

	test('Applies the stored locale to the document language on startup', async () => {
		localStorage.setItem('locale', 'en');

		await import('../i18next');

		expect(document.documentElement.lang).toBe('en');
	});

	test('Updates the document language when the locale changes', async () => {
		const { setLocale } = await import('../i18next');

		setLocale('en');

		expect(document.documentElement.lang).toBe('en');
	});

	test('Remembers the locale across browser sessions', async () => {
		const { setLocale } = await import('../i18next');

		setLocale('en');

		// sessionStorage is cleared when the browser closes, localStorage is not
		sessionStorage.clear();
		vi.resetModules();
		const { getLocale } = await import('../i18next');

		expect(getLocale()).toBe('en');
	});

	test('Falls back to Swedish when nothing is stored', async () => {
		const { getLocale } = await import('../i18next');

		expect(getLocale()).toBe('sv');
	});

	test('Falls back to Swedish when the stored locale is unsupported', async () => {
		localStorage.setItem('locale', 'de');

		const { getLocale } = await import('../i18next');

		expect(getLocale()).toBe('sv');
	});

	test.each(['constructor', 'toString'])(
		'Falls back to Swedish when the stored locale is the inherited property %s',
		async (storedLocale) => {
			localStorage.setItem('locale', storedLocale);

			const { getLocale } = await import('../i18next');

			expect(getLocale()).toBe('sv');
		}
	);
});
