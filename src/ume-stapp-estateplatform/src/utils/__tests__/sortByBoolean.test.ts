// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/utils/__tests__/sortByBoolean.test.ts @ 84b4a5dc
import { describe, expect, test } from 'vitest';
import { sortByBoolean } from '../sortByBoolean';

describe('sortByBoolean', () => {
	test('moves true values to the front without mutating the source array', () => {
		const items = [
			{ id: 1, isFavorite: false },
			{ id: 2, isFavorite: true },
			{ id: 3, isFavorite: false },
			{ id: 4, isFavorite: true },
		];

		const sortedItems = sortByBoolean(items, (item) => item.isFavorite);

		expect(sortedItems.map((item) => item.id)).toEqual([2, 4, 1, 3]);
		expect(items.map((item) => item.id)).toEqual([1, 2, 3, 4]);
	});

	test('preserves the relative order within true and false groups', () => {
		const items = [
			{ id: 1, isFavorite: true },
			{ id: 2, isFavorite: true },
			{ id: 3, isFavorite: false },
			{ id: 4, isFavorite: false },
		];

		const sortedItems = sortByBoolean(items, (item) => item.isFavorite);

		expect(sortedItems.map((item) => item.id)).toEqual([1, 2, 3, 4]);
	});

	test('can sort by any boolean field', () => {
		const items = [
			{ id: 1, isPinned: false },
			{ id: 2, isPinned: true },
			{ id: 3, isPinned: false },
		];

		const sortedItems = sortByBoolean(items, (item) => item.isPinned);

		expect(sortedItems.map((item) => item.id)).toEqual([2, 1, 3]);
	});
});
