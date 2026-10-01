import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, test, vi } from 'vitest';
import { defineComponent } from 'vue';
import { createI18n } from 'vue-i18n';
import { createStore } from 'vuex';
import EstateSearchFilter from '../EstateSearchFilter.vue';
import { EstateType } from '@/models/Enums';
import { SearchFilter } from '@/models/Interfaces';
import { SEARCHABLE_TYPES } from '../useEstateSearch';
import sv from '@/locales/sv.json';

const BusinessTypeStub = defineComponent({
	props: { modelValue: { type: Array, default: () => [] } },
	emits: ['update:modelValue'],
	template: '<div />',
});

const TypeSelectStub = defineComponent({
	props: { modelValue: { type: String, default: null } },
	emits: ['update:modelValue'],
	template: '<div />',
});

const ChipStub = defineComponent({
	emits: ['click:close'],
	template:
		'<span class="filter-tag" @click="$emit(\'click:close\')"><slot /></span>',
});

const BUSINESS_TYPES = [
	{ id: 5, name: 'Skola' },
	{ id: 7, name: 'Förskola' },
];

const mountFilter = (modelValue: SearchFilter, expanded = false) =>
	mount(EstateSearchFilter, {
		props: { modelValue, expanded },
		global: {
			plugins: [
				createStore({
					actions: {
						getBusinessTypes: vi
							.fn()
							.mockResolvedValue([...BUSINESS_TYPES]),
					},
				}),
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
			stubs: {
				'v-autocomplete': BusinessTypeStub,
				'v-select': TypeSelectStub,
				'v-chip': ChipStub,
				'v-btn': { template: '<button><slot /></button>' },
				'v-expand-transition': { template: '<div><slot /></div>' },
			},
		},
	});

const lastEmit = (wrapper: ReturnType<typeof mountFilter>) =>
	wrapper.emitted('update:modelValue')?.at(-1)?.[0];

const emitBusinessTypes = async (
	wrapper: ReturnType<typeof mountFilter>,
	ids: number[]
) => {
	wrapper.findComponent(BusinessTypeStub).vm.$emit('update:modelValue', ids);
	await wrapper.vm.$nextTick();
	return lastEmit(wrapper);
};

const tagTexts = (wrapper: ReturnType<typeof mountFilter>) =>
	wrapper.findAll('.filter-tag').map((tag) => tag.text());

describe('EstateSearchFilter', () => {
	test('Selecting verksamhetstyp leaves the type scope intact', async () => {
		const wrapper = mountFilter({ types: [EstateType.Building] });

		expect(await emitBusinessTypes(wrapper, [5])).toEqual({
			types: [EstateType.Building],
			businessTypes: [5],
		});
	});

	test('Clearing verksamhetstyp drops the key instead of leaving an empty array', async () => {
		const wrapper = mountFilter({
			types: [EstateType.Building],
			businessTypes: [5],
		});

		expect(await emitBusinessTypes(wrapper, [])).toEqual({
			types: [EstateType.Building],
		});
	});

	test('Building scope is preselected in the type select', () => {
		const wrapper = mountFilter({ types: [EstateType.Building] });

		expect(wrapper.findComponent(TypeSelectStub).props('modelValue')).toBe(
			EstateType.Building
		);
	});

	test('Clearing the type select widens to every searchable type, never to all types', async () => {
		const wrapper = mountFilter({ types: [EstateType.Building] });

		wrapper
			.findComponent(TypeSelectStub)
			.vm.$emit('update:modelValue', null);
		await wrapper.vm.$nextTick();

		expect(lastEmit(wrapper)).toMatchObject({ types: SEARCHABLE_TYPES });
	});

	test('Picking a type narrows the scope to only that type', async () => {
		const wrapper = mountFilter({ types: [EstateType.Building] });

		wrapper
			.findComponent(TypeSelectStub)
			.vm.$emit('update:modelValue', EstateType.Estate);
		await wrapper.vm.$nextTick();

		expect(lastEmit(wrapper)).toMatchObject({ types: [EstateType.Estate] });
	});

	test('Filter fields stay hidden until the panel is expanded', () => {
		const wrapper = mountFilter({ types: [EstateType.Building] });

		expect(wrapper.find('.filter-fields').attributes('style')).toContain(
			'display: none'
		);
	});

	test('Filter fields appear when the panel is expanded', () => {
		const wrapper = mountFilter({ types: [EstateType.Building] }, true);

		expect(
			wrapper.find('.filter-fields').attributes('style') ?? ''
		).not.toContain('display: none');
	});

	test('Default building scope shows as a tag while the panel is collapsed', () => {
		const wrapper = mountFilter({ types: [EstateType.Building] });

		expect(tagTexts(wrapper)).toEqual([
			sv.component.estateSearchFilter.typeTag.building,
		]);
	});

	test('A full type scope narrows nothing and carries no tag', () => {
		const wrapper = mountFilter({ types: [...SEARCHABLE_TYPES] });

		expect(tagTexts(wrapper)).toEqual([]);
	});

	test('Closing the last type tag widens the scope instead of emptying it', async () => {
		const wrapper = mountFilter({ types: [EstateType.Building] });

		await wrapper.find('.filter-tag').trigger('click');

		expect(lastEmit(wrapper)).toEqual({ types: SEARCHABLE_TYPES });
	});

	test('Every selected verksamhetstyp gets its own named tag', async () => {
		const wrapper = mountFilter({
			types: [EstateType.Building],
			businessTypes: [5, 7],
		});
		await flushPromises();

		expect(tagTexts(wrapper)).toEqual([
			sv.component.estateSearchFilter.typeTag.building,
			'Skola',
			'Förskola',
		]);
	});

	test('Clear all widens the scope and drops every filter', async () => {
		const wrapper = mountFilter(
			{ types: [EstateType.Building], businessTypes: [5] },
			true
		);
		await flushPromises();

		await wrapper.find('button').trigger('click');

		expect(lastEmit(wrapper)).toEqual({ types: SEARCHABLE_TYPES });
	});

	test('Clear all sits in the panel, never in the collapsed tag row', () => {
		const wrapper = mountFilter({
			types: [EstateType.Building],
			businessTypes: [5],
		});

		expect(wrapper.find('.filter-tags button').exists()).toBe(false);
		expect(wrapper.find('.filter-fields button').text()).toBe(
			sv.component.estateSearchFilter.clearAll
		);
	});

	test('Clear all is disabled when there is nothing to clear', () => {
		const wrapper = mountFilter({ types: [...SEARCHABLE_TYPES] }, true);

		expect(wrapper.find('button').attributes('disabled')).toBeDefined();
	});

	test('Clear all is enabled as soon as a filter applies', () => {
		const wrapper = mountFilter({ types: [EstateType.Building] }, true);

		expect(wrapper.find('button').attributes('disabled')).toBeUndefined();
	});

	test('Closing a verksamhetstyp tag keeps the other selections', async () => {
		const wrapper = mountFilter({
			types: [EstateType.Building],
			businessTypes: [5, 7],
		});
		await flushPromises();

		await wrapper.findAll('.filter-tag')[1].trigger('click');

		expect(lastEmit(wrapper)).toEqual({
			types: [EstateType.Building],
			businessTypes: [7],
		});
	});
});
