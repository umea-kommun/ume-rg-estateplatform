import { mount, config } from '@vue/test-utils';
import { describe, expect, test, beforeAll } from 'vitest';
import BaseSelectList from '@/components/base/BaseSelectList.vue';
import Validation from '@/plugins/validation';
import { I18n } from 'vue-i18n';

config.global.mocks.$t = (phrase: string): string => phrase;
describe('BaseSelectList', () => {
	beforeAll(() => {
		const i18n = { global: { t: (t: string) => t } };

		Validation(i18n as I18n);
	});
	test('should be less than ten', async () => {
		const items = [];
		for (let i = 0; i < 3; i++) {
			items.push({ id: i, title: 'Option ' + i, value: 'option' + i });
		}
		const wrapper = mount(BaseSelectList, {
			props: {
				id: 'my-select',
				items: items,
				label: 'Select an option',
			},
		});

		expect(wrapper.find('select').exists()).toBe(true);
		expect(wrapper.find('datalist').exists()).toBe(false);
	});
	test('should be more than ten', async () => {
		const items = [];
		for (let i = 0; i < 12; i++) {
			items.push({ id: i, title: 'Option ' + i, value: 'option' + i });
		}
		const wrapper = mount(BaseSelectList, {
			props: {
				id: 'my-select',
				items: items,
				label: 'Select an option',
			},
		});

		expect(wrapper.find('select').exists()).toBe(false);
		expect(wrapper.find('datalist').exists()).toBe(true);
	});
	test('should be required', async () => {
		const items = [];
		for (let i = 0; i < 2; i++) {
			items.push({ id: i, title: 'Option ' + i, value: 'option' + i });
		}
		const wrapper = mount(BaseSelectList, {
			props: {
				id: 'my-select',
				items: items,
				label: 'Select an option',
				rules: 'required',
			},
		});

		wrapper.find('select').trigger('blur');

		// Wait for validation to complete
		await new Promise((r) => setTimeout(r, 50));

		// Error message should be displayed
		expect(wrapper.find('#error-my-select').text()).toBe(
			'app.validation.messages.required'
		);
		expect(wrapper.find('select').exists()).toBe(true);
		expect(wrapper.find('datalist').exists()).toBe(false);
	});
});
