<template>
	<div class="kvittens-template-target-select">
		<vee-field
			:name="label"
			type="select"
			:rules="required ? 'required' : ''"
			:model-value="targets"
			v-slot="{ errors }"
		>
			<label> {{ label }}<span v-if="required">*</span> </label>
			<div class="d-flex flex-wrap ga-4 py-2">
				<v-chip
					v-for="target of displayTargets"
					:key="`${target.schoolForm}|${target.schoolYears}`"
					size="large"
					closable
					@click:close="removeTarget(target)"
					:disabled="disabled"
				>
					{{ target.schoolFormLabel }} {{ target.schoolYearsLabel }}
				</v-chip>
				<v-btn
					variant="text"
					prepend-icon="add"
					color="primary"
					@click="showAddDialog = true"
					:disabled="disabled"
				>
					{{
						$t(
							'component.internal.kvittensTemplateEdit.field.addSchoolFormAndYear'
						)
					}}
				</v-btn>
			</div>
			<base-help-and-error-text :errors="errors" />
		</vee-field>
		<target-select-dialog
			:template="template"
			v-model:visible="showAddDialog"
			v-model:targets="targets"
		/>
	</div>
</template>

<script setup lang="ts">
import {
	ICreateKvittensTemplate,
	IDisplayKvittensTemplateTarget,
	IKvittensTemplate,
	IKvittensTemplateTarget,
} from '@/models/kvittens/Interfaces';
import { computed, ref } from 'vue';
import { useKvittensTemplateTarget } from '../KvittensTemplateTarget';
import { Field as VeeField } from 'vee-validate';
import BaseHelpAndErrorText from '@/components/base/BaseHelpAndErrorText.vue';
import TargetSelectDialog from './TargetSelectDialog.vue';

const props = defineProps<{
	template: IKvittensTemplate | ICreateKvittensTemplate;
	modelValue: IKvittensTemplateTarget[];
	label: string;
	required?: boolean;
	disabled?: boolean;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', value: IKvittensTemplateTarget[]): void;
}>();

const targets = computed({
	get() {
		return props.modelValue;
	},
	set(value) {
		emit('update:modelValue', value);
	},
});

const { addDisplayTargetsToTemplate } = useKvittensTemplateTarget();
const displayTargets = computed(() => {
	if (!props.template) {
		return [];
	}
	return addDisplayTargetsToTemplate(props.template as IKvittensTemplate)
		.displayTargets;
});

const showAddDialog = ref(false);

const removeTarget = (targetToRemove: IDisplayKvittensTemplateTarget) => {
	targets.value = targets.value.filter(
		(target) =>
			!(
				target.schoolForm === targetToRemove.schoolForm &&
				targetToRemove.schoolYears.includes(target.schoolYear)
			)
	);
};
</script>

<style lang="scss" scoped>
.kvittens-template-target-select {
	.v-chip--disabled,
	.v-btn--disabled {
		opacity: 0.5;
	}
}
.kvittens-template-target-dialog {
	:deep(label) {
		opacity: 1;
	}
}
</style>
