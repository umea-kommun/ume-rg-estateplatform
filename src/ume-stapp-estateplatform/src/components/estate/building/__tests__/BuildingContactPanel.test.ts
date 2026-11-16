import { mount } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import BuildingContactPanel from '../BuildingContactPanel.vue';
import { IBuildingDetails } from '@/models/Interfaces';
import { ExternalOwnerStatus } from '@/models/Enums';
import sv from '@/locales/sv.json';

const mountPanel = (
	contactPersons: Record<string, unknown>,
	externalOwnerStatus: ExternalOwnerStatus | null = null
) =>
	mount(BuildingContactPanel, {
		props: {
			building: {
				contactPersons,
				externalOwnerInfo: { status: externalOwnerStatus },
			} as unknown as IBuildingDetails,
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

	test('rented building lists two caretakers instead of coordinator and technician', () => {
		const wrapper = mountPanel(
			{
				propertyManager: { name: 'Anna Johansson' },
				operationCoordinator: { name: 'Ska inte visas' },
				operationsTechnician: { name: 'Ska inte heller visas' },
				caretaker: { name: 'Erik Lindqvist' },
				secondCaretaker: { name: 'Anders Lindberg' },
			},
			ExternalOwnerStatus.Inhyrd
		);

		const roles = wrapper.findAll('.role').map((role) => role.text());
		const labels = sv.component.buildingContact;
		expect(roles).toEqual([
			labels.propertyManager,
			labels.operationsManager,
			labels.rentalAdministrator,
			labels.caretaker,
			labels.caretaker,
		]);
		expect(wrapper.text()).toContain('Anders Lindberg');
		expect(wrapper.text()).not.toContain('Ska inte');
	});
});
