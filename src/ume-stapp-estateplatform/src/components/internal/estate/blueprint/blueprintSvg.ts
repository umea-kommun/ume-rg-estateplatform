import { computed, onBeforeUnmount, ref, watch } from 'vue';

export const ROOM_TYPE_ID = 3;

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
			const image = document.createElement('image');
			image.setAttribute('href', svgBlobUrl.value);

			const vb = getViewBoxRounded(svgElement);
			image.setAttribute('x', String(vb.x));
			image.setAttribute('y', String(vb.y));
			image.setAttribute('width', String(vb.w));
			image.setAttribute('height', String(vb.h));
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
