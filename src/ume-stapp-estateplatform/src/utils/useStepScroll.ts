import { nextTick, onScopeDispose, type Ref } from 'vue';
import type EstateOrderStep from '@/components/estate/order/EstateOrderStep.vue';

// The sticky app header ($site-header-height: 5rem) covers the top 80px of the
// viewport, so treat that as the effective top edge.
const HEADER_HEIGHT = 80;
// A small gap so the step doesn't rest flush against the header.
const TOP_MARGIN = 16;
// How much of a step we aim to have on screen: the whole thing when it fits,
// otherwise at least this much (title + the first bit of content) for tall steps.
const MIN_VISIBLE = 250;

type StepRef = Ref<InstanceType<typeof EstateOrderStep> | null>;

// Give up waiting for layout to settle after this long. Scrolling is a bonus, so
// a page that never stops moving (unexpected) is left where it is rather than
// scrolled to a moving target or hanging.
const SETTLE_TIMEOUT = 700;

/**
 * Scroll helper for the step-based wizards (fault report, order, ...).
 *
 * Scrolls a step into view after the UI has settled, but only when too little of
 * it is showing. A step that fits below the header is brought fully into view; a
 * taller step gets at least its title plus MIN_VISIBLE px of content on screen.
 * Either way it rests just below the header rather than being centered.
 */
export const useStepScroll = () => {
	// The settle loop below runs across several frames; if the owning component
	// unmounts mid-settle (e.g. navigating away within SETTLE_TIMEOUT), the target
	// element detaches and its all-zero rect reads as "stable", which would then
	// scroll the next route. Cancel the loop and stop scrolling on disposal.
	let disposed = false;
	let rafId: number | null = null;

	onScopeDispose(() => {
		disposed = true;
		if (rafId !== null) {
			cancelAnimationFrame(rafId);
			rafId = null;
		}
	});

	/**
	 * Resolves `true` once the element's position stops moving between frames, or
	 * `false` if it never settles within the timeout (or the scope is disposed).
	 *
	 * Selecting a step collapses the previous one with a height animation, so the
	 * target step keeps sliding up for a few hundred ms. Measuring/scrolling before
	 * that finishes lands at a stale position and fights the animation, so we wait
	 * for the layout to come to rest first. When nothing is animating this resolves
	 * within a frame or two.
	 */
	const waitForStableLayout = (el: HTMLElement) =>
		new Promise<boolean>((resolve) => {
			const start = performance.now();
			let lastTop: number | null = null;
			let stableFrames = 0;

			const tick = () => {
				// Bail if the component unmounted mid-settle: the element is now
				// detached and would measure as a stable all-zero rect.
				if (disposed) {
					resolve(false);
					return;
				}

				const top = el.getBoundingClientRect().top + window.scrollY;
				if (lastTop !== null && Math.abs(top - lastTop) < 0.5) {
					stableFrames++;
				} else {
					stableFrames = 0;
				}
				lastTop = top;

				if (stableFrames >= 2) {
					resolve(true);
					return;
				}
				if (performance.now() - start > SETTLE_TIMEOUT) {
					resolve(false);
					return;
				}
				rafId = requestAnimationFrame(tick);
			};

			rafId = requestAnimationFrame(tick);
		});

	const scrollToStep = async (stepRef: StepRef) => {
		await nextTick();

		const step = stepRef.value?.root;
		if (!step) {
			return;
		}

		// Wait for the collapse animation above to finish so we measure and scroll
		// against the final layout instead of racing it. Scrolling is a nicety, not
		// essential - if the layout never settles, skip it rather than scroll to a
		// moving target.
		const settled = await waitForStableLayout(step);
		if (!settled) {
			return;
		}

		const rect = step.getBoundingClientRect();
		// Space below the header the step can actually occupy once rested.
		const usableHeight = window.innerHeight - HEADER_HEIGHT - TOP_MARGIN;

		// Show the whole step when it fits, otherwise the first MIN_VISIBLE px
		// (capped by what fits on very small screens).
		const targetVisible = Math.min(rect.height, MIN_VISIBLE, usableHeight);
		// How much of the step is currently on screen below the header.
		const visibleHeight =
			Math.min(rect.bottom, window.innerHeight) -
			Math.max(rect.top, HEADER_HEIGHT);

		// Visible enough only if the title clears the header and enough of the
		// step shows.
		if (rect.top >= HEADER_HEIGHT && visibleHeight >= targetVisible) {
			return;
		}

		// Rest the step's top just below the header, keeping content on screen.
		const delta = rect.top - (HEADER_HEIGHT + TOP_MARGIN);
		window.scrollBy({ top: delta, behavior: 'smooth' });
	};

	return { scrollToStep };
};
