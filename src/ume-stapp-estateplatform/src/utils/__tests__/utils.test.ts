import { getFormattedDate, getReviewSignalColor } from '../utils';
import { describe, expect, test } from 'vitest';

describe('utils', () => {
	describe('getFormattedDate', () => {
		test('Returns correctly formatted date string 1', () => {
			const dateString = '2022-12-16';
			expect(getFormattedDate(dateString)).toBe('Dec 16 2022');
		});

		// Note that due to the current inmplementation, the month part will always be in english.
		test('Returns correctly formatted date string 2', () => {
			const dateString = '2022-05-16';
			expect(getFormattedDate(dateString)).toBe('May 16 2022');
		});
	});

	describe('getReviewSignalColor', () => {
		test('Returns correct color for True', () => {
			const item = { review: 'True' };
			expect(getReviewSignalColor(item)).toBe('#eaffea');
		});

		test('Returns correct color for False', () => {
			const item = { review: 'False' };
			expect(getReviewSignalColor(item)).toBe('#ffe6ea');
		});

		test('Returns correct color for other values', () => {
			const item = { review: 'anything' };
			expect(getReviewSignalColor(item)).toBe('none');
		});
	});
});
