<template>
	<v-autocomplete
		ref="auto"
		:items="props.items"
		:item-title="props.itemTitle"
		:item-value="props.itemValue"
		v-model="selectedItemId"
		:label="props.title"
		variant="outlined"
		density="comfortable"
		color="primary"
		autocomplete="off"
		:menu-props="menuProps"
		:no-data-text="
			props.loading
				? $t('component.baseAutocomplete.loading')
				: $t('component.baseAutocomplete.noDataAvailable')
		"
		:loading="props.loading"
		:disabled="props.disabled"
	>
		<template v-slot:item="{ props, item }">
			<div v-if="item.raw.groupTitle" class="pl-4 pb-1 group-title">
				<b>{{ item.raw.groupTitle }}</b>
			</div>
			<v-list-item v-bind="props" :title="item.title"></v-list-item>
		</template>
	</v-autocomplete>
</template>
<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';

const props = defineProps<{
	modelValue: string | null;
	items: IAutocompleteItem[];
	title: string;
	itemTitle: string;
	itemValue: string;
	menuProps: object;
	loading?: boolean;
	disabled?: boolean;
}>();
const emit = defineEmits(['update:modelValue']);

const selectedItemId = computed({
	get: () => props.modelValue,
	set: (newValue: string | null) => {
		emit('update:modelValue', newValue);
	},
});

interface IAutocompleteItem {
	[key: string]: string;
}

function pickDefaultItemIfOnlyOne() {
	if (props.items && props.items.length === 1) {
		if (props.itemValue in props.items[0]) {
			selectedItemId.value = props.items[0][props.itemValue];
		}
	}
}

watch(
	() => props.items,
	() => {
		selectedItemId.value = null;
		pickDefaultItemIfOnlyOne();
	}
);

onMounted(async () => {
	pickDefaultItemIfOnlyOne();
});
</script>

<style scoped lang="css">
:deep(.v-list-item-title) {
	white-space: unset;
	text-overflow: unset;
}
</style>
