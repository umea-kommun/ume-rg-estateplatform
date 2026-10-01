import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { createTileErrorTracker, TileErrorReport } from '../tileErrors';
import type { TileSourceEvent } from 'ol/source/Tile';

function tileEvent(src: string) {
	// Mirrors the ImageTile shape getTileUrl reads: on error OpenLayers keeps
	// the failing request URL on the tile (`src_`), not on its (now blank) image.
	return { tile: { src_: src } } as unknown as TileSourceEvent;
}

describe('createTileErrorTracker', () => {
	beforeEach(() => vi.useFakeTimers());
	afterEach(() => vi.useRealTimers());

	test('reports a burst of failed tiles once', () => {
		const reports: TileErrorReport[] = [];
		const { handleTileLoadError } = createTileErrorTracker(
			(r) => reports.push(r),
			2000
		);

		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/a.png'));
		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/b.png'));
		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/c.png'));

		expect(reports).toHaveLength(0);

		vi.advanceTimersByTime(2000);

		expect(reports).toEqual([
			{
				layerName: 'Lovisa',
				failedTileCount: 3,
				exampleTileUrl: 'https://wms.umea.se/a.png',
			},
		]);
	});

	test('reports a later burst separately', () => {
		const reports: TileErrorReport[] = [];
		const { handleTileLoadError } = createTileErrorTracker(
			(r) => reports.push(r),
			2000
		);

		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/a.png'));
		vi.advanceTimersByTime(2000);
		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/d.png'));
		vi.advanceTimersByTime(2000);

		expect(reports.map((r) => r.exampleTileUrl)).toEqual([
			'https://wms.umea.se/a.png',
			'https://wms.umea.se/d.png',
		]);
	});

	test('flushes the pending burst when the base layer changes', () => {
		const reports: TileErrorReport[] = [];
		const { handleTileLoadError } = createTileErrorTracker(
			(r) => reports.push(r),
			2000
		);

		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/a.png'));
		handleTileLoadError('Ortofoto', tileEvent('https://wms.umea.se/b.png'));
		vi.advanceTimersByTime(2000);

		expect(reports).toEqual([
			{
				layerName: 'Lovisa',
				failedTileCount: 1,
				exampleTileUrl: 'https://wms.umea.se/a.png',
			},
			{
				layerName: 'Ortofoto',
				failedTileCount: 1,
				exampleTileUrl: 'https://wms.umea.se/b.png',
			},
		]);
	});

	test('a stale timer from the previous layer does not split the new burst', () => {
		const reports: TileErrorReport[] = [];
		const { handleTileLoadError } = createTileErrorTracker(
			(r) => reports.push(r),
			2000
		);

		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/a.png'));
		vi.advanceTimersByTime(500);
		// Layer change flushes Lovisa; the Lovisa timer must not linger.
		handleTileLoadError('Ortofoto', tileEvent('https://wms.umea.se/b.png'));
		// The lingering Lovisa timer (scheduled at t=0) would fire here.
		vi.advanceTimersByTime(1500);
		handleTileLoadError('Ortofoto', tileEvent('https://wms.umea.se/c.png'));
		vi.advanceTimersByTime(2000);

		expect(reports.filter((r) => r.layerName === 'Ortofoto')).toEqual([
			{
				layerName: 'Ortofoto',
				failedTileCount: 2,
				exampleTileUrl: 'https://wms.umea.se/b.png',
			},
		]);
	});

	test('reset drops pending errors without reporting', () => {
		const reports: TileErrorReport[] = [];
		const { handleTileLoadError, reset } = createTileErrorTracker(
			(r) => reports.push(r),
			2000
		);

		handleTileLoadError('Lovisa', tileEvent('https://wms.umea.se/a.png'));
		reset();
		vi.advanceTimersByTime(2000);

		expect(reports).toHaveLength(0);
	});

	test('tolerates a tile without a url', () => {
		const reports: TileErrorReport[] = [];
		const { handleTileLoadError } = createTileErrorTracker(
			(r) => reports.push(r),
			2000
		);

		handleTileLoadError('Lovisa', {} as TileSourceEvent);
		vi.advanceTimersByTime(2000);

		expect(reports[0].exampleTileUrl).toBe('');
	});
});
