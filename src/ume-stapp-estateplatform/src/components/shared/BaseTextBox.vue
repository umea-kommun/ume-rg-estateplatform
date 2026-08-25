<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/base/BaseTextBox.vue -->
<template>
	<div class="base-text-box">
		<vee-field
			:name="props.name ?? id"
			:label="validationLabel ?? label"
			v-model="value"
			v-slot="{ field, errors }"
			:rules="rules"
			:keepValue="true"
		>
			<base-form-field
				:id="'label' + id"
				:labelFor="id"
				:label="label"
				:is-required="rules.indexOf('required') > -1"
				:errorDisplay="!!errors.length || !!errorMessage"
			>
				<v-text-field
					v-if="!textArea"
					v-bind="field"
					:id="id"
					density="compact"
					color="primary"
					:prepend-inner-icon="prependInnerIcon"
					aria-autocomplete="both"
					aria-haspopup="false"
					:aria-labelledby="ariaLabelledby"
					:variant="variant"
					:rounded="rounded"
					:aria-describedby="
						!!errors.length || !!errorMessage ? 'error-' + id : null
					"
					:disabled="disabled"
					single-line
					:placeholder="placeholder"
					:error="!!errors.length || !!errorMessage"
					@input="emit('input')"
					hide-details
				/>
				<v-textarea
					v-else
					v-bind="field"
					:id="id"
					density="compact"
					color="primary"
					:prepend-inner-icon="prependInnerIcon"
					aria-autocomplete="both"
					aria-haspopup="false"
					:aria-labelledby="ariaLabelledby"
					:variant="variant"
					:rounded="rounded"
					:auto-grow="autoGrow"
					:aria-describedby="
						!!errors.length || !!errorMessage ? 'error-' + id : null
					"
					:disabled="disabled"
					:placeholder="placeholder"
					:error="!!errors.length || !!errorMessage"
					@input="emit('input')"
					hide-details
				/>
				<base-help-text
					:helpText="helpText"
					:getValidationId="id"
					:errors="errorMessage ? [errorMessage, ...errors] : errors"
				/>
			</base-form-field>
		</vee-field>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Field as VeeField } from 'vee-validate';
import BaseHelpText from '@/components/shared/BaseHelpAndErrorText.vue';
import BaseFormField from '@/components/shared/BaseFormField.vue';

const props = defineProps({
	id: { type: String, required: true },
	name: String,
	modelValue: {
		type: String,
		required: true,
	},
	label: String,
	validationLabel: String,
	helpText: String,
	variant: String as () => 'filled' | 'outlined' | 'underlined',
	rounded: String,
	autoGrow: Boolean,
	rules: {
		type: String,
		default: '',
	},
	textArea: {
		type: Boolean,
		default: false,
	},
	prefix: {
		type: String,
		default: '',
	},
	placeholder: {
		type: String,
		default: '',
	},
	disabled: {
		type: Boolean,
		default: false,
	},
	errorMessage: {
		type: String,
		default: '',
	},
	prependInnerIcon: String,
	ariaLabelledby: String,
});

const emit = defineEmits(['update:modelValue', 'input']);

const value = computed({
	get: () => {
		return props.modelValue;
	},
	set: (newValue: string) => {
		emit('update:modelValue', newValue);
	},
});
</script>

<style scoped lang="scss">
.admin-text-box {
	:deep(.v-input .v-field__field) {
		--v-input-padding-top: 5px;
	}
	:deep(.v-input__details) {
		display: none;
	}

	:deep(.v-text-field__prefix) {
		opacity: 1;
	}

	&.prefix {
		:deep(.v-input .v-field__field input) {
			padding-left: 0;
		}
	}
}
</style>
