import { addProjection, getTransform, Projection } from 'ol/proj';
import { getTopLeft, getWidth } from 'ol/extent';
import { register as registerProj4 } from 'ol/proj/proj4';
import proj4 from 'proj4';
import TileGrid from 'ol/tilegrid/TileGrid';
import TileLayer from 'ol/layer/Tile';
import TileWMS from 'ol/source/TileWMS';

/** Initialize layers and Umeå coordinates system */
proj4.defs(
	'EPSG:3016',
	'+proj=tmerc +lat_0=0 +lon_0=20.25 +k=1 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs'
);
registerProj4(proj4);

const SWEREF992015 = new Projection({
	code: 'EPSG:3016', // Umeå municipality coordinate system
	extent: [-93218.3385, 7034909.8738, 261434.62459999998, 7744215.8],
	units: 'm',
});
addProjection(SWEREF992015);

const to3016 = getTransform('EPSG:4326', 'EPSG:3016'); // Convert from lon/lat to EPSG:3016 (Umeå kommun coordinates)

function createResolutions() {
	const projectionExtent = SWEREF992015.getExtent();
	const size = getWidth(projectionExtent) / 256;
	const resolutions: number[] = [];
	for (let z = 0; z < 14; z++) {
		resolutions[z] = size / Math.pow(2, z);
	}
	return resolutions;
}

function createWmsLayer(layerName: string, visible: boolean) {
	const resolutions = createResolutions();
	const tileGrid = new TileGrid({
		origin: getTopLeft(SWEREF992015.getExtent()),
		resolutions,
		tileSize: 256,
	});

	return new TileLayer({
		visible,
		source: new TileWMS({
			url: 'https://wms.umea.se/geoserver/gwc/service/wms',
			params: {
				LAYERS: layerName,
				FORMAT: 'image/png',
				TILED: true,
				VERSION: '1.1.0',
				SRS: 'EPSG:3016',
			},
			tileGrid,
			serverType: 'geoserver',
			projection: SWEREF992015,

			// Needs to be false to work on HiDPI screens (mobile devices etc), since WMS only supports 256px tiles
			hidpi: false,
		}),
	});
}

export { SWEREF992015, to3016, createWmsLayer };
