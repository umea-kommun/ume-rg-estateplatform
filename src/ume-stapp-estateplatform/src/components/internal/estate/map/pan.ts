import Map from 'ol/Map';

export const smoothZoom = (map: Map | null, delta: number) => {
	if (!map) return;
	const view = map.getView();
	const current = view.getZoom() ?? 0;
	const target = view.getConstrainedZoom(current + delta);
	view.animate({
		zoom: target,
		duration: 250,
		easing: (t: number) => 1 - Math.pow(1 - t, 3), // easeOutCubic
	});
};

export const panToWithPixelOffset = (
	map: Map | null,
	coord: number[],
	offsetPx: [number, number]
) => {
	if (!map) return;
	const view = map.getView();

	// current center in pixel space
	const center = view.getCenter();
	if (!center) return;

	const currentCenterPx = map.getPixelFromCoordinate(center);

	// clicked feature pixel
	const featurePx = map.getPixelFromCoordinate(coord);

	// we want feature to appear at (map center + offset)
	const targetFeaturePx: [number, number] = [
		currentCenterPx[0] + offsetPx[0],
		currentCenterPx[1] + offsetPx[1],
	];

	// compute how much we need to move the view in pixels
	const deltaPx: [number, number] = [
		featurePx[0] - targetFeaturePx[0],
		featurePx[1] - targetFeaturePx[1],
	];

	// new center pixel = current center pixel + delta
	const newCenterPx: [number, number] = [
		currentCenterPx[0] + deltaPx[0],
		currentCenterPx[1] + deltaPx[1],
	];

	const newCenterCoord = map.getCoordinateFromPixel(newCenterPx);

	view.animate({
		center: newCenterCoord,
		duration: 300,
	});
};
