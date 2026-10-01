import { describe, expect, test } from 'vitest';
import {
	formatDateTime,
	formatFullDate,
	formatRecentDate,
} from '../formatRecentDate';

// Local times, so the day boundaries hold in any time zone.
const now = new Date(2026, 8, 28, 15, 30); // Monday 28 September 2026

describe('formatRecentDate', () => {
	test.each([
		['earlier today', new Date(2026, 8, 28, 9, 5), 'I dag 09:05'],
		['just after midnight', new Date(2026, 8, 28, 0, 1), 'I dag 00:01'],
		['yesterday', new Date(2026, 8, 27, 23, 59), 'I går 23:59'],
		['two days ago', new Date(2026, 8, 26, 8, 0), 'Lör 08:00'],
		['six days ago', new Date(2026, 8, 22, 12, 0), 'Tis 12:00'],
		['a week ago', new Date(2026, 8, 21, 12, 0), '2026-09-21'],
		['last year', new Date(2025, 11, 31, 12, 0), '2025-12-31'],
	])('shows %s as %s', (_, date, expected) => {
		expect(formatRecentDate(date, 'sv', now)).toBe(expected);
	});

	test.each([
		[new Date(2026, 8, 28, 9, 5), 'Today 09:05 AM'],
		[new Date(2026, 8, 27, 14, 0), 'Yesterday 02:00 PM'],
	])('follows the locale: %s as %s', (date, expected) => {
		expect(formatRecentDate(date, 'en', now)).toBe(expected);
	});

	test('shows a slightly future time (clock skew) as today', () => {
		expect(formatRecentDate(new Date(2026, 8, 28, 15, 31), 'sv', now)).toBe(
			'I dag 15:31'
		);
	});
});

describe('formatDateTime', () => {
	test.each([
		['sv', '2026-09-16 12:05'],
		['en', '9/16/2026, 12:05 PM'],
	])('formats %s as %s', (locale, expected) => {
		expect(formatDateTime(new Date(2026, 8, 16, 12, 5), locale)).toBe(
			expected
		);
	});
});

describe('formatFullDate', () => {
	test('includes the weekday, date and time', () => {
		expect(formatFullDate(new Date(2026, 8, 28, 12, 5), 'sv')).toBe(
			'måndag 28 september 2026 kl. 12:05'
		);
	});
});
