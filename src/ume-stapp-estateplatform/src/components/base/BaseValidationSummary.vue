<template>
	<div :id="id" class="form-validation-summary" v-if="errorCount > 0">
		<v-alert type="error" variant="outlined" :value="true">
			<label class="field-title body-2">
				<span
					>{{
						$t('component.baseValidationSummary.error', {
							count: errorCount,
						})
					}}
				</span>
				<span aria-hidden="true">{{
					$t('component.baseValidationSummary.resolution')
				}}</span>
			</label>
			<ul>
				<li
					v-for="(error, id) in validationErrors"
					:key="'validationerror' + id"
				>
					<a :href="'#' + id">{{ error }}</a>
				</li>
			</ul>
		</v-alert>
	</div>
</template>

<script setup lang="ts">
/**
 * Component to render a summary of validation errors. List errors and provide shortcut to input field.
 */
import { computed, PropType } from 'vue';

const props = defineProps({
	validationErrors: {
		type: Object as PropType<Record<string, string | undefined>>,
	},
	id: String,
});

const errorCount = computed(() => {
	const values = Object.values(props.validationErrors as object);
	return values.length;
});
</script>

<style scoped lang="scss">
.form-validation-summary {
	.field-title {
		padding-bottom: 8px;
		display: block;
	}
	ul {
		list-style: inside;
	}
}
</style>
