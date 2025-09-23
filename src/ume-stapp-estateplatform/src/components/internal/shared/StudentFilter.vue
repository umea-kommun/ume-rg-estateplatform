<template>
	<div class="dropdowns mt-5 mb-4">
		<base-autocomplete
			:items="fetchedSchools"
			v-model="selectedSchoolId"
			:title="t('component.internal.studentFilter.schoolLabel')"
			itemTitle="name"
			itemValue="refId"
			:menuProps="autocompleteMenuProps"
			:loading="isBusyLoadingSchoolsAndGroups"
		/>
		<base-autocomplete
			:items="groupsInSelectedSchool"
			v-model="selectedGroupId"
			:title="t('component.internal.studentFilter.classLabel')"
			itemTitle="name"
			itemValue="refId"
			:menuProps="autocompleteMenuProps"
			:disabled="!selectedSchoolId"
		/>
		<base-autocomplete
			:items="studentsInSelectedGroup"
			v-model="selectedStudentId"
			:title="t('component.internal.studentFilter.studentLabel')"
			itemTitle="name"
			itemValue="studentSsno"
			:menuProps="autocompleteMenuProps"
			:loading="isBusyLoadingStudents"
			:disabled="!selectedGroupId"
		/>
	</div>
	<v-alert
		v-if="!isBusyLoadingSchoolsAndGroups && !fetchedSchools.length"
		icon="warning"
		>{{ t('component.internal.studentFilter.noAccess') }}</v-alert
	>
</template>

<script setup lang="ts">
import BaseAutocomplete from '@/components/base/BaseAutocomplete.vue';
import { DispatchType } from '@/models/Enums';
import { IRootState } from '@/models/Interfaces';
import {
	IFilterClass,
	IFilterSchool,
	IFilterStudent,
} from '@/models/schoolInterfaces';
import ErrorService from '@/utils/ErrorService';
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';

const emit = defineEmits(['student-selected']);

const { t } = useI18n();
const store = useStore<IRootState>();

const autocompleteMenuProps = { width: '300px' };

const isBusyLoadingSchoolsAndGroups = ref(false);
const isBusyLoadingStudents = ref(false);

const fetchedSchools = ref<IFilterSchool[]>([]);
const fetchedGroups = ref<IFilterClass[]>([]);
const studentsInSelectedGroup = ref<IFilterStudent[]>([]);

const selectedSchoolId = ref<string | null>(null);
const selectedGroupId = ref<string | null>(null);
const selectedStudentId = ref<string | null>(null);

const groupsInSelectedSchool = computed(() => {
	if (!selectedSchoolId.value) {
		return [];
	}
	return fetchedGroups.value.filter(
		(group) => group.schoolRefId === selectedSchoolId.value
	);
});

//Fetch data
function sort_asc(
	a: IFilterSchool | IFilterClass | IFilterStudent,
	b: IFilterSchool | IFilterClass | IFilterStudent
) {
	if (a.name < b.name) return -1;
	if (a.name > b.name) return 1;
	return 0;
}

async function fetchSchools() {
	isBusyLoadingSchoolsAndGroups.value = true;

	try {
		const { schools: schools, classes: groups } = await store.dispatch(
			DispatchType.GetKvittensFilterGroups
		);
		fetchedSchools.value = schools.sort(sort_asc);
		fetchedGroups.value = groups.sort(sort_asc);
	} catch (err) {
		ErrorService.onError({ err });
	} finally {
		isBusyLoadingSchoolsAndGroups.value = false;
	}
}

async function fetchStudents() {
	isBusyLoadingStudents.value = true;

	try {
		const students: IFilterStudent[] = await store.dispatch(
			DispatchType.GetStudentsInGroup,
			selectedGroupId.value
		);
		studentsInSelectedGroup.value = students.sort(sort_asc);
	} catch (err) {
		ErrorService.onError({ err });
	} finally {
		isBusyLoadingStudents.value = false;
	}
}

watch(selectedGroupId, () => {
	studentsInSelectedGroup.value = [];
	if (selectedGroupId.value) {
		fetchStudents();
	}
});

watch(selectedStudentId, () => {
	if (!selectedStudentId.value) {
		emit('student-selected', null);
	} else {
		const selectedStudent = studentsInSelectedGroup.value.find(
			(student) => student.studentSsno === selectedStudentId.value
		);
		emit('student-selected', selectedStudent ?? null);
	}
});

onMounted(() => {
	fetchSchools();
});
</script>

<style scoped lang="scss">
.dropdowns {
	display: flex;
	flex-wrap: wrap;
	gap: 14px;

	:deep(.v-input) {
		flex: 1 1 calc(25.333% - 10px);
		max-width: none;
		min-width: 170px;
		margin: 0;
	}
}
</style>
