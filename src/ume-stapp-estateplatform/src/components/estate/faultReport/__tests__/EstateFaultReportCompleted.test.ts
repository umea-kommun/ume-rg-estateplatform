import { mount } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import EstateFaultReportCompleted from '../EstateFaultReportCompleted.vue';
import { MyErrandsRoute } from '@/router/routes';
import sv from '@/locales/sv.json';

const BtnStub = {
	name: 'VBtn',
	props: { to: Object },
	template: '<button><slot /></button>',
};

describe('EstateFaultReportCompleted', () => {
	test('points the user to Mina ärenden on the start page', () => {
		const wrapper = mount(EstateFaultReportCompleted, {
			global: {
				plugins: [
					createI18n({
						legacy: false,
						locale: 'sv',
						messages: { sv },
					}),
				],
				stubs: {
					'v-btn': BtnStub,
					BaseAnimatedSuccess: true,
					RateFeedback: true,
				},
			},
		});

		expect(wrapper.text()).toContain(
			'Du kan följa ditt ärende under Mina ärenden på startsidan.'
		);
		const button = wrapper.findComponent(BtnStub);
		expect(button.text()).toBe('Gå till Mina ärenden');
		expect(button.props('to')).toEqual(MyErrandsRoute);
	});
});
