import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import EstateDetailsBuildings from '../EstateDetailsBuildings.vue';
import { IEstateBuilding } from '@/models/Interfaces';
import { BuildingSortOption } from '@/models/Enums';
import sv from '@/locales/sv.json';

const STORAGE_KEY = 'estate-buildings-sort';

const building = (
	name: string,
	grossArea: number,
	street: string | null = null
): IEstateBuilding =>
	({
		id: name.length + grossArea,
		name,
		popularName: null,
		grossArea,
		metrics: { floorCount: null, roomCount: null },
		address: street ? { street, zipCode: '', city: '' } : null,
	}) as IEstateBuilding;

const buildings = [
	building('Björken', 500, 'Storgatan 2'),
	building('Almen', 1500, null),
	building('Cedern', 100, 'Aspvägen 7'),
];

const MenuStub = {
	template: '<div><slot name="activator" :props="{}" /><slot /></div>',
};

const ListItemStub = {
	props: ['title', 'value'],
	template: `<li
			class="sort-option"
			:data-value="value"
			@click="$emit('click')"
		>{{ title }}</li>`,
};

const mountBuildings = () =>
	mount(EstateDetailsBuildings, {
		props: { loading: false, failed: false, buildings },
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
			stubs: {
				AppLoadingSpinner: true,
				FavoriteButton: true,
				'v-menu': MenuStub,
				'v-list-item': ListItemStub,
			},
		},
	});

const renderedNames = (wrapper: ReturnType<typeof mountBuildings>) =>
	wrapper.findAll('.building-name').map((node) => node.text());

const selectSort = (
	wrapper: ReturnType<typeof mountBuildings>,
	option: BuildingSortOption
) => wrapper.find(`.sort-option[data-value="${option}"]`).trigger('click');

describe('EstateDetailsBuildings sorting', () => {
	beforeEach(() => {
		localStorage.clear();
	});

	test('Defaults to the largest building first when nothing is saved', () => {
		expect(renderedNames(mountBuildings())).toEqual([
			'Almen',
			'Björken',
			'Cedern',
		]);
	});

	test('Restores the sort choice saved in the browser', () => {
		localStorage.setItem(STORAGE_KEY, BuildingSortOption.AreaAsc);

		expect(renderedNames(mountBuildings())).toEqual([
			'Cedern',
			'Björken',
			'Almen',
		]);
	});

	test('Falls back to the default when the saved sort choice is unknown', () => {
		localStorage.setItem(STORAGE_KEY, 'no-longer-supported');

		expect(renderedNames(mountBuildings())).toEqual([
			'Almen',
			'Björken',
			'Cedern',
		]);
	});

	test('Sorts buildings without an address last', async () => {
		const wrapper = mountBuildings();

		await selectSort(wrapper, BuildingSortOption.Address);

		expect(renderedNames(wrapper)).toEqual(['Cedern', 'Björken', 'Almen']);
	});

	test('Saves the selected sort option in the browser', async () => {
		const wrapper = mountBuildings();

		await selectSort(wrapper, BuildingSortOption.Name);

		expect(localStorage.getItem(STORAGE_KEY)).toBe(BuildingSortOption.Name);
		expect(renderedNames(wrapper)).toEqual(['Almen', 'Björken', 'Cedern']);
	});
});
