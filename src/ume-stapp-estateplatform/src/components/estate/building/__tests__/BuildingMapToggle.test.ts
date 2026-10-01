import { mount } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import BuildingMapToggle from '../BuildingMapToggle.vue';
import BaseIconButton from '@/components/shared/BaseIconButton.vue';
import { ActiveMapType } from '@/models/Enums';
import sv from '@/locales/sv.json';

const BtnToggleStub = {
	props: { modelValue: null, mandatory: Boolean },
	emits: ['update:modelValue'],
	template: '<div><slot /></div>',
};

const mountToggle = (props: {
	activeMap: ActiveMapType;
	blueprintAvailable: boolean;
	variant: 'overlay' | 'inline';
}) =>
	mount(BuildingMapToggle, {
		props,
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
			stubs: {
				'v-btn-toggle': BtnToggleStub,
				'v-tooltip': {
					template: '<div><slot name="activator" /></div>',
				},
				'v-btn': {
					props: { disabled: Boolean },
					template:
						'<button :disabled="disabled" v-bind="$attrs"><slot /></button>',
				},
			},
		},
	});

const toggle = (wrapper: ReturnType<typeof mountToggle>) =>
	wrapper.findComponent(BtnToggleStub);

const pills = (wrapper: ReturnType<typeof mountToggle>) =>
	wrapper.findAll('button');

describe('BuildingMapToggle', () => {
	test('blueprint pill is disabled when the building has no blueprint', () => {
		const wrapper = mountToggle({
			activeMap: ActiveMapType.Map,
			blueprintAvailable: false,
			variant: 'overlay',
		});

		const [map, blueprint] = pills(wrapper);
		expect(map.attributes('disabled')).toBeUndefined();
		expect(blueprint.attributes('disabled')).toBeDefined();
	});

	test('selects the active map type on the map overlay', () => {
		const wrapper = mountToggle({
			activeMap: ActiveMapType.Blueprint,
			blueprintAvailable: true,
			variant: 'overlay',
		});

		expect(toggle(wrapper).props('modelValue')).toBe(
			ActiveMapType.Blueprint
		);
	});

	test('inline variant marks nothing selected, since the map pane is hidden', () => {
		const wrapper = mountToggle({
			activeMap: ActiveMapType.Blueprint,
			blueprintAvailable: true,
			variant: 'inline',
		});

		expect(toggle(wrapper).exists()).toBe(false);
	});

	test('inline variant re-emits the map type already shown, so it reopens fullscreen', async () => {
		const wrapper = mountToggle({
			activeMap: ActiveMapType.Map,
			blueprintAvailable: true,
			variant: 'inline',
		});

		await pills(wrapper)[0].trigger('click');

		expect(wrapper.emitted('select')?.at(-1)?.[0]).toBe(ActiveMapType.Map);
	});

	test('inline blueprint pill is disabled when the building has no blueprint', () => {
		const wrapper = mountToggle({
			activeMap: ActiveMapType.Map,
			blueprintAvailable: false,
			variant: 'inline',
		});

		const [map, blueprint] = pills(wrapper);
		expect(map.attributes('disabled')).toBeUndefined();
		expect(blueprint.attributes('disabled')).toBeDefined();
	});

	test('explains the disabled blueprint pill only when it is missing', () => {
		const missing = sv.component.buildingDetails.blueprintMissingTooltip;

		const withBlueprint = mountToggle({
			activeMap: ActiveMapType.Map,
			blueprintAvailable: true,
			variant: 'inline',
		});
		const withoutBlueprint = mountToggle({
			activeMap: ActiveMapType.Map,
			blueprintAvailable: false,
			variant: 'inline',
		});

		const tooltipOf = (wrapper: ReturnType<typeof mountToggle>) =>
			wrapper.findAllComponents(BaseIconButton)[1].props('tooltip');

		expect(tooltipOf(withBlueprint)).toBe('');
		expect(tooltipOf(withoutBlueprint)).toBe(missing);
	});

	test('emits the selected map type', async () => {
		const wrapper = mountToggle({
			activeMap: ActiveMapType.Map,
			blueprintAvailable: true,
			variant: 'overlay',
		});

		await toggle(wrapper).vm.$emit(
			'update:modelValue',
			ActiveMapType.Blueprint
		);

		expect(wrapper.emitted('select')?.at(-1)?.[0]).toBe(
			ActiveMapType.Blueprint
		);
	});

	test('ignores a cleared selection', async () => {
		const wrapper = mountToggle({
			activeMap: ActiveMapType.Map,
			blueprintAvailable: true,
			variant: 'overlay',
		});

		await toggle(wrapper).vm.$emit('update:modelValue', null);

		expect(wrapper.emitted('select')).toBeUndefined();
	});
});
