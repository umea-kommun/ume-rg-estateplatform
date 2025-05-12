<template>
	<div class="base-html-editor">
		<Field
			:name="props.name ?? id"
			:label="label"
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
				:errorDisplay="!!errors.length"
			>
				<div class="html-editor">
					<!-- We cannot v-bind="field" because field.onChange causes error with TinyMCE, 
					     but onBlur and modelValue is enough for validation to work -->
					<TinyEditor
						@blur="field.onBlur"
						:model-value="field.value"
						@update:model-value="field['onUpdate:modelValue']"
						:id="id"
						class="editor"
						:init="editorInit"
						:disabled="disabled"
					/>
				</div>
				<div :class="getCharLimitClass" v-if="charLimit">
					{{ textLength }} / {{ charLimit }}
				</div>
				<BaseHelpText
					:helpText="helpText"
					:getValidationId="id"
					:errors="errors"
				/>
			</base-form-field>
		</Field>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { Field } from 'vee-validate';
import BaseHelpText from './BaseHelpAndErrorText.vue';

import TinyEditor from '@tinymce/tinymce-vue'; // Import tinymce-vue plugin
/* Import TinyMCE */
import tinymce, { Editor } from 'tinymce'; // Import tinymce, required to use tinymce without cloud
/* Import plugins */
import 'tinymce/plugins/link'; // Import tinymce theme, required to use tinymce without cloud
import 'tinymce/plugins/lists'; // Import tinymce theme, required to use tinymce without cloud
/* Required TinyMCE components */
import 'tinymce/themes/silver'; // Import tinymce theme, required to use tinymce without cloud
import 'tinymce/models/dom'; // Import tinymce theme, required to use tinymce without cloud
/* Default icons are required. After that, import custom icons if applicable */
import 'tinymce/icons/default'; // Import tinymce theme, required to use tinymce without cloud
/* Import a skin (can be a custom skin instead of the default) */
import 'tinymce/skins/ui/oxide/skin.min.css'; // Import of tinymce skin, required to use tinymce without cloud
import BaseFormField from './BaseFormField.vue';

// Init TinyMce
tinymce.init({});
tinymce.EditorManager.execCommand('mceRemoveEditor', true, TinyEditor);

const props = defineProps({
	id: { type: String, required: true },
	name: String,
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
	width: {
		type: String,
		required: false,
		default: '100%',
	},
	height: {
		type: String,
		required: false,
	},
	charLimit: {
		type: Number,
		required: false,
	},
	disabled: {
		type: Boolean,
		default: false,
	},
});

const textLength = ref<number>(0);

const emit = defineEmits(['update:modelValue', 'input']);

const value = computed({
	get: () => {
		return props.modelValue;
	},
	set: (newValue: string) => {
		emit('update:modelValue', newValue);
	},
});

const editorInit = computed(() => {
	return {
		branding: false,
		menubar: false,
		plugins: ['link', 'lists'],
		width: props.width,
		height: props.height,
		theme: 'silver',
		skin: false,
		content_css: false,
		content_style:
			'body { font-family: "Calibri", "Candara", "Segoe", "Segoe UI", "Optima", Arial, sans-serif; }',
		toolbar:
			// eslint-disable-next-line max-len
			'insertfile a11ycheck undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link | removes | removeformat',
		setup: (ed: Editor) => {
			ed.on('keydown', (args: KeyboardEvent) => {
				textLength.value = ed.getContent({ format: 'text' }).length;
				if (
					args.ctrlKey &&
					(args.key === 'x' || args.key === 'c' || args.key === 'v')
				) {
					return true;
				} else if (
					props.charLimit &&
					(textLength.value === props.charLimit ||
						textLength.value >= props.charLimit) &&
					args.keyCode !== 8 &&
					args.keyCode !== 46 &&
					args.keyCode !== 13
				) {
					return false;
				}
			})
				.on('keyup', () => {
					textLength.value = ed.getContent({ format: 'text' }).length;
				})
				.on('init', () => {
					textLength.value = ed.getContent({ format: 'text' }).length;
				});
		},
	};
});

const getCharLimitClass = computed(() => {
	if (props.charLimit) {
		if (textLength.value > props.charLimit) {
			return 'right-aligned error-text';
		} else {
			return 'right-aligned';
		}
	}
	return '';
});
</script>

<style scoped lang="scss">
.base-html-editor {
	.right-aligned {
		float: right;
	}
	.error-text {
		color: red;
	}
	.html-editor {
		background: #fff !important;

		.mce-content-body {
			padding: 10px 10px 0;
			p:last-child {
				padding-bottom: 0;
				margin-bottom: 0;
			}
		}
	}
}
</style>
