<template>
	<div class="estate-search-filter">
		<div
			v-if="activeTags.length"
			class="filter-tags d-flex flex-wrap align-center ga-2"
		>
			<v-chip
				v-for="tag in activeTags"
				:key="tag.key"
				closable
				@click:close="tag.remove()"
			>
				{{ tag.label }}
			</v-chip>
		</div>
		<v-expand-transition>
			<div
				v-show="expanded"
				class="filter-fields d-flex flex-column ga-4 pt-2"
			>
				<v-select
					:label="$t('component.estateSearchFilter.typeLabel')"
					v-model="selectedType"
					:items="typeItems"
					item-title="title"
					item-value="value"
					color="primary"
					rounded="lg"
					density="comfortable"
					variant="outlined"
					clearable
				/>
				<v-autocomplete
					v-model:menu="isMenuOpen"
					@mousedown.capture="rememberMenuState"
					@mousedown="closeMenuOnRepeatClick"
					:label="
						$t('component.estateSearchFilter.businessTypeLabel')
					"
					v-model="selectedBusinessTypes"
					:items="businessTypes"
					item-title="name"
					item-value="id"
					color="primary"
					rounded="lg"
					density="comfortable"
					variant="outlined"
					:loading="isBusyFetchFilters"
					autocomplete="off"
					chips
					multiple
					clearable
				/>
				<v-btn
					:disabled="!activeTags.length"
					class="align-self-center"
					variant="text"
					prepend-icon="close"
					@click="clearAllFilters"
				>
					{{ $t('component.estateSearchFilter.clearAll') }}
				</v-btn>
			</div>
		</v-expand-transition>
	</div>
</template>
<script setup lang="ts">
import { DispatchType, EstateType } from '@/models/Enums';
import { IBusinessType, SearchFilter } from '@/models/Interfaces';
import { IRootState } from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import { isTypeScopeNarrowed, SEARCHABLE_TYPES } from './useEstateSearch';

const props = defineProps<{
	modelValue: SearchFilter;
	expanded?: boolean;
}>();
const emit = defineEmits(['update:modelValue']);

const store = useStore<IRootState>();
const { t } = useI18n();

const searchFilter = computed({
	get: () => props.modelValue,
	set: (value: SearchFilter) => {
		emit('update:modelValue', value);
	},
});

const typeItems = computed(() =>
	SEARCHABLE_TYPES.map((type) => ({
		value: type,
		title: t(`component.estateSearchFilter.typeFilter.${type}`),
	}))
);

const selectedType = computed({
	get: () =>
		searchFilter.value.types?.length === 1
			? searchFilter.value.types[0]
			: null,
	set: (type: EstateType | null) => {
		searchFilter.value = {
			...searchFilter.value,
			types: type ? [type] : [...SEARCHABLE_TYPES],
		};
	},
});

const selectedBusinessTypes = computed({
	get: () => searchFilter.value.businessTypes || [],
	set: (ids: number[]) => {
		const next = { ...searchFilter.value };
		if (ids.length) {
			next.businessTypes = ids;
		} else {
			delete next.businessTypes;
		}
		searchFilter.value = next;
	},
});
const businessTypes = ref<IBusinessType[]>([]);

const removeType = (type: EstateType) => {
	const remaining = (searchFilter.value.types ?? []).filter(
		(value) => value !== type
	);
	searchFilter.value = {
		...searchFilter.value,
		types: remaining.length ? remaining : [...SEARCHABLE_TYPES],
	};
};

const activeTags = computed(() => {
	const types = searchFilter.value.types ?? [];
	const typeTags = isTypeScopeNarrowed(searchFilter.value)
		? types.map((type) => ({
				key: `type-${type}`,
				label: t(`component.estateSearchFilter.typeTag.${type}`),
				remove: () => removeType(type),
		  }))
		: [];

	const businessTypeTags = selectedBusinessTypes.value.map((id) => ({
		key: `businessType-${id}`,
		label:
			businessTypes.value.find((type) => type.id === id)?.name ??
			String(id),
		remove: () => {
			selectedBusinessTypes.value = selectedBusinessTypes.value.filter(
				(value) => value !== id
			);
		},
	}));

	return [...typeTags, ...businessTypeTags];
});

const clearAllFilters = () => {
	searchFilter.value = { types: [...SEARCHABLE_TYPES] };
};

const isMenuOpen = ref(false);
const wasMenuOpen = ref(false);

const rememberMenuState = () => {
	wasMenuOpen.value = isMenuOpen.value;
};

const closeMenuOnRepeatClick = () => {
	if (wasMenuOpen.value) {
		isMenuOpen.value = false;
	}
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
	display: contents;
	.filter-fields {
		flex-basis: 100%;
	}
	.filter-fields :deep(.v-btn--disabled) {
		opacity: 0.5;
	}
	.filter-tags :deep(.v-chip) {
		border-radius: $border-radius;
	}
	// Vuetify pulls the chip close button 6px past the chip padding.
	.filter-tags :deep(.v-chip .v-chip__close) {
		margin-inline-end: 0;
		color: $grey-darken-2;

		&:hover,
		&:focus-visible {
			color: $grey-darken-3;
		}
	}
	:deep(.v-autocomplete__selection) {
		pointer-events: none;
	}
}
</style>
