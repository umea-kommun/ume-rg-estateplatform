import { describe, it, expect } from 'vitest';
import { createPlanImage } from '../blueprintSvg';

describe('createPlanImage', () => {
	it('lays the plan out 100x larger and scales it back to the viewBox', () => {
		const image = createPlanImage('blob:plan', {
			x: -13,
			y: -67,
			w: 131,
			h: 31,
		});

		expect(image.getAttribute('href')).toBe('blob:plan');
		expect(image.getAttribute('x')).toBe('-1300');
		expect(image.getAttribute('y')).toBe('-6700');
		expect(image.getAttribute('width')).toBe('13100');
		expect(image.getAttribute('height')).toBe('3100');
		expect(image.getAttribute('transform')).toBe('scale(0.01)');
	});
});
