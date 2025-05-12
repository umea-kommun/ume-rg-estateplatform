<template>
	<div class="template-group-select">
		<Field
			:name="id"
			:label="label"
			v-model="selectedGroups"
			v-slot="{ errors }"
			type="select"
			:rules="rules"
			:keepValue="true"
		>
			<base-form-field
				:id="'label' + id"
				:labelFor="id"
				:label="label"
				:is-required="rules.indexOf('required') > -1"
				:errorDisplay="!!errors.length"
			>
				<slot v-if="!selectedGroups.length" name="empty"></slot>
				<v-chip
					v-for="group in selectedGroups"
					:key="group.refId"
					:closable="!disabled"
					:disabled="disabled"
					@click:close="removeGroup(group.refId)"
					>{{ group.title }}</v-chip
				>
				<slot name="addButton" :onClick="addButtonClick">
					<v-btn
						:id="id"
						class="add-button"
						variant="text"
						color="primary"
						:disabled="disabled"
						@click="addButtonClick"
						prepend-icon="add"
						>{{ addLabel }}</v-btn
					>
				</slot>
				<BaseHelpText :getValidationId="id" :errors="[...errors]" />
			</base-form-field>
		</Field>

		<v-dialog v-model="showDialog" width="500">
			<v-card v-if="showDialog">
				<v-card-title>
					{{ addLabel }}
				</v-card-title>

				<v-card-text>
					<base-select-list
						id="template-select-unitType"
						:label="
							$t(
								'component.internal.templateGroupSelect.modal.unitTypeTitle'
							)
						"
						v-model="modalUnitType"
						:items="unitTypes"
						item-title="name"
						item-value="refId"
						:disabled="disabled || isBusyFetchingUnitTypes"
						:return-object="true"
						:loading="isBusyFetchingUnitTypes"
						rules="required"
					/>

					<base-select-list
						id="template-select-unit"
						:label="
							$t(
								'component.internal.templateGroupSelect.modal.unitTitle'
							)
						"
						v-model="modalUnit"
						:items="filteredUnits"
						item-value="refId"
						:disabled="
							disabled || isBusyFetchingUnits || !modalUnitType
						"
						:return-object="true"
						:loading="isBusyFetchingUnits"
					/>
					<base-select-list
						id="template-select-class"
						:label="
							$t(
								'component.internal.templateGroupSelect.modal.classTitle'
							)
						"
						v-model="modalClass"
						:items="schoolClasses"
						item-value="refId"
						:disabled="
							disabled ||
							isBusyFetchingUnits ||
							!modalUnit ||
							modalUnit?.refId === defaultAllSchools.refId ||
							isBusyFetchingClasses
						"
						:return-object="true"
						:loading="isBusyFetchingClasses"
					>
						<template v-slot:item="{ props, item }">
							<v-list-item
								v-bind="props"
								:title="item.title"
								:subtitle="
									classPeriod(
										item.raw as IConsentTemplateGroup
									)
								"
							></v-list-item>
						</template>
					</base-select-list>
				</v-card-text>

				<v-divider></v-divider>

				<v-card-actions>
					<v-spacer></v-spacer>

					<v-btn @click="showDialog = false">
						{{
							$t(
								'component.internal.templateGroupSelect.modal.cancel'
							)
						}}</v-btn
					>
					<v-btn
						color="primary"
						@click="addGroup"
						:disabled="!modalUnitType"
					>
						{{
							modalAddBtnLabel ??
							$t(
								'component.internal.templateGroupSelect.modal.add'
							)
						}}</v-btn
					>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>
<script setup lang="ts">
import { ref, computed, onMounted, PropType, watch, onUnmounted } from 'vue';
import { useStore } from 'vuex';
import {
	IRootState,
	IConsentTemplateGroup,
	IConsentTemplateUnitType,
} from '@/models/Interfaces';
import { DispatchType, TemplateConnectionType } from '@/models/Enums';
import BaseSelectList from '@/components/base/BaseSelectList.vue';
import { Field } from 'vee-validate';
import BaseHelpText from '@/components/base/BaseHelpAndErrorText.vue';
import BaseFormField from '@/components/base/BaseFormField.vue';
import { Helper } from '@/utils/helper';
import { useI18n } from 'vue-i18n';
import moment from 'moment';

const { t } = useI18n();
const store = useStore<IRootState>();

const props = defineProps({
	id: { type: String, required: true },
	modelValue: {
		type: Array as PropType<IConsentTemplateGroup[]>,
		required: true,
	},
	disabled: {
		type: Boolean,
		default: false,
	},
	label: String,
	addLabel: String,
	modalAddBtnLabel: String,
	rules: {
		type: String,
		default: '',
	},
});

const emit = defineEmits(['update:modelValue']);

const showDialog = ref(false);
const isBusyFetchingUnitTypes = ref(false);
const isBusyFetchingUnits = ref(false);
const isBusyFetchingClasses = ref(false);
const modalUnitType = ref<IConsentTemplateUnitType>();

const defaultAllSchools = {
	title: t('component.internal.templateGroupSelect.modal.allSchools'),
	refId: 'all-schools',
	type: TemplateConnectionType.Unit,
};
const modalUnit = ref<IConsentTemplateGroup>(defaultAllSchools);

const defaultAllClasses = {
	title: t('component.internal.templateGroupSelect.modal.allClasses'),
	refId: 'all-classes',
	type: TemplateConnectionType.Class,
};
const modalClass = ref<IConsentTemplateGroup>(defaultAllClasses);
const schoolClasses = ref<IConsentTemplateGroup[]>([defaultAllClasses]);

const unitTypes = computed(() => {
	return (
		store.state?.consentTemplateUnitTypes
			?.filter((schoolFormAbbr) => {
				switch (schoolFormAbbr) {
					case 'FS':
					case 'FK':
					case 'GR':
					case 'S':
					case 'GY':
					case 'GS':
					case 'PC':
						return true;
					default:
						return false;
				}
			})
			.map((schoolFormAbbr) => ({
				name: t('schoolForm.' + schoolFormAbbr),
				refId: schoolFormAbbr,
				type: TemplateConnectionType.Skolform,
				key: schoolFormAbbr,
			}))
			.sort((a, b) => a.name.localeCompare(b.name)) ?? []
	);
});

const units = computed(() => {
	return store.state?.consentTemplateGroups ?? [];
});

const filteredUnits = computed(() => {
	const unitList = units.value.filter(
		(unit) =>
			modalUnitType.value &&
			unit.schoolTypes?.includes(modalUnitType.value.key)
	);
	unitList.unshift(defaultAllSchools);
	return unitList;
});

watch(modalUnit, async () => {
	// If school is changed, reset selected class
	modalClass.value = defaultAllClasses;

	if (
		modalUnitType.value &&
		modalUnit.value &&
		modalUnit.value.refId !== defaultAllSchools.refId
	) {
		// If a school is selected we want to fetch classes for that school
		isBusyFetchingClasses.value = true;

		const fetchingRefId = modalUnit.value.refId;
		const unorderedClasses = await store.dispatch(
			DispatchType.GetConsentTemplateUnitGroups,
			{
				schoolId: fetchingRefId,
				schoolForms: [modalUnitType.value.key],
			}
		);

		// Make sure the selected unit is still the same as when we started fetching (so the user hasn't changed it)
		// This could be improved so we cancel any class requests if they change unit
		if (fetchingRefId === modalUnit.value.refId) {
			const orderedClasses = unorderedClasses.sort(Helper.sortByTitle);
			orderedClasses.unshift(defaultAllClasses);
			schoolClasses.value = orderedClasses;
		}

		isBusyFetchingClasses.value = false;
	}
});

watch(modalUnitType, (newUnitType, oldUnitType) => {
	if (oldUnitType?.refId !== newUnitType?.refId) {
		// If school unit type is changed, reset selected school and class
		modalUnit.value = defaultAllSchools;
		modalClass.value = defaultAllClasses;
	}
});

const selectedGroups = computed({
	get: () => props.modelValue,
	set: (groups: IConsentTemplateGroup[]) => emit('update:modelValue', groups),
});

const removeGroup = (refId: string): void => {
	selectedGroups.value = selectedGroups.value.filter(
		(group) => group.refId !== refId
	);
};
const addGroup = (): void => {
	let group: IConsentTemplateGroup | undefined;
	if (
		modalClass.value &&
		modalClass.value.refId !== defaultAllClasses.refId
	) {
		// Adding a specific class in a school
		group = {
			...modalClass.value,
			title: modalUnit.value.title + ' - ' + modalClass.value.title,
		};
	} else if (
		modalUnit.value &&
		modalUnit.value.refId !== defaultAllSchools.refId
	) {
		// Adding a school (unit)
		group = { ...modalUnit.value };
	} else if (modalUnitType.value) {
		// Adding a school type (all schools of a type)
		group = {
			refId: modalUnitType.value.refId,
			title: modalUnitType.value.name + ' - ' + defaultAllSchools.title,
			type: modalUnitType.value.type,
		};
	}

	// Don't add it if it is already selected.
	if (group && !selectedGroups.value.find((g) => g.refId === group?.refId)) {
		selectedGroups.value = [...selectedGroups.value, group];
	}
	modalClass.value = defaultAllClasses;
	showDialog.value = false;
};

const addButtonClick = (): void => {
	showDialog.value = true;
};

const fetchSchoolUnitTypes = async () => {
	// Fetch school unit types if we don't have them in store
	if (!unitTypes.value.length) {
		isBusyFetchingUnitTypes.value = true;
		await store.dispatch(DispatchType.GetConsentTemplateUnitTypes);
		isBusyFetchingUnitTypes.value = false;
	}
};

const fetchSchoolUnits = async () => {
	// Fetch school units if we don't already have them in store
	if (!units.value.length) {
		isBusyFetchingUnits.value = true;
		await store.dispatch(DispatchType.GetConsentTemplateUnits);
		isBusyFetchingUnits.value = false;
	}
};

const classPeriod = (item: IConsentTemplateGroup): string | undefined => {
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

onMounted(async () => {
	Helper.fixSelectMenuFlicker(true);

	await Promise.all([fetchSchoolUnitTypes(), fetchSchoolUnits()]);
});

onUnmounted(() => {
	Helper.fixSelectMenuFlicker(false);
});
</script>
<style scoped lang="scss">
.template-group-select {
	.v-chip {
		font-size: size(16);
		margin: 6px 8px 6px 0px;

		&--disabled {
			opacity: 0.8;
		}
	}
	.add-button {
		letter-spacing: normal;
		text-transform: none;
		font-size: size(16);

		:deep(.v-btn__prepend) {
			margin-top: 4px;
		}
	}
}
</style>
