import { computed, onBeforeUnmount, ref, watch } from 'vue';

export const ROOM_TYPE_ID = 3;

// The browser lays out an SVG image at the size of its <image> element. The
// plan's viewBox is only ~100 units wide, so labels would be laid out
// at under a pixel: WebKit (all iOS browsers) drops them and Chromium spaces
// them out. Laying the image out 100x larger and scaling it back keeps its
// size and position.
const PLAN_IMAGE_SCALE = 100;

export const createPlanImage = (
	href: string,
	viewBox: { x: number; y: number; w: number; h: number }
) => {
	const image = document.createElement('image');
	image.setAttribute('href', href);
	image.setAttribute('x', String(viewBox.x * PLAN_IMAGE_SCALE));
	image.setAttribute('y', String(viewBox.y * PLAN_IMAGE_SCALE));
	image.setAttribute('width', String(viewBox.w * PLAN_IMAGE_SCALE));
	image.setAttribute('height', String(viewBox.h * PLAN_IMAGE_SCALE));
	image.setAttribute('transform', `scale(${1 / PLAN_IMAGE_SCALE})`);
	return image;
};

export const useBlueprintSvg = (blueprintSvg: string) => {
	const svgBlobUrl = ref<string | null>(null);

	const getViewBoxRounded = (element: HTMLElement) => {
		const vb = (
			element.getAttribute('viewBox')?.split(' ') ?? [0, 0, 100, 100]
		).map((v) => Math.round(Number(v)));

		return { x: vb[0], y: vb[1], w: vb[2], h: vb[3] };
	};

	const interactiveSvg = computed(() => {
		const svg = blueprintSvg.replace(/wstxns1:/g, '');

		const parser = new DOMParser();
		const svgDoc = parser.parseFromString(svg, 'image/svg+xml');
		const svgElement = svgDoc.documentElement;

		svgElement.querySelectorAll<SVGAElement>('g[data-iid]').forEach((g) => {
			const [type, idStr] = g.getAttribute('data-iid')?.split('_') ?? [];
			if (type === String(ROOM_TYPE_ID)) {
				g.classList.add('room');
				const id = Number(idStr);
				if (Number.isNaN(id)) {
					g.remove();
				}
			} else {
				g.remove();
			}
		});

		if (svgBlobUrl.value) {
			const vb = getViewBoxRounded(svgElement);
			const image = createPlanImage(svgBlobUrl.value, vb);
			svgElement.setAttribute(
				'viewBox',
				`${vb.x} ${vb.y} ${vb.w} ${vb.h}`
			);
			svgElement.insertBefore(image, svgElement.firstChild);
		}

		return svgElement.outerHTML;
	});

	const getStaticSvg = (svg: string) => {
		const parser = new DOMParser();
		const svgDoc = parser.parseFromString(svg, 'image/svg+xml');
		const svgElement = svgDoc.documentElement;
		const vb = getViewBoxRounded(svgElement);
		svgElement.setAttribute('viewBox', `${vb.x} ${vb.y} ${vb.w} ${vb.h}`);
		const newSvg = svgElement.outerHTML;
		return newSvg;
	};

	onBeforeUnmount(() => {
		if (svgBlobUrl.value) {
			URL.revokeObjectURL(svgBlobUrl.value);
		}
	});

	watch(
		() => blueprintSvg,
		(newSvg) => {
			if (svgBlobUrl.value) {
				URL.revokeObjectURL(svgBlobUrl.value);
			}
			if (newSvg) {
				const staticSvg = getStaticSvg(newSvg);
				const blob = new Blob([staticSvg], { type: 'image/svg+xml' });
				svgBlobUrl.value = URL.createObjectURL(blob);
			}
		},
		{ immediate: true }
	);

	return { interactiveSvg, svgBlobUrl };
};
