<template>
	<div class="estate-search-filter py-4">
		<v-autocomplete
			variant="outlined"
			v-model="selectedBusinessTypes"
			:items="businessTypes"
			:label="
				$t('component.internal.estateSearchFilter.businessTypeLabel')
			"
			color="primary"
			item-title="name"
			item-value="id"
			density="comfortable"
			:loading="isBusyFetchFilters"
			chips
			multiple
			clearable
		/>
		<div class="d-flex justify-center mt-4">
			<v-btn
				variant="text"
				color="primary"
				class="regular-text"
				prepend-icon="filter_list_off"
				@click="clearFilters"
			>
				{{ $t('component.internal.estateSearchFilter.clearFilter') }}
			</v-btn>
		</div>
	</div>
</template>
<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import { IBusinessType, SearchFilter } from '@/models/estate/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { computed, onMounted, ref } from 'vue';
import { useStore } from 'vuex';

const props = defineProps<{
	modelValue: SearchFilter;
}>();
const emit = defineEmits(['update:modelValue', 'close']);

const store = useStore<IRootState>();

const searchFilter = computed({
	get: () => props.modelValue,
	set: (value: SearchFilter) => {
		emit('update:modelValue', value);
	},
});

const selectedBusinessTypes = computed({
	get: () => searchFilter.value.businessTypes || [],
	set: (ids: number[]) => {
		if (ids.length) {
			searchFilter.value.businessTypes = ids;
		} else {
			delete searchFilter.value.businessTypes;
		}
	},
});
const businessTypes = ref<IBusinessType[]>([]);

const clearFilters = () => {
	searchFilter.value = {};
	emit('close');
};

const isBusyFetchFilters = ref(false);
const fetchSearchFilters = async () => {
	try {
		isBusyFetchFilters.value = true;
		businessTypes.value = (
			await store.dispatch(DispatchType.GetBusinessTypes)
		).sort((a: IBusinessType, b: IBusinessType) =>
			a.name.localeCompare(b.name)
		);
	} finally {
		isBusyFetchFilters.value = false;
	}
};

onMounted(fetchSearchFilters);
</script>

<style lang="scss" scoped>
.estate-search-filter {
	border-bottom: solid 1px $grey-lighten-4;
}
</style>
