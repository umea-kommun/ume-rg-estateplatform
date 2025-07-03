<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyFetchingSchoolsAndClasses"
		class="default-passwords"
		:pageTitle="$t('component.appHeader.title.internalDefaultPasswords')"
	>
		<base-back-button />
		<consumer-tester />

		<h1 class="mt-2 mb-4">
			{{ $t('component.internal.defaultPasswords.title') }}
		</h1>
		<div class="filter">
			<div class="dropdowns">
				<base-autocomplete
					class="b-auto"
					:items="fetchedSchools"
					@update:selectedItem="schoolSelected"
					:title="
						$t('component.internal.defaultPasswords.schoolLabel')
					"
					itemTitle="name"
					itemValue="id"
				/>
				<base-autocomplete
					class="b-auto"
					:items="groupsInSelectedSchool"
					@update:selectedItem="groupSelected"
					:title="
						$t('component.internal.defaultPasswords.classLabel')
					"
					itemTitle="name"
					itemValue="id"
				/>
			</div>
			<v-btn
				variant="outlined"
				:disabled="fetchedPasswords.length === 0"
				class="print-btn"
				prepend-icon="print"
				@click="printClicked"
				>{{ $t('component.internal.defaultPasswords.print') }}</v-btn
			>
		</div>
		<default-passwords-table
			class="password-table"
			:items="fetchedPasswords"
			@update:sortBy="updateSortBy"
		/>
		<v-alert
			v-if="
				!isBusyFetchingSchoolsAndClasses &&
				(fetchedSchools.length === 0 || fetchedGroups.length === 0)
			"
			icon="info"
			class="mt-6"
		>
			{{ $t('component.internal.defaultPasswords.noAccess') }}
		</v-alert>
		<v-alert
			v-else-if="!selectedSchoolId || !selectedGroupId"
			icon="info"
			class="mt-6"
		>
			{{ $t('component.internal.defaultPasswords.selectSchoolAndClass') }}
		</v-alert>
		<app-loading-spinner
			v-else-if="isBusyFetchingPasswords"
			:isVisible="true"
		/>
		<v-alert
			v-else-if="fetchedPasswords.length === 0"
			icon="info"
			class="mt-6"
		>
			{{ $t('component.internal.defaultPasswords.noResults') }}
		</v-alert>
		<print-password-table
			:items="fetchedPasswords"
			:school="selectedSchoolName"
			:group="selectedGroupName"
			:sortBy="sortTableBy"
		/>
	</app-content>
</template>
<script setup lang="ts">
import AppContent from '@/components/app/AppContent.vue';
import {
	AppContentSize,
	DispatchType,
	TemplateConnectionType,
} from '@/models/Enums';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import ConsumerTester from '@/components/internal/shared/ConsumerTester.vue';
import DefaultPasswordsTable from './DefaultPasswordsTable.vue';
import PrintPasswordTable from './PrintPasswordTable.vue';
import BaseAutocomplete from '@/components/base/BaseAutocomplete.vue';
import { onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { IRootState, ISortBy } from '@/models/Interfaces';
import { useStore } from 'vuex';
import {
	IPasswordDefaultAssignment,
	IPasswordGroup,
	IPasswordSchool,
} from '@/models/password/Interfaces';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
const route = useRoute();
const store = useStore<IRootState>();

const isBusyFetchingSchoolsAndClasses = ref<boolean>(false);
const isBusyFetchingPasswords = ref<boolean>(false);
const sortTableBy = ref<ISortBy[]>([{ key: 'name', order: 'asc' }]);

// Fetch data
const fetchedSchools = ref<IPasswordSchool[]>([]);
const fetchedGroups = ref<IPasswordGroup[]>([]);
const fetchedPasswords = ref<IPasswordDefaultAssignment[]>([]);

const selectedSchoolId = ref<string | null>(null);
const selectedSchoolName = ref<string | null>(null);
function schoolSelected(id: string) {
	selectedSchoolId.value = id;
	selectedSchoolName.value =
		fetchedSchools.value.find((school) => school.id === id)?.name ?? null;
}

const groupHeaders = {
	[TemplateConnectionType.Department]: t(
		'component.internal.defaultPasswords.groupType.department'
	),
	[TemplateConnectionType.Class]: t(
		'component.internal.defaultPasswords.groupType.class'
	),
	[TemplateConnectionType.EducationGroup]: t(
		'component.internal.defaultPasswords.groupType.educationGroup'
	),
	[TemplateConnectionType.Unit]: t(
		'component.internal.defaultPasswords.groupType.unit'
	),
	[TemplateConnectionType.Skolform]: t(
		'component.internal.defaultPasswords.groupType.skolform'
	),
};

function sortGroupsByType(a: IPasswordGroup, b: IPasswordGroup) {
	if (a.type < b.type) return -1;
	if (a.type > b.type) return 1;
	return 0;
}

const groupsInSelectedSchool = ref<IPasswordGroup[]>([]);
watch(selectedSchoolId, () => {
	if (!selectedSchoolId.value) {
		groupsInSelectedSchool.value = [];
	} else {
		const groups = fetchedGroups.value.filter(
			(group) => group.schoolId === selectedSchoolId.value
		);
		// Add group title to first of each group
		const titles = { ...groupHeaders };
		groupsInSelectedSchool.value = groups
			.sort(sortGroupsByType)
			.map((group) => {
				const groupType = group.type as TemplateConnectionType;
				let groupTitle;
				if (groupType in titles) {
					groupTitle = titles[groupType];
					delete titles[groupType];
				}
				return { ...group, groupTitle };
			});
	}
});

const selectedGroupId = ref<string | null>(null);
const selectedGroupName = ref<string | null>(null);
function groupSelected(id: string) {
	selectedGroupId.value = id;
	selectedGroupName.value =
		groupsInSelectedSchool.value.find((group) => group.id === id)?.name ??
		null;
}

async function fetchPasswords() {
	isBusyFetchingPasswords.value = true;

	fetchedPasswords.value = await store.dispatch(
		DispatchType.GetDefaultPasswordAssignments,
		{ groupId: selectedGroupId.value }
	);

	isBusyFetchingPasswords.value = false;
}

watch(selectedGroupId, () => {
	if (selectedGroupId.value) {
		fetchPasswords();
	} else {
		fetchedPasswords.value = [];
	}
});

// Print
function printClicked() {
	window.print();
}

function updateSortBy(value: ISortBy[]) {
	sortTableBy.value = value;
}

// Initial setup
function sort_asc(
	a: IPasswordSchool | IPasswordGroup,
	b: IPasswordSchool | IPasswordGroup
) {
	if (a.name < b.name) return -1;
	if (a.name > b.name) return 1;
	return 0;
}

onMounted(async () => {
	isBusyFetchingSchoolsAndClasses.value = true;

	const data = await store.dispatch(DispatchType.GetConsumerGroupsAndSchools);
	fetchedSchools.value = data.schools.sort(sort_asc);
	fetchedGroups.value = data.groups.sort(sort_asc);

	isBusyFetchingSchoolsAndClasses.value = false;
});

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.filter {
	display: flex;
	flex-wrap: wrap;
	justify-content: space-between;
	align-items: end;
}
.b-auto {
	flex-basis: 250px;
	max-width: 300px;
	margin: 0px;
	margin-top: 3%;
	margin-right: 3%;
}
.print-btn {
	border: thin solid $grey-lighten-6;
	height: 40px;
	margin: 0px;
	text-transform: none;
	font-size: 16px;
	margin-top: 3%;
}
.dropdowns {
	flex-grow: 4;
	display: flex;
	flex-wrap: wrap;
	margin: 0px;
}

@media print {
	.default-passwords {
		display: none;
	}
}
</style>
