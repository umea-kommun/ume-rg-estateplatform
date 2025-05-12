import { mount, config } from '@vue/test-utils';
import { describe, expect, test, beforeAll } from 'vitest';
import BaseTextBox from '../BaseTextBox.vue';
import Validation from '@/plugins/validation';
import { I18n } from 'vue-i18n';

config.global.mocks.$t = (phrase: string): string => phrase;
describe('MyComponent', () => {
	beforeAll(() => {
		const i18n = { global: { t: (t: string) => t } };
		Validation(i18n as I18n);
	});

	test('renders without crashing', () => {
		const wrapper = mount(BaseTextBox, {
			props: {
				id: 'test-id',
				modelValue: '',
				prefix: 'prefix',
			},
		});
		expect(wrapper.exists()).toBeTruthy();
	});

	test('renders text field when textArea prop is false', () => {
		const wrapper = mount(BaseTextBox, {
			props: {
				id: 'test-id',
				modelValue: '',
				textArea: false,
			},
		});
		expect(wrapper.find('input').exists()).toBeTruthy();
		expect(wrapper.find('textarea').exists()).toBeFalsy();
	});

	test('renders textarea when textArea prop is true', () => {
		const wrapper = mount(BaseTextBox, {
			props: {
				id: 'test-id',
				modelValue: '',
				textArea: true,
			},
		});
		expect(wrapper.find('textarea').exists()).toBeTruthy();
		expect(wrapper.find('textfield').exists()).toBeFalsy();
	});

	test('updates modelValue when input event is emitted', async () => {
		const wrapper = mount(BaseTextBox, {
			props: {
				id: 'test-id',
				modelValue: 'Ett testvärde',
			},
		});

		wrapper.find('input').setValue('new value');
		const emitted = wrapper.emitted('update:modelValue');
		expect(emitted).toBeTruthy();
		expect(emitted?.[0]).toEqual(['new value']);
	});

	test('required rule works', async () => {
		const wrapper = mount(BaseTextBox, {
			props: {
				id: 'test-id',
				modelValue: '',
				label: 'label',
				rules: 'required',
			},
		});

		wrapper.find('input').trigger('blur');

		// Wait for validation to complete
		await new Promise((r) => setTimeout(r, 50));

		// Error message should be displayed
		expect(wrapper.find('#error-test-id').text()).toBe(
			'app.validation.messages.required'
		);
	});
});
