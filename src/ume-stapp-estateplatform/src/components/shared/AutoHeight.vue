<template>
	<div ref="outer" class="auto-height">
		<div ref="inner">
			<slot />
		</div>
	</div>
</template>

<script setup lang="ts">
/**
 * Smoothly animates its own height when its slotted content shrinks (e.g. a step
 * collapsing to a summary card), so neighbouring content flows up instead of
 * jumping. Content is swapped instantly as usual; this wrapper just tweens the
 * height down between the old and new size. Growth is left instant.
 *
 * Honours `prefers-reduced-motion` (no animation) and never animates the first
 * measurement, so nothing animates on initial render. No `overflow` is toggled,
 * so margin-collapsing stays identical during and after the tween - otherwise the
 * height would settle a few pixels off and jump at the end.
 */
import { onBeforeUnmount, onMounted, useTemplateRef } from 'vue';

const props = withDefaults(defineProps<{ duration?: number }>(), {
	duration: 200,
});

// Width changes up to this are ignored (a page scrollbar appearing/disappearing
// as our own height change toggles page overflow is ~17px wide). Anything larger
// is a genuine reflow (window resize / rewrap) and skips the animation.
const SCROLLBAR_TOLERANCE = 25;

const outer = useTemplateRef<HTMLElement>('outer');
const inner = useTemplateRef<HTMLElement>('inner');

let observer: ResizeObserver | null = null;
let lastHeight: number | null = null;
let lastWidth: number | null = null;
let animation: Animation | null = null;

const prefersReducedMotion = () =>
	window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

const onResize = () => {
	const el = outer.value;
	const content = inner.value;
	if (!el || !content) return;

	const newHeight = content.offsetHeight;
	const newWidth = content.offsetWidth;

	// First measurement: record the baseline and leave the height as `auto`.
	if (lastHeight === null) {
		lastHeight = newHeight;
		lastWidth = newWidth;
		return;
	}

	// A large width change means the layout genuinely reflowed (window resize /
	// rewrap), not a content swap - snap to the new height instead of tweening it.
	// A small change (a scrollbar gutter toggling as our height change adds/removes
	// page overflow) must NOT block the height animation.
	if (Math.abs(newWidth - (lastWidth ?? newWidth)) > SCROLLBAR_TOLERANCE) {
		animation?.cancel();
		lastHeight = newHeight;
		lastWidth = newWidth;
		return;
	}
	if (newHeight === lastHeight) return;

	// Only animate collapses; let growing content appear instantly.
	const shrinking = newHeight < lastHeight;

	// Start from the current on-screen height (mid-animation this is the tweened
	// value, so interrupted animations continue smoothly rather than snapping).
	const from =
		animation?.playState === 'running'
			? el.getBoundingClientRect().height
			: lastHeight;
	lastHeight = newHeight;
	lastWidth = newWidth;

	if (!shrinking || prefersReducedMotion()) {
		animation?.cancel();
		return;
	}

	animation?.cancel();
	animation = el.animate(
		[{ height: `${from}px` }, { height: `${newHeight}px` }],
		{ duration: props.duration, easing: 'cubic-bezier(0.4, 0, 0.2, 1)' }
	);
};

onMounted(() => {
	if (!inner.value) return;
	observer = new ResizeObserver(onResize);
	observer.observe(inner.value);
});

onBeforeUnmount(() => {
	observer?.disconnect();
	animation?.cancel();
});
</script>
