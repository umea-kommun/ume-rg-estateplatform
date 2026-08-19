<template>
	<div class="estate-search-filter py-4">
		<v-autocomplete
			variant="outlined"
			v-model="selectedBusinessTypes"
			:items="businessTypes"
			:label="
				$t('component.estateSearchFilter.businessTypeLabel')
			"
			color="primary"
			item-title="name"
			item-value="id"
			density="comfortable"
			:loading="isBusyFetchFilters"
			autocomplete="off"
			chips
			multiple
			clearable
		/>
		<div class="d-flex justify-center mt-4">
			<v-btn
				variant="text"
				color="primary"
				prepend-icon="filter_list_off"
				@click="clearFilters"
			>
				{{ $t('component.estateSearchFilter.clearFilter') }}
			</v-btn>
		</div>
	</div>
</template>
<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import { IBusinessType, SearchFilter } from '@/models/Interfaces';
import { IRootState } from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';

const props = defineProps<{
	modelValue: SearchFilter;
}>();
const emit = defineEmits(['update:modelValue', 'close']);

const store = useStore<IRootState>();
const { t } = useI18n();

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
	} catch (err) {
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchSearchFilter'),
		});
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
