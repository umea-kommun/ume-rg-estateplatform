<template>
	<div ref="viewport" class="blueprint-viewport">
		<div ref="content" class="blueprint-content" v-html="interactiveSvg" />
	</div>
</template>

<script setup lang="ts">
import { IBlueprintPosition } from '@/models/estate/Interfaces';
import { useDebounceFn } from '@vueuse/core';
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useBlueprintSvg, ROOM_TYPE_ID } from './blueprintSvg';

const props = defineProps<{
	blueprintSvg: string;
	selectedRoomId?: number;
	startPosition?: IBlueprintPosition | null;
	roomZoomPadding?: number; // 0.2 for 20% padding etc.
}>();
const emit = defineEmits<{
	(e: 'room-clicked', id: number): void;
	(e: 'camera-moved', camPos: IBlueprintPosition): void;
}>();

const viewport = ref<HTMLDivElement | null>(null);
const content = ref<HTMLDivElement | null>(null);

const zoomInDisabled = ref(false);
const zoomOutDisabled = ref(false);

let svgElement: SVGSVGElement | null = null;
let camera: SVGGElement | null = null;

const DOUBLE_CLICK_THRESHOLD = 300;
const ZOOM_STEP = 1.7;

const { interactiveSvg } = useBlueprintSvg(props.blueprintSvg);

// ---------- Camera state (user units) ----------
// Transform applied to <g id="camera">: [x',y'] = s*[x,y] + [tx,ty]
// s is relative zoom; tx,ty are in USER units (viewBox coordinates)
const cam: IBlueprintPosition = { s: 1, tx: 0, ty: 0 };

const baseViewBox = { x: 0, y: 0, w: 0, h: 0 };
let svgW = 0;
let svgH = 0;

// base scale (CSS px per user unit) for a "contain" fit
const s0 = (): number => {
	const w = Math.max(1e-6, baseViewBox.w);
	const h = Math.max(1e-6, baseViewBox.h);
	if (svgW <= 0 || svgH <= 0) return 1;
	return Math.min(svgW / w, svgH / h);
};

// Zoom limits expressed as visible world width (in user units)
let MIN_VISIBLE_W = 10; // max zoom-in (smallest visible width)
const MAX_VISIBLE_W = 300; // min zoom-out (largest visible width)

function computeScaleLimits() {
	const w = Math.max(1e-6, baseViewBox.w);
	const minW = Math.max(1e-6, MIN_VISIBLE_W);
	const maxW = Math.max(minW, MAX_VISIBLE_W || w); // default to fitting whole drawing
	// visibleWidth = w / s  =>  s = w / visibleWidth
	const sMax = w / minW; // zoom-in upper bound
	const sMin = w / maxW; // zoom-out lower bound
	return { sMin, sMax };
}

const cameraMovedDebounced = useDebounceFn(() => {
	emit('camera-moved', { s: cam.s, tx: cam.tx, ty: cam.ty });
}, 300);

function applyCamera() {
	if (
		!camera ||
		!Number.isFinite(cam.s) ||
		!Number.isFinite(cam.tx) ||
		!Number.isFinite(cam.ty)
	) {
		return;
	}

	camera.setAttribute(
		'transform',
		`matrix(${cam.s},0,0,${cam.s},${cam.tx},${cam.ty})`
	);

	cameraMovedDebounced();
}

// ---------- rAF render batching ----------
let needsRender = false;
function scheduleRender() {
	if (needsRender) return;
	needsRender = true;
	requestAnimationFrame(() => {
		needsRender = false;
		applyCamera();
	});
}

// ---------- Init: wrap all <svg> children into <g id="camera"> ----------
function ensureCameraWrapper() {
	if (!svgElement) return;

	svgW = svgElement.clientWidth;
	svgH = svgElement.clientHeight;

	// Parse/initialize base viewBox
	const vb = svgElement.viewBox.baseVal;
	if (vb && (vb.width || vb.height)) {
		baseViewBox.x = vb.x;
		baseViewBox.y = vb.y;
		baseViewBox.w = vb.width;
		baseViewBox.h = vb.height;
	} else {
		// Fallback if no viewBox: build one from width/height
		const w =
			svgElement.width.baseVal.value || svgElement.clientWidth || 1000;
		const h =
			svgElement.height.baseVal.value || svgElement.clientHeight || 1000;
		baseViewBox.x = 0;
		baseViewBox.y = 0;
		baseViewBox.w = w;
		baseViewBox.h = h;
		svgElement.setAttribute('viewBox', `0 0 ${w} ${h}`);
	}

	// Cache viewport size
	svgW = svgElement.clientWidth;
	MIN_VISIBLE_W = Math.max(5, baseViewBox.w / 100); // at least 5 user units or 1/100 of full width

	// Create camera group and move children into it (keep <defs>/<title> outside)
	camera = svgElement.querySelector('#camera') as SVGGElement | null;
	if (!camera) {
		camera = document.createElementNS('http://www.w3.org/2000/svg', 'g');
		camera.setAttribute('id', 'camera');

		const nodesToMove: ChildNode[] = [];
		svgElement.childNodes.forEach((n) => {
			if (n.nodeType === Node.ELEMENT_NODE) {
				const tag = (n as Element).tagName.toLowerCase();
				if (tag === 'defs' || tag === 'title') return; // keep in place
			}
			nodesToMove.push(n);
		});

		// Insert camera after any <defs>/<title>
		let insertBeforeNode: ChildNode | null = null;
		for (const n of Array.from(svgElement.childNodes)) {
			if (
				!(
					n.nodeType === Node.ELEMENT_NODE &&
					['defs', 'title'].includes(
						(n as Element).tagName.toLowerCase()
					)
				)
			) {
				insertBeforeNode = n;
				break;
			}
		}
		svgElement.insertBefore(camera, insertBeforeNode);
		nodesToMove.forEach((n) => camera?.appendChild(n));
	}

	// Initial camera: identity relative to base viewBox
	if (props.startPosition) {
		cam.s = props.startPosition.s;
		cam.tx = props.startPosition.tx;
		cam.ty = props.startPosition.ty;
	} else {
		cam.s = 1;
		cam.tx = 0;
		cam.ty = 0;
	}
	applyCamera();
}

// ---------- Coord utilities ----------
function clientToSvgPoint(clientX: number, clientY: number) {
	if (!svgElement) return { x: 0, y: 0 };
	const ctm = svgElement.getScreenCTM();
	if (!ctm) return { x: 0, y: 0 };
	try {
		const inv = ctm.inverse();
		const p = new DOMPoint(clientX, clientY).matrixTransform(inv);
		if (!Number.isFinite(p.x) || !Number.isFinite(p.y))
			return { x: 0, y: 0 };
		return { x: p.x, y: p.y }; // USER units
	} catch {
		return { x: 0, y: 0 };
	}
}
function getViewportCenterSvg() {
	// Fallback to viewBox center if layout info is missing
	if (!viewport.value) {
		return {
			x: baseViewBox.x + baseViewBox.w / 2,
			y: baseViewBox.y + baseViewBox.h / 2,
		};
	}
	const rect = viewport.value.getBoundingClientRect();
	return clientToSvgPoint(
		rect.left + rect.width / 2,
		rect.top + rect.height / 2
	);
}
function distanceAndMidpoint(a: PointerEvent, b: PointerEvent) {
	const dx = b.clientX - a.clientX;
	const dy = b.clientY - a.clientY;
	const dist = Math.hypot(dx, dy);
	const mid = {
		x: (a.clientX + b.clientX) / 2,
		y: (a.clientY + b.clientY) / 2,
	};
	return { dist, mid };
}

// ---------- Animation ----------
let animFrame = 0;
let animCancelled = false;
const easeInOutCubic = (t: number) =>
	t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
function cancelAnimation() {
	animCancelled = true;
	cancelAnimationFrame(animFrame);
}
function animateCamera(
	to: { s: number; tx: number; ty: number },
	duration = 600,
	easing = easeInOutCubic
) {
	cancelAnimation();
	animCancelled = false;
	const from = { ...cam };
	const t0 = performance.now();
	const tick = (now: number) => {
		if (animCancelled) return;
		const t = Math.min(1, (now - t0) / duration);
		const k = easing(t);
		cam.s = from.s + (to.s - from.s) * k;
		cam.tx = from.tx + (to.tx - from.tx) * k;
		cam.ty = from.ty + (to.ty - from.ty) * k;
		scheduleRender();
		if (t < 1) animFrame = requestAnimationFrame(tick);
	};
	animFrame = requestAnimationFrame(tick);
}

// ---------- Fit a rect (room) into the viewport, preserving aspect with margin ----------
function fitRectWithMargin(bbox: DOMRect, marginRatio = 0.1) {
	const margin = Math.max(bbox.width, bbox.height) * marginRatio;
	const desiredW = bbox.width + margin * 2;
	const desiredH = bbox.height + margin * 2;

	// To fully show desiredW x desiredH, choose the smaller of width/height scales
	// visibleW = baseViewBox.w / s, visibleH = baseViewBox.h / s  =>  s <= min(baseW/desiredW, baseH/desiredH)
	let sFit = Math.min(baseViewBox.w / desiredW, baseViewBox.h / desiredH);

	// Clamp
	const { sMin, sMax } = computeScaleLimits();
	sFit = Math.max(sMin, Math.min(sFit, sMax));

	// Center the bbox in view (USER units)
	const cx = bbox.x + bbox.width / 2;
	const cy = bbox.y + bbox.height / 2;
	const vx = baseViewBox.x + baseViewBox.w / 2;
	const vy = baseViewBox.y + baseViewBox.h / 2;

	const tx = vx - sFit * cx;
	const ty = vy - sFit * cy;

	zoomInDisabled.value = sFit >= sMax;
	zoomOutDisabled.value = sFit <= sMin;

	return { s: sFit, tx, ty };
}

// ---------- Select & fly-to ----------
const roomsById = new Map<number, SVGAElement>();

function indexRooms() {
	if (!svgElement) return;
	roomsById.clear();
	svgElement.querySelectorAll<SVGAElement>('g[data-iid]').forEach((g) => {
		const [type, idStr] = g.getAttribute('data-iid')?.split('_') ?? [];
		if (type === String(ROOM_TYPE_ID)) {
			g.classList.add('room');
			const id = Number(idStr);
			if (!Number.isNaN(id)) roomsById.set(id, g);
		}
	});
}

function centerRoomElement(g: SVGAElement) {
	if (!svgElement) return;
	const bbox = g.getBBox();
	const target = fitRectWithMargin(bbox, props.roomZoomPadding ?? 0.2); // 20% padding fallback
	animateCamera(target, 600);
}

let selectedEl: SVGAElement | null = null;
function selectRoom(roomId: number | null, centerPosition = true) {
	if (!svgElement) return;

	selectedEl?.classList.remove('selected-room');
	selectedEl = null;

	if (!roomId) return;

	const el = roomsById.get(roomId);
	if (!el) return;

	el.classList.add('selected-room');
	selectedEl = el;
	if (centerPosition) {
		centerRoomElement(el);
	}
}

/**
 * Zoom by a multiplicative factor around an anchor point in USER units.
 * If animate=false it updates immediately (good for wheel).
 */
function zoomBy(
	factor: number,
	anchor?: { x: number; y: number },
	animate = true
) {
	if (!svgElement) return;

	// clamp scale
	const { sMin, sMax } = computeScaleLimits();
	const sNew = Math.max(sMin, Math.min(cam.s * factor, sMax));

	// keep anchor fixed in post-camera space:
	// q = s*p + t  =>  t' = q - (s'/s) * (q - t)
	const ratio = sNew / Math.max(1e-6, cam.s);
	const a = anchor ?? getViewportCenterSvg();
	const tx = a.x - ratio * (a.x - cam.tx);
	const ty = a.y - ratio * (a.y - cam.ty);

	zoomInDisabled.value = sNew >= sMax;
	zoomOutDisabled.value = sNew <= sMin;

	if (animate) {
		animateCamera({ s: sNew, tx, ty }, 200); // short, snappy zoom
	} else {
		cam.s = sNew;
		cam.tx = tx;
		cam.ty = ty;
		scheduleRender();
	}
}

// ---------- Interactions ----------
let isPanning = false;
let startClientX = 0;
let startClientY = 0;
let startTx = 0;
let startTy = 0;

// Pinch zoom
const activePointers = new Map<number, PointerEvent>();
let isPinching = false;
let lastPinchDist = 0; // in CSS px
let lastPinchMidClient = { x: 0, y: 0 }; // in CSS px

const onPointerDown = (e: PointerEvent) => {
	(e.target as Element).setPointerCapture?.(e.pointerId);
	activePointers.set(e.pointerId, e);

	if (activePointers.size === 2) {
		isPinching = true;
		cancelAnimation();
		const [p1, p2] = Array.from(activePointers.values());
		const { dist, mid } = distanceAndMidpoint(p1, p2);
		lastPinchDist = Math.max(1e-6, dist);
		lastPinchMidClient = mid;

		isPanning = false;
		viewport.value?.classList.remove('is-panning');
		e.preventDefault();
		return;
	}

	if (e.button !== 0) return;
	e.preventDefault();
	cancelAnimation();

	isPanning = true;
	startClientX = e.clientX;
	startClientY = e.clientY;
	startTx = cam.tx;
	startTy = cam.ty;

	viewport.value?.classList.add('is-panning');
};

const onPointerMove = (e: PointerEvent) => {
	// keep the latest event for this pointer
	if (activePointers.has(e.pointerId)) {
		activePointers.set(e.pointerId, e);
	}

	// Mobile pinch zoom (two active pointers)
	if (isPinching && activePointers.size >= 2) {
		const [p1, p2] = Array.from(activePointers.values());
		const { dist, mid } = distanceAndMidpoint(p1, p2);

		// 1) Relative scale step since the last frame (stable, frame-to-frame)
		const step = Math.max(1e-6, dist) / Math.max(1e-6, lastPinchDist);

		// 2) Scale about the *current* midpoint in USER/SVG space
		//    (keeps the content under fingers steady while scaling)
		const anchorSvg = clientToSvgPoint(mid.x, mid.y);
		zoomBy(step, anchorSvg, /*animate*/ false);

		// 3) Translate to follow the midpoint movement
		//    Convert client delta -> USER units using current px-per-user-unit
		const dClientX = mid.x - lastPinchMidClient.x;
		const dClientY = mid.y - lastPinchMidClient.y;
		const pxPerUser = Math.max(1e-6, s0() * cam.s); // current composite scale to screen
		cam.tx += dClientX / pxPerUser;
		cam.ty += dClientY / pxPerUser;

		scheduleRender();

		// 4) Update for next frame
		lastPinchDist = Math.max(1e-6, dist);
		lastPinchMidClient = mid;

		// Prevent browser gestures/scroll
		e.preventDefault();
		return;
	}

	// Single-finger/mouse pan
	if (!isPanning || !svgElement) return;

	const dxScreen = e.clientX - startClientX;
	const dyScreen = e.clientY - startClientY;

	const k = Math.max(1e-6, s0());
	cam.tx = startTx + dxScreen / k;
	cam.ty = startTy + dyScreen / k;

	scheduleRender();
};

let lastPointerUpTime = 0;

const onPointerUp = (e: PointerEvent) => {
	activePointers.delete(e.pointerId);

	// If we were pinching and pointers drop below 2, stop pinch
	if (isPinching && activePointers.size < 2) {
		isPinching = false;
	}

	// If exactly 1 pointer remains, dont auto-resume pan, wait for restart
	if (activePointers.size === 0) {
		isPanning = false;

		const moved = Math.hypot(
			e.clientX - startClientX,
			e.clientY - startClientY
		);
		if (moved < 5) {
			const currentTime = Date.now();
			if (currentTime - lastPointerUpTime < DOUBLE_CLICK_THRESHOLD) {
				// Double-click
				zoomBy(ZOOM_STEP, clientToSvgPoint(e.clientX, e.clientY), true);
			} else {
				// Click
				const parent = (e.target as HTMLElement).parentElement;
				if (
					parent?.hasAttribute('data-iid') &&
					parent.classList.contains('room')
				) {
					const iid = parent.getAttribute('data-iid')?.split('_');
					if (iid?.length === 2) {
						const roomId = parseInt(iid[1]);
						if (roomId) emit('room-clicked', roomId);
					}
				}
			}
			lastPointerUpTime = currentTime;
		}

		viewport.value?.classList.remove('is-panning');
	}
};

let startTouchY = 0;

const onTouchStart = (e: TouchEvent) => {
	// record where the gesture began (first finger)
	startTouchY = e.touches[0]?.clientY ?? 0;
};

const onTouchMove = (e: TouchEvent) => {
	// Only guard during map interaction to avoid breaking normal page scroll elsewhere
	const interacting = isPanning || isPinching;

	// If the document is at the top and the user is dragging down (or using two fingers),
	// prevent the browser's pull-to-refresh / rubber-band.
	if (interacting && window.scrollY === 0) {
		const dy = (e.touches[0]?.clientY ?? 0) - startTouchY;
		if (dy > 0 || e.touches.length >= 2) {
			e.preventDefault(); // requires non-passive listener
		}
	}
};

let wheelAccum = 0;
let wheelScheduled = false;
const onWheel = (e: WheelEvent) => {
	if (!svgElement) return;
	e.preventDefault();
	cancelAnimation();

	wheelAccum += e.deltaY;
	// clamp accumulation to avoid overflow -> Infinity
	wheelAccum = Math.max(-2000, Math.min(2000, wheelAccum));
	if (wheelScheduled) return;
	wheelScheduled = true;

	requestAnimationFrame(() => {
		wheelScheduled = false;

		const mouseSvg = clientToSvgPoint(e.clientX, e.clientY); // USER units

		const zoomIntensity = 0.0015;
		let factorExp = Math.exp(-wheelAccum * zoomIntensity); // >1 in, <1 out
		if (!Number.isFinite(factorExp))
			factorExp = wheelAccum < 0 ? 1e6 : 1e-6;
		wheelAccum = 0;

		zoomBy(factorExp, mouseSvg, false);
	});
};

const zoomIn = () => {
	cancelAnimation();
	zoomBy(ZOOM_STEP, undefined, true);
};
const zoomOut = () => {
	cancelAnimation();
	zoomBy(1 / ZOOM_STEP, undefined, true);
};

defineExpose({
	zoomIn,
	zoomOut,
	zoomInDisabled,
	zoomOutDisabled,
});

// ---------- Lifecycle ----------
watch(
	() => props.selectedRoomId,
	(id) => selectRoom(id ?? null)
);

onMounted(() => {
	svgElement = content.value?.querySelector('svg') ?? null;
	if (!svgElement) return;

	// Wait until the SVG has a non-zero size to avoid 0/0 -> NaN/Infinity
	const waitForLayout = () =>
		new Promise<void>((resolve) => {
			const check = () => {
				if (
					svgElement &&
					svgElement.clientWidth > 0 &&
					svgElement.clientHeight > 0
				)
					return resolve();
				requestAnimationFrame(check);
			};
			check();
		});

	(async () => {
		await waitForLayout();
		ensureCameraWrapper();
		indexRooms();

		viewport.value?.addEventListener('wheel', onWheel, { passive: false });
		viewport.value?.addEventListener('pointerdown', onPointerDown);
		window.addEventListener('pointermove', onPointerMove);
		window.addEventListener('pointerup', onPointerUp);
		window.addEventListener('pointercancel', onPointerUp);

		viewport.value?.addEventListener('touchstart', onTouchStart, {
			passive: true,
		});
		viewport.value?.addEventListener('touchmove', onTouchMove, {
			passive: false,
		});

		// Keep math correct on container resizes
		const ro = new ResizeObserver(() => {
			if (!svgElement) return;
			svgW = svgElement.clientWidth;
			svgH = svgElement.clientHeight;
			scheduleRender();
		});
		if (viewport.value) {
			ro.observe(viewport.value);
		}

		if (props.selectedRoomId) {
			selectRoom(props.selectedRoomId, props.startPosition == null);
		}
	})();
});

onBeforeUnmount(() => {
	viewport.value?.removeEventListener('wheel', onWheel);
	viewport.value?.removeEventListener('pointerdown', onPointerDown);
	window.removeEventListener('pointermove', onPointerMove);
	window.removeEventListener('pointerup', onPointerUp);
	window.removeEventListener('pointercancel', onPointerUp);

	viewport.value?.removeEventListener('touchstart', onTouchStart);
	viewport.value?.removeEventListener('touchmove', onTouchMove);
});
</script>

<style scoped lang="scss">
.blueprint-viewport {
	position: relative;
	overflow: hidden;
	user-select: none;
	height: 100%;
	cursor: grab;
	overscroll-behavior: contain; // keep scroll/refresh from escaping this box
	touch-action: none; // better pointer perf

	:deep(svg *) {
		pointer-events: none;
	}
	:deep(.room) {
		pointer-events: all;
		path {
			pointer-events: all;
		}
		cursor: pointer;
		transition: fill 0.3s ease;
		fill: transparent !important;
		opacity: 0.3;

		@media (hover: hover) and (pointer: fine) {
			&:hover {
				fill: rgb(149, 149, 149) !important;
			}
		}
		&.selected-room {
			fill: rgb(73, 73, 255) !important;
		}
	}

	.blueprint-content {
		height: 100%;
	}
	&.is-panning {
		cursor: grabbing;
	}
}

/* speed-biased rendering */
:deep(svg) {
	shape-rendering: optimizeSpeed;
	text-rendering: optimizeSpeed;
}

html,
body {
	height: 100%;
	overscroll-behavior-y: none;
}
</style>
