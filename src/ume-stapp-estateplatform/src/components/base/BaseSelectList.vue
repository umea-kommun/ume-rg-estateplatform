<template>
	<div class="base-select-list">
		<Field
			:name="props.name ?? 'name' + id"
			:label="label"
			v-model="value"
			v-slot="{ field, errors }"
			type="select"
			:rules="rules"
			:keepValue="true"
			:standalone="props.standalone"
		>
			<base-form-field
				:id="'label' + id"
				:labelFor="id"
				:label="label"
				:is-required="rules.indexOf('required') > -1"
				:errorDisplay="!!errors.length"
			>
				<v-autocomplete
					v-if="isAutocomplete"
					ref="autocompleteRef"
					:id="id"
					v-model:search="searchValue"
					v-model="value"
					:items="items"
					:item-title="itemTitle"
					:item-value="itemValue"
					@click:append-inner="MenuArrow"
					:hide-no-data="true"
					:menu-props="{
						maxHeight: '200',
						class: 'v-select-menu v-menu-' + id,
					}"
					:multiple="multiple"
					:chips="chips ?? multiple"
					persistent-hint
					:disabled="disabled"
					density="compact"
					:color="errors.length ? 'error' : 'primary'"
					:error="!!errors.length"
					:aria-describedby="!!errors.length ? 'error-' + id : null"
					:dropdown-should-open="() => true"
					autocomplete="off"
					:return-object="returnObject"
					:loading="loading"
				>
					<template v-slot:append-inner>
						<slot name="append-inner"></slot>
					</template>
					<template v-if="slots.item" v-slot:item="{ item, props }">
						<slot
							name="item"
							:item="item"
							:select="selectItem"
							:props="props"
						></slot>
					</template>
					<template
						v-if="slots.selection"
						v-slot:selection="{ item, index }"
					>
						<slot
							name="selection"
							:item="item"
							:index="index"
						></slot>
					</template>
				</v-autocomplete>
				<v-select
					v-else
					:id="id"
					ref="selectRef"
					v-model="value"
					v-bind="field"
					density="compact"
					:color="
						props.errorMessages?.length || errors?.length
							? 'error'
							: 'primary'
					"
					:items="items"
					:item-title="itemTitle"
					:item-value="itemValue"
					:multiple="multiple"
					:chips="chips ?? multiple"
					:disabled="disabled"
					:menu-props="{
						maxHeight: '300',
					}"
					:return-object="returnObject"
					:loading="loading"
				>
					<template v-slot:append-inner>
						<slot name="append-inner"></slot>
					</template>
					<template v-if="slots.item" v-slot:item="{ item, props }">
						<slot
							name="item"
							:item="item"
							:select="selectItem"
							:props="props"
						></slot>
					</template>
					<template
						v-if="slots.selection"
						v-slot:selection="{ item, index }"
					>
						<slot
							name="selection"
							:item="item"
							:index="index"
						></slot>
					</template>
				</v-select>
				<BaseHelpText
					:helpText="helpText"
					:getValidationId="id"
					:errors="[...props.errorMessages, ...errors]"
				/>
			</base-form-field>
		</Field>
	</div>
</template>

<script setup lang="ts">
import { computed, PropType, ref, useSlots } from 'vue';
import { Field } from 'vee-validate';
import BaseHelpText from '@/components/base/BaseHelpAndErrorText.vue';
import BaseFormField from '@/components/base/BaseFormField.vue';

const props = defineProps({
	id: { type: String, required: true },
	name: String,
	modelValue: [Number, String, Array, Object],
	items: {
		type: Array,
		required: true,
	},
	label: String,
	helpText: String,
	itemTitle: {
		type: String,
		default: 'title',
	},
	itemValue: {
		type: String,
		default: 'value',
	},
	multiple: {
		type: Boolean,
		default: false,
	},
	chips: {
		type: Boolean,
	},
	disabled: {
		type: Boolean,
		default: false,
	},
	errorMessages: {
		type: Array as PropType<string[]>,
		default: () => [],
	},
	rules: {
		type: String,
		default: '',
	},
	standalone: {
		type: Boolean,
		default: true,
	},
	returnObject: {
		type: Boolean,
		default: false,
	},
	loading: {
		type: Boolean,
		default: false,
	},
});

const emit = defineEmits(['update:modelValue']);

const slots = useSlots();

const value = computed({
	get: () => {
		return props.modelValue;
	},
	set: (newValue: string | number | object | unknown[] | undefined) => {
		emit('update:modelValue', newValue);
	},
});

const isAutocomplete = computed(() => props.items.length > 10);

const selectRef = ref();
const autocompleteRef = ref();

const selectItem = (item: unknown): void => {
	if (isAutocomplete.value) {
		autocompleteRef.value?.select(item);
	} else {
		selectRef.value?.select(item);
	}
};

const searchValue = ref<string>('');
function MenuArrow(): void {
	const productSortSelect = autocompleteRef.value;
	if (productSortSelect.isMenuActive) {
		autocompleteRef.value.isMenuActive = false;
		document.getElementById(props.id)?.blur();
	} else {
		autocompleteRef.value.isMenuActive = true;
		document.getElementById(props.id)?.focus();
	}
}
</script>

<style scoped lang="scss">
.base-select-list {
	:deep(.v-field__input) {
		padding-left: 8px;
		padding-top: 11px;
		padding-bottom: 10px;
	}
	:deep(.v-input__details) {
		display: none;
	}

	:deep(.v-select--chips) {
		.v-field__input {
			padding-top: 5px;
			padding-bottom: 4px;
			min-height: 45px;
			align-items: center;
		}
		.v-select__selection {
			margin: 0;
			margin-left: 8px;
			padding: 2px !important;
		}
	}
}
</style>
