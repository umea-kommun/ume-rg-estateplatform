import { mount, config } from '@vue/test-utils';
import { describe, expect, test, beforeAll, vi } from 'vitest';

// Mock window.matchMedia used by TinyMCE (so we mock it before we import tinymce)
Object.defineProperty(window, 'matchMedia', {
	writable: true,
	value: vi.fn().mockImplementation((query) => ({
		matches: false,
		media: query,
		onchange: null,
		addListener: vi.fn(), // Deprecated
		removeListener: vi.fn(), // Deprecated
		addEventListener: vi.fn(),
		removeEventListener: vi.fn(),
		dispatchEvent: vi.fn(),
	})),
});

import BaseHtmlEditor from '../BaseHtmlEditor.vue';
import Validation from '@/plugins/validation';
import { I18n } from 'vue-i18n';

config.global.mocks.$t = (phrase: string): string => phrase;
describe('BaseHtmlEditor', () => {
	beforeAll(() => {
		const i18n = { global: { t: (t: string) => t } };
		Validation(i18n as I18n);
	});

	test('renders correct label', async () => {
		const wrapper = mount(BaseHtmlEditor, {
			props: {
				id: 'test-2',
				modelValue: 'value',
				label: 'min titel',
			},
		});

		expect(wrapper.find('label').text()).toEqual('min titel');
	});

	test('required rule works', async () => {
		const wrapper = mount(BaseHtmlEditor, {
			props: {
				id: 'test-2',
				modelValue: '',
				label: 'min titel',
				rules: 'required',
			},
		});

		wrapper.find('textarea').trigger('blur');

		// Wait for validation to complete
		await new Promise((r) => setTimeout(r, 50));

		// Error message should be displayed
		expect(wrapper.find('#error-test-2').text()).toBe(
			'app.validation.messages.required'
		);
	});
});
