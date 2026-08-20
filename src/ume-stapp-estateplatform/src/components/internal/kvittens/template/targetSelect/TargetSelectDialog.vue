<template>
	<v-dialog
		v-model="showAddDialog"
		:max-width="600"
		class="kvittens-template-target-dialog"
	>
		<v-card
			:title="
				$t('component.internal.kvittensTemplateEdit.targetDialog.title')
			"
		>
			<v-card-text class="pb-0">
				<base-select-list
					:label="
						t(
							'component.internal.kvittensTemplateEdit.targetDialog.schoolForm'
						)
					"
					id="schoolForm"
					:items="schoolForms"
					v-model="selectedSchoolForm"
				/>
				<template v-if="selectedSchoolForm">
					<v-range-slider
						v-if="isRangeOfSchoolYears"
						:label="
							$t(
								'component.internal.kvittensTemplateEdit.targetDialog.schoolYear'
							)
						"
						v-model="selectedYearRange"
						class="mt-10"
						:min="schoolYearsNumericSorted[0]"
						:max="
							schoolYearsNumericSorted[
								schoolYearsNumericSorted.length - 1
							]
						"
						color="primary"
						:step="1"
						thumb-label="always"
						show-ticks="always"
					/>
					<base-select-list
						v-else
						:label="
							$t(
								'component.internal.kvittensTemplateEdit.targetDialog.schoolYear'
							)
						"
						id="schoolYear"
						:items="schoolYears"
						v-model="selectedYearList"
						multiple
					/>
				</template>
			</v-card-text>
			<v-card-actions>
				<v-btn text @click="showAddDialog = false">
					{{
						$t(
							'component.internal.kvittensTemplateEdit.targetDialog.cancel'
						)
					}}
				</v-btn>
				<v-btn color="primary" @click="addTarget">
					{{
						$t(
							'component.internal.kvittensTemplateEdit.targetDialog.add'
						)
					}}
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script lang="ts" setup>
import BaseSelectList from '@/components/base/BaseSelectList.vue';
import {
	IKvittensTemplateTarget,
	ISchoolYearPerSchoolForm,
} from '@/models/kvittens/Interfaces';
import store from '@/store/store';
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	visible: boolean;
	targets: IKvittensTemplateTarget[];
}>();

const emit = defineEmits<{
	(e: 'update:visible', value: boolean): void;
	(e: 'update:targets', value: IKvittensTemplateTarget[]): void;
}>();

const { t } = useI18n();

const showAddDialog = computed({
	get() {
		return props.visible;
	},
	set(value) {
		emit('update:visible', value);
	},
});

const targets = computed({
	get() {
		return props.targets;
	},
	set(value) {
		emit('update:targets', value);
	},
});

const schoolFormsWithSchoolYears = ref<ISchoolYearPerSchoolForm[] | null>(null);

const schoolForms = computed(() => {
	if (!schoolFormsWithSchoolYears.value) {
		return [];
	}
	return schoolFormsWithSchoolYears.value
		.filter((item) => item.schoolYears.length > 0)
		.map((item) => ({
			value: item.schoolForm,
			title: t('schoolForm.' + item.schoolForm),
		}))
		.sort((a, b) => a.title.localeCompare(b.title));
});
const selectedSchoolForm = ref<string | undefined>(undefined);
const selectedYearRange = ref<[number, number]>([0, 0]);
const selectedYearList = ref<string[]>([]);

const schoolYears = computed(() => {
	if (!selectedSchoolForm.value || !schoolFormsWithSchoolYears.value) {
		return [];
	}
	const schoolForm = schoolFormsWithSchoolYears.value.find(
		(item) => item.schoolForm === selectedSchoolForm.value
	);
	if (!schoolForm) {
		return [];
	}
	return schoolForm.schoolYears;
});

const schoolYearsNumericSorted = computed(() =>
	schoolYears.value
		.map(Number)
		.filter((n) => !isNaN(n))
		.sort((a, b) => a - b)
);

watch(selectedSchoolForm, () => {
	selectedYearList.value = [];
	const n = schoolYearsNumericSorted.value;
	if (n.length > 0) {
		selectedYearRange.value = [n[0], n[n.length - 1]];
	}
});

const isRangeOfSchoolYears = computed(() => {
	if (schoolYears.value.length <= 1) {
		return false;
	}
	const parsed = schoolYears.value.map(Number);
	if (parsed.some(isNaN)) {
		return false;
	}
	const sorted = parsed.sort((a, b) => a - b);
	return sorted.every((year, i) => i === 0 || year === sorted[i - 1] + 1);
});

const isFetchingSchoolForms = ref(false);
const fetchSchoolFormsWithSchoolYears = async () => {
	isFetchingSchoolForms.value = true;
	try {
		schoolFormsWithSchoolYears.value = await store.dispatch(
			'getSchoolYearsPerSchoolForm'
		);
	} finally {
		isFetchingSchoolForms.value = false;
	}
};

onMounted(() => {
	fetchSchoolFormsWithSchoolYears();
});

const addTarget = () => {
	if (!selectedSchoolForm.value) return;

	const yearsToAdd = isRangeOfSchoolYears.value
		? Array.from(
				{
					length:
						selectedYearRange.value[1] -
						selectedYearRange.value[0] +
						1,
				},
				(_, i) => String(selectedYearRange.value[0] + i)
		  )
		: selectedYearList.value;

	const newTargets = yearsToAdd
		.filter(
			(year) =>
				!targets.value.some(
					(t) =>
						t.schoolForm === selectedSchoolForm.value &&
						t.schoolYear === year
				)
		)
		.map((year) => ({
			schoolForm: selectedSchoolForm.value as string,
			schoolYear: year,
		}));

	targets.value = [...targets.value, ...newTargets];
	showAddDialog.value = false;
};
</script>
