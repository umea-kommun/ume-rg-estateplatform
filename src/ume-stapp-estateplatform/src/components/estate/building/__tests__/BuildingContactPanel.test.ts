import { mount } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import BuildingContactPanel from '../BuildingContactPanel.vue';
import { IBuildingDetails } from '@/models/Interfaces';
import sv from '@/locales/sv.json';

const mountPanel = (contactPersons: Record<string, unknown>) =>
	mount(BuildingContactPanel, {
		props: {
			building: { contactPersons } as unknown as IBuildingDetails,
		},
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
		},
	});

describe('BuildingContactPanel', () => {
	test('lists every role, showing the unassigned ones as missing', () => {
		const wrapper = mountPanel({
			propertyManager: { name: 'Anna Johansson' },
			operationsManager: { name: '  ' },
			caretaker: null,
		});

		expect(wrapper.findAll('.person')).toHaveLength(6);
		expect(wrapper.findAll('.missing')).toHaveLength(5);
		expect(wrapper.text()).toContain(
			sv.component.buildingContact.valueMissing
		);
	});

	test('renders email and phone as separate rows, omitting the ones missing', () => {
		const wrapper = mountPanel({
			propertyManager: {
				name: 'Anna Johansson',
				email: 'anna@umea.se',
			},
			caretaker: {
				name: 'Erik Lindqvist',
				email: 'erik@umea.se',
				phone: '090-16 10 00',
			},
		});

		const contactRowsFor = (name: string) =>
			wrapper
				.findAll('.person')
				.filter((person) => person.text().includes(name))
				.flatMap((person) => person.findAll('.contact'));

		expect(contactRowsFor('Anna Johansson')).toHaveLength(1);
		expect(contactRowsFor('Erik Lindqvist')).toHaveLength(2);
	});
});
