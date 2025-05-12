<template>
	<v-row class="filter-wrap">
		<v-col :cols="showSearch ? 4 : 6" class="pl-0">
			<v-autocomplete
				v-model="selectedSchoolId"
				:items="schools"
				:label="
					allSchoolsArePedagogicalCare
						? $t(
								'component.internal.consentConsumerList.filter.caretaker'
						  )
						: $t(
								'component.internal.consentConsumerList.filter.school'
						  )
				"
				:no-data-text="
					$t('component.internal.consentConsumerList.filter.noData')
				"
				:disabled="schools.length === 0"
				itemTitle="name"
				itemValue="refId"
				variant="outlined"
				density="comfortable"
				:clearable="schools.length > 1"
				color="primary"
				hide-details
			/>
		</v-col>
		<v-col :cols="showSearch ? 4 : 6" :class="{ 'pr-0': !showSearch }">
			<v-autocomplete
				v-model="selectedGroupId"
				:items="groupsInSelectedSchool"
				:label="
					$t(
						'component.internal.consentConsumerList.filter.classOrGroup'
					)
				"
				:no-data-text="
					$t('component.internal.consentConsumerList.filter.noData')
				"
				itemTitle="name"
				itemValue="refId"
				variant="outlined"
				density="comfortable"
				color="primary"
				:clearable="true"
				hide-details
				:disabled="!selectedSchoolId"
			>
				<template v-slot:item="{ props, item }">
					<div
						v-if="item.raw.groupTitle"
						class="pl-4 pb-1 group-title"
					>
						<b>{{ item.raw.groupTitle }}</b>
					</div>
					<v-list-item
						v-bind="props"
						:title="item.title"
						:subtitle="getGroupPeriod(item.raw)"
					></v-list-item>
				</template>
			</v-autocomplete>
		</v-col>
		<v-col cols="4" class="pr-0 pt-0" v-if="showSearch">
			<base-text-box
				id="search"
				v-model="searchValue"
				variant="outlined"
				:label="
					$t('component.internal.consentConsumerList.filter.search')
				"
				prependInnerIcon="search"
			/>
		</v-col>
	</v-row>
</template>

<script setup lang="ts">
import { IConsumerGroup } from '@/models/Interfaces';
import BaseTextBox from '@/components/base/BaseTextBox.vue';
import moment from 'moment';
import { computed, PropType, ref, watch } from 'vue';
import { TemplateConnectionType } from '@/models/Enums';
import { useI18n } from 'vue-i18n';

const props = defineProps({
	schools: {
		type: Array as PropType<IConsumerGroup[]>,
		required: true,
	},
	groups: {
		type: Array as PropType<IConsumerGroup[]>,
		required: true,
	},
	selectedSchoolId: {
		type: String as PropType<string | null>,
	},
	selectedGroupId: {
		type: String as PropType<string | null>,
	},
	searchValue: String,
	showSearch: {
		type: Boolean,
		default: true,
	},
});

const { t } = useI18n();
const emit = defineEmits([
	'update:selectedSchoolId',
	'update:selectedGroupId',
	'update:searchValue',
]);

const localSelectedSchoolId = ref<string | null>(null);
const localSelectedGroupId = ref<string | null>(null);
const localSearchValue = ref('');

const selectedSchoolId = computed({
	get: () => props.selectedSchoolId ?? localSelectedSchoolId.value,
	set: (value: string | null) => {
		localSelectedSchoolId.value = value;
		emit('update:selectedSchoolId', value);
	},
});

const selectedGroupId = computed({
	get: () => props.selectedGroupId ?? localSelectedGroupId.value,
	set: (value: string | null) => {
		emit('update:selectedGroupId', value);
		localSelectedGroupId.value = value;
	},
});

const searchValue = computed({
	get: () => props.searchValue ?? localSearchValue.value,
	set: (value: string) => {
		emit('update:searchValue', value);
		localSearchValue.value = value;
	},
});

const groupTitles = {
	[TemplateConnectionType.Department]: t(
		'component.internal.consentConsumerList.groupType.department'
	),
	[TemplateConnectionType.Class]: t(
		'component.internal.consentConsumerList.groupType.class'
	),
	[TemplateConnectionType.EducationGroup]: t(
		'component.internal.consentConsumerList.groupType.educationGroup'
	),
	[TemplateConnectionType.Unit]: t(
		'component.internal.consentConsumerList.groupType.unit'
	),
	[TemplateConnectionType.Skolform]: t(
		'component.internal.consentConsumerList.groupType.skolform'
	),
};

type GroupWithTitle = IConsumerGroup & { groupTitle?: string };
const groupsInSelectedSchool = computed<GroupWithTitle[]>(() => {
	if (!selectedSchoolId.value) {
		return [];
	}
	const groups = props.groups.filter(
		(group) => group.parents?.find((p) => p.id === selectedSchoolId.value)
	);

	// Add group title to first of each group
	const titles = { ...groupTitles };
	return groups.map((group) => {
		let groupTitle;
		if (group.type in titles) {
			groupTitle = titles[group.type];
			delete titles[group.type];
		}
		return { ...group, groupTitle };
	});
});

const getGroupPeriod = (item: GroupWithTitle): string | undefined => {
	if (!item.startDate || !item.endDate) {
		return undefined;
	}
	return `Period (${moment
		.utc(item.startDate)
		.local()
		.format('Do MMMM YYYY')} - ${moment
		.utc(item.endDate)
		.local()
		.format('Do MMMM YYYY')})`;
};

const allSchoolsArePedagogicalCare = computed(() => {
	return (
		props.schools.length > 0 &&
		props.schools.every((school) => {
			return school.parents?.every((parent) => {
				return parent.id === 'PC';
			});
		})
	);
});

watch(selectedSchoolId, (newSelectedSchool) => {
	selectedGroupId.value = null;

	if (newSelectedSchool && groupsInSelectedSchool.value.length === 1) {
		// If user only has access to one class/group in the school, preselect it
		selectedGroupId.value = groupsInSelectedSchool.value[0].refId;
	}
});
</script>

<style scoped lang="scss">
.filter-wrap {
	margin-top: 0;
	flex-wrap: nowrap;
	align-items: flex-end;
	.v-col {
		align-items: center;
		&.group-wrap {
			flex: auto;
		}

		:deep(.help-and-error-wrap) {
			margin-bottom: 0;
		}

		.create-button {
			color: $white !important;
		}
	}
}
.group-title {
	color: $primary;
}
@media only screen and (max-width: 700px) {
	.filter-wrap {
		flex-wrap: wrap;

		.v-col {
			padding-left: 0;
			padding-right: 0;
			flex: auto;
			max-width: none;
		}
	}
}
</style>
