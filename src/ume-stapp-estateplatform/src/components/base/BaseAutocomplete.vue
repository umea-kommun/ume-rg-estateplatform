<template>
	<v-autocomplete
		ref="auto"
		:items="props.items"
		:item-title="props.itemTitle"
		:item-value="props.itemValue"
		v-model="selectedItem"
		:label="props.title"
		variant="outlined"
		density="comfortable"
		:disabled="props.items.length === 0"
		color="primary"
		autocomplete="off"
		:no-data-text="$t('component.baseAutocomplete.noDataAvailable')"
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
import { onMounted, ref, watch } from 'vue';

const props = defineProps<{
	items: IAutocompleteItem[];
	title: string;
	itemTitle: string;
	itemValue: string;
}>();
const emit = defineEmits(['update:selectedItem']);

const selectedItem = ref<string | null>();
watch(selectedItem, async (newValue, oldValue) => {
	if (newValue) {
		emit('update:selectedItem', newValue);
	} else if (oldValue) {
		selectedItem.value = oldValue;
	}
});

interface IAutocompleteItem {
	[key: string]: string;
}

function pickDefaultItemIfOnlyOne() {
	if (props.items && props.items.length === 1) {
		// If only one item in list, preselect it
		if (props.itemValue in props.items[0]) {
			selectedItem.value = props.items[0][props.itemValue];
		}
	}
}

watch(
	() => props.items,
	() => {
		selectedItem.value = null;
		pickDefaultItemIfOnlyOne();
	}
);

onMounted(async () => {
	pickDefaultItemIfOnlyOne();
});
</script>
