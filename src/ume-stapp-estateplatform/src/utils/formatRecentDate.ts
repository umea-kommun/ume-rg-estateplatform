const capitalize = (text: string) =>
	text.charAt(0).toLocaleUpperCase() + text.slice(1);

const startOfDay = (date: Date) =>
	new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();

const DayMs = 24 * 60 * 60 * 1000;

/**
 * Short date for lists: "i dag" or "i går" with the time, the weekday with the
 * time within the past week, and the date after that.
 */
export function formatRecentDate(date: Date, locale: string, now = new Date()) {
	const time = date.toLocaleTimeString(locale, {
		hour: '2-digit',
		minute: '2-digit',
	});
	// Rounded, since a day across a daylight saving change is not 24 hours.
	const daysAgo = Math.round((startOfDay(now) - startOfDay(date)) / DayMs);

	// A slightly future time (clock skew) still reads as today.
	if (daysAgo <= 1) {
		const day = new Intl.RelativeTimeFormat(locale, {
			numeric: 'auto',
		}).format(-Math.max(daysAgo, 0), 'day');
		return `${capitalize(day)} ${time}`;
	}
	// Under a week, so a weekday never means two different days.
	if (daysAgo < 7) {
		const weekday = date.toLocaleDateString(locale, { weekday: 'short' });
		return `${capitalize(weekday)} ${time}`;
	}
	return date.toLocaleDateString(locale);
}

/** Numeric date and time, such as "2026-09-16 12:05". */
export const formatDateTime = (date: Date, locale: string) =>
	date.toLocaleString(locale, {
		year: 'numeric',
		month: 'numeric',
		day: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
	});

/** Full date and time, for a tooltip next to formatRecentDate. */
export const formatFullDate = (date: Date, locale: string) =>
	date.toLocaleString(locale, { dateStyle: 'full', timeStyle: 'short' });
