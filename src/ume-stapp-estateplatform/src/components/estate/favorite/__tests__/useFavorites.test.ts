import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, test, vi } from 'vitest';
import { createI18n } from 'vue-i18n';
import { Component } from 'vue';
import { EstateType } from '@/models/Enums';
import { IEstateSearchResultEntry } from '@/models/Interfaces';
import sv from '@/locales/sv.json';

const dispatch = vi.fn();

vi.mock('@/store/store', () => ({
	default: {
		state: {},
		commit: vi.fn(),
		dispatch: (...args: unknown[]) => dispatch(...args),
	},
}));

const buildingEntry = {
	id: 1,
	type: EstateType.Building,
	name: 'Hus A',
	popularName: null,
	municipalityArea: null,
	imageUrl: null,
	isFavorite: true,
	address: null,
	metrics: {},
	ancestors: [],
} as unknown as IEstateSearchResultEntry;

const mountOptions = {
	global: {
		plugins: [
			createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
		],
		stubs: {
			EstateSearchResultItem: { template: '<div class="entry" />' },
			'v-skeleton-loader': true,
		},
	},
};

const mountButton = async (component: Component, isFavorite: boolean) =>
	mount(component, {
		...mountOptions,
		props: { id: 1, type: EstateType.Building, isFavorite },
	});

const loadModules = async () => {
	vi.resetModules();
	return {
		FavoriteButton: (await import('../FavoriteButton.vue')).default,
		FavoriteList: (await import('../FavoriteList.vue')).default,
	};
};

const isStarred = (wrapper: { html: () => string }) =>
	wrapper.html().includes('star_border') === false;

describe('favorites', () => {
	beforeEach(() => {
		dispatch.mockReset();
		dispatch.mockImplementation(async (type: string) =>
			type === 'getFavorites' ? [] : undefined
		);
	});

	test('Toggling one star updates every other star for the same node', async () => {
		const { FavoriteButton } = await loadModules();

		const mapCardStar = await mountButton(FavoriteButton, false);
		const searchHitStar = await mountButton(FavoriteButton, false);

		await mapCardStar.find('button').trigger('click');
		await flushPromises();

		expect(dispatch).toHaveBeenCalledWith('setFavorite', {
			id: 1,
			type: EstateType.Building,
		});
		expect(isStarred(mapCardStar)).toBe(true);
		expect(isStarred(searchHitStar)).toBe(true);
	});

	test('Favoriting from elsewhere adds the node to a mounted list', async () => {
		const { FavoriteButton, FavoriteList } = await loadModules();

		const list = mount(FavoriteList, mountOptions);
		await flushPromises();
		expect(list.findAll('.entry')).toHaveLength(0);

		dispatch.mockImplementation(async (type: string) =>
			type === 'getFavorites' ? [buildingEntry] : undefined
		);

		const star = await mountButton(FavoriteButton, false);
		await star.find('button').trigger('click');
		await flushPromises();

		expect(list.findAll('.entry')).toHaveLength(1);
		expect(list.text()).toContain('Mina favoriter (1)');
	});

	test('A failed write rolls the star back', async () => {
		const { FavoriteButton } = await loadModules();

		dispatch.mockImplementation(async (type: string) => {
			if (type === 'setFavorite') throw new Error('boom');
			return [];
		});

		const star = await mountButton(FavoriteButton, false);
		await star.find('button').trigger('click');
		await flushPromises();

		expect(isStarred(star)).toBe(false);
	});
});
