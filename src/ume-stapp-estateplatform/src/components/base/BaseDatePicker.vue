<template>
	<div class="base-date-picker">
		<field
			:name="id"
			:label="label"
			v-model="value"
			:keepValue="true"
			v-slot="{ field, errors }"
			:rules="validationRules"
		>
			<base-form-field
				:id="'label' + id"
				:labelFor="id"
				:label="label"
				:is-required="isRequired"
				:errorDisplay="!!errors.length"
			>
				<t-date-picker
					ref="datepicker"
					:id="id"
					v-bind="field"
					:aria-describedby="!!errors.length ? 'error-' + id : null"
					:min="minDate"
					:max="maxDate"
					:disabled="disabled"
					variant="outlined"
				/>

				<base-help-text
					:helpText="helpText"
					:getValidationId="id"
					:errors="errors"
				/>
			</base-form-field>
		</field>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import BaseHelpText from '@/components/base/BaseHelpAndErrorText.vue';
import BaseFormField from '@/components/base/BaseFormField.vue';
import { TDatePicker } from '@turkos/components';
import { Field } from 'vee-validate';

const props = defineProps({
	id: { type: String, required: true },
	modelValue: {
		type: String,
		required: true,
	},
	label: String,
	helpText: String,
	rules: {
		type: String,
		default: '',
	},
	minDate: String,
	maxDate: String,
	disabled: {
		type: Boolean,
		default: false,
	},
});

const emit = defineEmits(['update:modelValue']);

const value = computed({
	get: () => {
		return props.modelValue;
	},
	set: (newValue: string) => {
		emit('update:modelValue', newValue);
	},
});

const isRequired = computed(() => props.rules.indexOf('required') > -1);

const datepicker = ref<{ valid: boolean } | null>(null);
const validationRules = computed(() => {
	let rules = ['validDate', ...props.rules.split('|')];

	if (props.minDate) {
		rules.push('minDate:' + props.minDate);
	}
	if (props.maxDate) {
		rules.push('maxDate:' + props.maxDate);
	}
	if (datepicker.value?.valid === false) {
		// The html datepicker value is invalid, show error instead of saying that field is required
		if (isRequired.value) {
			rules = rules.filter((rule) => rule !== 'required');
		}
		rules.push('invalidDate');
	}
	return rules.join('|');
});
</script>

<style scoped lang="scss">
.base-date-picker {
	:deep(input) {
		background-color: transparent;
	}
}
</style>
