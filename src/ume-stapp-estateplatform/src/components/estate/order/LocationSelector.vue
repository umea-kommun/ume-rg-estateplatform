<template>
	<div class="location-selector">
		<!-- Collapsed: a compact card of the chosen location. -->
		<selection-card
			v-if="selectedOption"
			class="mt-2"
			:icon="selectedOption.icon"
			:title="selectedOption.title"
			:description="selectedOption.selected"
		>
			<template #actions>
				<v-btn
					rounded="lg"
					variant="outlined"
					color="grey-darken-2"
					@click="emit('select', null)"
				>
					{{ $t('component.faultReport.changeAnswer') }}
				</v-btn>
			</template>
		</selection-card>

		<!-- Selecting: the shared indoor/outdoor tile grid. -->
		<option-card-grid
			v-else
			:options="options"
			:selected="selected"
			@select="emit('select', $event)"
		/>
	</div>
</template>

<script setup lang="ts">
/**
 * Generic indoor/outdoor (location) selector shared by the fault report and order
 * flows: the same OptionCardGrid tiles + collapsed SelectionCard, with the copy
 * supplied per flow via `options` (each flow wording differs - a fault vs an
 * order). The value is a plain string so callers map it to their own enum.
 */
import { computed } from 'vue';
import OptionCardGrid from './OptionCardGrid.vue';
import type { OptionCard } from './OptionCardGrid.vue';
import SelectionCard from './SelectionCard.vue';

// A location tile plus the sentence shown once it is the collapsed summary.
export type LocationOption = OptionCard & { selected: string };

const props = defineProps<{
	options: LocationOption[];
	selected: string | null;
}>();

const emit = defineEmits(['select']);

const selectedOption = computed(() =>
	props.options.find((option) => option.value === props.selected)
);
</script>
