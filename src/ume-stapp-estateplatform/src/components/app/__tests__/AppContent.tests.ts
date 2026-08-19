// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/components/app/__tests__/AppContent.tests.ts @ 84b4a5dc
import { mount } from '@vue/test-utils';
import { describe, expect, test, beforeAll } from 'vitest';
import AppContent from '../AppContent.vue';
import { createI18n } from 'vue-i18n';

describe('AppContent', () => {
	let i18n: any;

	beforeAll(() => {
		i18n = createI18n({
			messages: {
				gb: {},
				'en-US': {
					'app.title': 'app.title',
				},
			},
		});
	});

	test('Renders slot', () => {
		const wrapper = mount(AppContent, {
			global: {
				plugins: [i18n],
			},
			slots: {
				default: '<span>slot content</span>',
			},
		});

		expect(wrapper.text()).toContain('slot content');
	});

	test('Renders loading spinner', () => {
		const wrapper = mount(AppContent, {
			props: {
				isLoading: true,
			},
			global: {
				plugins: [i18n],
			},
			slots: {
				default: '<span>slot content</span>',
			},
		});

		expect(wrapper.find('.app-loading-spinner').exists()).toBe(true);
	});
});
