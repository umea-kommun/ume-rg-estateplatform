import { mount, config } from '@vue/test-utils';
import { describe, expect, test, beforeAll } from 'vitest';
import BaseDatePicker from '../BaseDatePicker.vue';
import Validation from '@/plugins/validation';
import i18nInstance from '@/plugins/i18next';

config.global.mocks.$t = (phrase: string): string => phrase;
describe('BaseDatePicker.vue', () => {
	beforeAll(() => {
		const i18n = { global: { t: (t: string) => t } };
		Validation(i18n as typeof i18nInstance);
	});

	test('renders correctly', () => {
		const wrapper = mount(BaseDatePicker, {
			props: {
				id: 'testId',
				modelValue: '2023-05-15',
				label: 'Test Label',
				helpText: 'Test Help Text',
				minDate: '2023-01-01',
				maxDate: '2023-12-31',
			},
		});

		const input = wrapper.find('input');
		expect(input.element.value).toBe('2023-05-15');

		const label = wrapper.find('label');
		expect(label.text()).toBe('Test Label');
	});

	test('required rule works', async () => {
		const wrapper = mount(BaseDatePicker, {
			props: {
				id: 'testId2',
				modelValue: '',
				label: 'Test Label',
				helpText: 'Test Help Text',
				minDate: '2023-01-01',
				maxDate: '2023-12-31',
				rules: 'required',
			},
		});

		wrapper.find('input').trigger('blur');

		// Wait for validation to complete
		await new Promise((r) => setTimeout(r, 50));

		// Error message should be displayed
		expect(wrapper.find('#error-testId2').text()).toBe(
			'app.validation.messages.required'
		);
	});
});
