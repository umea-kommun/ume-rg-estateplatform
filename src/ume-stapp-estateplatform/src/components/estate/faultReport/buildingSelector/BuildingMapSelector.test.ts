import { mount } from '@vue/test-utils';
import { describe, expect, test, vi } from 'vitest';
import { defineComponent, nextTick, ref } from 'vue';
import { createI18n } from 'vue-i18n';
import { IMapPoint } from '@/models/Interfaces';
import { EstateType } from '@/models/Enums';
import BuildingMapSelector from './BuildingMapSelector.vue';
import sv from '@/locales/sv.json';

vi.mock('@/plugins/appInsights', () => ({ appInsights: null }));
vi.mock('vuex', async (importOriginal) => ({
	...(await importOriginal<typeof import('vuex')>()),
	useStore: () => ({ dispatch: vi.fn() }),
}));
vi.mock('../../search/useEstateSearch', () => ({
	useEstateSearch: () => ({
		fetchSearchResults: vi.fn(),
		buildingPoints: ref<IMapPoint[]>([
			{ id: 1, type: EstateType.Building, lon: 20.25, lat: 63.83 },
			{ id: 2, type: EstateType.Building, lon: 20.26, lat: 63.84 },
		]),
	}),
}));

const MapStub = defineComponent({
	props: ['points'],
	setup(_, { expose }) {
		expose({ open: () => undefined });
	},
	template: '<div><slot name="overlay" /></div>',
});

const TextFieldStub = defineComponent({
	props: ['modelValue'],
	emits: ['update:modelValue'],
	template:
		'<input :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" />',
});

describe('BuildingMapSelector search', () => {
	test('keeps the map search and the form search in sync while filtering points', async () => {
		const search = ref('Stadshuset');
		const resultIds = ref<number[] | null>([1]);
		const Host = defineComponent({
			components: { BuildingMapSelector },
			setup: () => ({ search, resultIds }),
			template:
				'<BuildingMapSelector v-model:search="search" :search-result-ids="resultIds" />',
		});
		const wrapper = mount(Host, {
			global: {
				plugins: [
					createI18n({
						legacy: false,
						locale: 'sv',
						messages: { sv },
					}),
				],
				stubs: {
					BuildingMap: MapStub,
					'v-text-field': TextFieldStub,
					'v-btn': { template: '<button><slot /></button>' },
				},
			},
		});
		await wrapper.find('button').trigger('click');
		await nextTick();

		expect(wrapper.find('input').element.value).toBe('Stadshuset');
		expect(
			wrapper
				.findComponent(MapStub)
				.props('points')
				.map((p: IMapPoint) => p.id)
		).toEqual([1]);

		await wrapper.find('input').setValue('Skola');
		expect(search.value).toBe('Skola');
		resultIds.value = [2];
		await nextTick();
		expect(
			wrapper
				.findComponent(MapStub)
				.props('points')
				.map((p: IMapPoint) => p.id)
		).toEqual([2]);

		resultIds.value = [];
		await nextTick();
		expect(wrapper.findComponent(MapStub).props('points')).toEqual([]);

		await wrapper
			.findComponent(TextFieldStub)
			.vm.$emit('update:modelValue', null);
		expect(search.value).toBe('');
		resultIds.value = null;
		await nextTick();
		expect(
			wrapper
				.findComponent(MapStub)
				.props('points')
				.map((p: IMapPoint) => p.id)
		).toEqual([1, 2]);

		search.value = 'Bibliotek';
		await nextTick();
		expect(wrapper.find('input').element.value).toBe('Bibliotek');
		wrapper.unmount();
	});
});
