import { mount, config } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import BaseFormField from '../BaseFormField.vue';

config.global.mocks.$t = (phrase: string): string => phrase;

describe('BaseFormField', () => {
	test('Renders title', async () => {
		const wrapper = mount(BaseFormField, {
			props: {
				label: 'hello',
				labelFor: 'test',
			},
			slots: {
				default: '<input id="test" />',
			},
		});

		expect(wrapper.text()).toBe('hello');
	});

	test('Renders title and asterisk if required field', async () => {
		const wrapper = mount(BaseFormField, {
			props: {
				label: 'hello',
				labelFor: 'test',
				isRequired: true,
			},
			slots: {
				default: '<input id="test" />',
			},
		});

		expect(wrapper.text()).toBe('hello *');
	});
});
