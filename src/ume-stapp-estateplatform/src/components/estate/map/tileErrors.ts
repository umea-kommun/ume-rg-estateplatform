import type { TileSourceEvent } from 'ol/source/Tile';

/** Aggregation window before a burst of failed tiles is reported once. */
const REPORT_DELAY_MS = 3000;

export interface TileErrorReport {
	layerName: string;
	failedTileCount: number;
	exampleTileUrl: string;
}

function getTileUrl(event: TileSourceEvent) {
	const tile = event.tile as { src_?: unknown; key?: unknown } | undefined;

	if (typeof tile?.src_ === 'string') {
		return tile.src_;
	}
	return typeof tile?.key === 'string' ? tile.key : '';
}

/**
 * OpenLayers emits one `tileloaderror` per failing tile, so a single broken WMS
 * request produces dozens of events. This collects them so a burst is reported
 * once (with a count and an example tile URL) instead of flooding the log.
 */
export function createTileErrorTracker(
	onReport: (report: TileErrorReport) => void,
	reportDelayMs = REPORT_DELAY_MS
) {
	let failedTileCount = 0;
	let exampleTileUrl = '';
	let currentLayerName = '';
	let timer: ReturnType<typeof setTimeout> | null = null;

	const flush = () => {
		// Cancel any pending timeout: flush is also called synchronously on a
		// layer change, and a leftover timer would later fire an extra flush and
		// split the next layer's burst into several reports.
		if (timer !== null) {
			clearTimeout(timer);
			timer = null;
		}
		if (failedTileCount === 0) {
			return;
		}

		onReport({
			layerName: currentLayerName,
			failedTileCount,
			exampleTileUrl,
		});

		failedTileCount = 0;
		exampleTileUrl = '';
	};

	const handleTileLoadError = (layerName: string, event: TileSourceEvent) => {
		if (layerName !== currentLayerName) {
			flush();
			currentLayerName = layerName;
		}

		failedTileCount++;
		exampleTileUrl ||= getTileUrl(event);

		timer ??= setTimeout(flush, reportDelayMs);
	};

	const reset = () => {
		if (timer !== null) {
			clearTimeout(timer);
			timer = null;
		}
		failedTileCount = 0;
		exampleTileUrl = '';
	};

	return { handleTileLoadError, reset };
}
