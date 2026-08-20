import { createEmpty, extend } from 'ol/extent';
import CircleStyle from 'ol/style/Circle';
import Cluster from 'ol/source/Cluster';
import Feature, { FeatureLike } from 'ol/Feature';
import Fill from 'ol/style/Fill';
import Point from 'ol/geom/Point';
import Stroke from 'ol/style/Stroke';
import Style from 'ol/style/Style';
import Text from 'ol/style/Text';
import VectorLayer from 'ol/layer/Vector';
import VectorSource from 'ol/source/Vector';
import MapHouseSvg from '@/assets/map_house.svg';
import MapHouseMultipleSvg from '@/assets/map_house_multi.svg';
import MapHouseHighlightSvg from '@/assets/map_house_grey.svg';
import Icon from 'ol/style/Icon';
import { to3016 } from './layer';
import { IMapPoint } from '@/models/estate/Interfaces';
import { Map } from 'ol';

const FIT_POINTS_PADDING = 100;
const BOUNDS = {
	latMin: 62.3281,
	latMax: 64.3729,
	lonMin: 19.624,
	lonMax: 21.8917,
};

export function createPointLayers() {
	const pointSource = new VectorSource();

	const clusterSource = new Cluster({
		distance: 40, // px; how close points must be to merge
		minDistance: 15, // optional; helps avoid tiny clusters
		source: pointSource,
	});

	const singleCircle = new Icon({
		src: MapHouseSvg,
	});
	const singleStyle = new Style({ image: singleCircle });

	const cache: { [key: string]: Style } = {};

	function allFeaturesAtSameLocation(clustered: Feature[]) {
		if (clustered.length <= 1) {
			return true;
		}

		const extent = createEmpty();
		clustered.forEach((f) => {
			const geo = f.getGeometry();
			if (geo) {
				extend(extent, geo.getExtent());
			}
		});

		const width = extent[2] - extent[0];
		const height = extent[3] - extent[1];

		const tolerance = 4;
		return width <= tolerance && height <= tolerance;
	}

	function getSameLocationPointStyle(size: number) {
		return new Style({
			image: new Icon({
				src: MapHouseMultipleSvg,
			}),
			text: new Text({
				text: String(size),
				font: `normal ${size >= 10 ? 12 : 14}px sans-serif`,
				fill: new Fill({ color: '#000' }),
				offsetX: 8,
				offsetY: size >= 10 ? 0 : 1,
				textBaseline: 'bottom',
				textAlign: 'center',
			}),
		});
	}

	function getClusterPointStyle(size: number) {
		const radius = Math.max(14, Math.min(26, 10 + Math.log2(size) * 3));
		return new Style({
			image: new CircleStyle({
				radius,
				fill: new Fill({
					color: '#006e1e',
				}),
				stroke: new Stroke({ color: '#fff', width: 2 }),
			}),
			text: new Text({
				text: String(size),
				font: 'bold 14px sans-serif',
				fill: new Fill({ color: '#fff' }),
				textBaseline: 'middle',
				textAlign: 'center',
			}),
		});
	}

	function clusterStyle(feature: FeatureLike) {
		const clustered = feature.get('features') as Feature[] | undefined;
		const size = clustered?.length ?? 0;

		if (size <= 1) {
			return singleStyle;
		}

		const allSameLocation = allFeaturesAtSameLocation(clustered || []);
		const cacheKey = `${size}_${allSameLocation}`;

		// Cluster, reuse cached style
		if (!cache[cacheKey]) {
			cache[cacheKey] = allSameLocation
				? getSameLocationPointStyle(size)
				: getClusterPointStyle(size);
		}
		return cache[cacheKey];
	}

	const clusterLayer = new VectorLayer({
		source: clusterSource,
		style: clusterStyle,
	});

	const highlightSource = new VectorSource({ wrapX: false });
	const highlightLayer = new VectorLayer({
		source: highlightSource,
		style: new Style({
			image: new Icon({
				src: MapHouseHighlightSvg,
				scale: 1.5,
			}),
		}),
		zIndex: 9999, // render above clusters
	});

	let idToFeature: { [key: number]: Feature } = {};

	const updatePoints = (
		points: IMapPoint[],
		map: Map | null,
		fitPoints?: boolean
	) => {
		// Only show points within Umeå municipality bounds
		points = points.filter(
			(p) =>
				p.lat <= BOUNDS.latMax &&
				p.lat >= BOUNDS.latMin &&
				p.lon >= BOUNDS.lonMin &&
				p.lon <= BOUNDS.lonMax
		);

		if (!points.length) {
			pointSource.clear(true);
			return;
		}

		const features: Feature[] = new Array(points.length);
		const extent = createEmpty();

		idToFeature = {};
		for (let i = 0; i < points.length; i++) {
			const p = points[i];
			const coord = to3016([p.lon, p.lat]);
			const f = new Feature({
				geometry: new Point(coord),
				id: p.id,
				type: p.type,
				grossArea: p.grossArea,
			});
			f.setId(p.id);
			features[i] = f;
			idToFeature[p.id as number] = f;
			extend(extent, (f.getGeometry() as Point).getExtent());
		}

		// Add all points to source
		pointSource.clear(true);
		pointSource.addFeatures(features);
		clusterSource.refresh();

		if (fitPoints && map) {
			map.getView().fit(extent, {
				size: map.getSize(),
				padding: [
					FIT_POINTS_PADDING,
					FIT_POINTS_PADDING,
					FIT_POINTS_PADDING,
					FIT_POINTS_PADDING,
				],
				maxZoom: 12,
				duration: 0,
			});
		}
	};

	function setHighlightedPoint(id: number | null) {
		highlightSource.clear(true);
		if (id == null) return;

		const original = idToFeature[id];
		if (!original) return;

		const geom = original.getGeometry() as Point | undefined;
		if (!geom) return;

		const f = new Feature({
			geometry: new Point(geom.getCoordinates()),
			id,
		});
		highlightSource.addFeature(f);
	}

	return {
		FIT_POINTS_PADDING,
		clusterLayer,
		highlightLayer,
		updatePoints,
		setHighlightedPoint,
		allFeaturesAtSameLocation,
	};
}
