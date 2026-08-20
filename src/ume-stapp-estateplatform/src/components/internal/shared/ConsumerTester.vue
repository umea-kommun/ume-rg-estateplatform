<template>
	<div v-if="userIsTester && showTesterButton" class="consumer-tester">
		<v-btn
			class="tester-button"
			size="large"
			:prepend-icon="showModal ? 'expand_more' : 'expand_less'"
			@click="showModal = !showModal"
			:title="
				testingAsPerson
					? $t(
							'component.internal.consumerTester.testingAsPersonAtSchool',
							{
								name: testingAsPerson.name,
								school: testingAsPerson.unitTitle,
							}
					  )
					: ''
			"
		>
			{{
				$t('component.internal.consumerTester.testingAs', {
					name: testingAsPerson
						? testingAsPerson.name
						: $t('component.internal.consumerTester.yourself'),
				})
			}}
		</v-btn>
		<v-dialog class="consumer-tester-modal" v-model="showModal" width="500">
			<v-card>
				<v-card-title>{{
					$t('component.internal.consumerTester.modal.title')
				}}</v-card-title>

				<v-card-text>
					<p class="mb-4">
						{{
							$t(
								'component.internal.consumerTester.modal.description'
							)
						}}
					</p>
					<base-select-list
						id="tester-school"
						:label="
							$t('component.internal.consumerTester.modal.school')
						"
						v-model="selectedUnit"
						:loading="isBusyFetchingUnits"
						:disabled="isBusyFetchingUnits"
						:items="units"
						item-value="refId"
						:return-object="true"
					/>
					<base-select-list
						id="tester-person"
						:label="
							$t('component.internal.consumerTester.modal.person')
						"
						:loading="isBusyFetchingTeacher"
						:disabled="isBusyFetchingTeacher || !selectedUnit"
						v-model="selectedTeacher"
						:items="unitTeachers"
						item-value="socialSecurityNumber"
						item-title="name"
						:return-object="true"
					/>
				</v-card-text>

				<v-divider></v-divider>

				<v-card-actions>
					<v-btn
						v-if="testingAsPerson"
						@click="stopTest"
						color="error"
					>
						{{
							$t(
								'component.internal.consumerTester.modal.cancelTest'
							)
						}}</v-btn
					>
					<v-btn
						v-if="!testingAsPerson"
						@click="showTesterButton = false"
					>
						{{
							$t(
								'component.internal.consumerTester.modal.hideButton'
							)
						}}</v-btn
					>
					<v-spacer></v-spacer>

					<v-btn @click="showModal = false">
						{{
							$t('component.tConfirmDialog.cancelDefault')
						}}</v-btn
					>
					<v-btn
						color="primary"
						@click="saveTestAs"
						:disabled="
							!selectedTeacher ||
							selectedTeacher.socialSecurityNumber ===
								testingAsPerson?.socialSecurityNumber
						"
					>
						{{
							$t(
								'component.internal.consumerTester.modal.confirm'
							)
						}}</v-btn
					>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>

<script setup lang="ts">
import BaseSelectList from '@/components/base/BaseSelectList.vue';
import Config from '@/Config';
import { DispatchType, MutationType } from '@/models/Enums';
import {
	IConsentTemplateGroup,
	IRootState,
	ITesterTestAsPerson,
} from '@/models/Interfaces';
import { computed, onMounted, ref, watch } from 'vue';
import { useStore } from 'vuex';

const store = useStore<IRootState>();

const showTesterButton = ref(true);
const showModal = ref(false);

const isBusyFetchingUnits = ref(false);
const isBusyFetchingTeacher = ref(false);
const selectedUnit = ref<IConsentTemplateGroup>();
const unitTeachers = ref<ITesterTestAsPerson[]>([]);
const selectedTeacher = ref<ITesterTestAsPerson>();

const testingAsPerson = computed(() => {
	return store.state.tester?.testAsPerson;
});

const units = computed(() => {
	return store.state?.tester?.schoolUnits ?? [];
});

// If the logged in user is member of the tester group
const userIsTester = computed(() => {
	return (
		(store.state.user.groups ?? []).indexOf(
			Config.VUE_APP_AUTH_GROUP_TESTER_ID
		) > -1
	);
});

const stopTest = (): void => {
	showModal.value = false;
	store.commit(MutationType.SetTesterTestAs, undefined);
	location.reload();
};
const saveTestAs = (): void => {
	showModal.value = false;
	if (selectedTeacher.value) {
		store.commit(MutationType.SetTesterTestAs, selectedTeacher.value);

		// Reload window to fetch data as the test person
		location.reload();
	}
};

watch(selectedUnit, async () => {
	if (selectedUnit.value) {
		// If a school is selected we want to fetch teachers for that school
		isBusyFetchingTeacher.value = true;

		const fetchingSchoolId = selectedUnit.value.refId;
		const unorderedTeachers: {
			name: string;
			socialSecurityNumber: string;
		}[] = await store.dispatch(
			DispatchType.GetTesterSchoolTeachers,
			fetchingSchoolId
		);

		// Make sure the selected unit is still the same as when we started fetching (so the user hasn't changed it)
		if (fetchingSchoolId === selectedUnit.value.refId) {
			const orderedTeachers: ITesterTestAsPerson[] = unorderedTeachers
				.map((teacher) => ({
					name: teacher.name,
					socialSecurityNumber: teacher.socialSecurityNumber,
					unitRefId: selectedUnit.value?.refId ?? '',
					unitTitle: selectedUnit.value?.title ?? '',
				}))
				.sort((a, b) => a.name.localeCompare(b.name));
			unitTeachers.value = orderedTeachers;
		}

		isBusyFetchingTeacher.value = false;
	}
	if (selectedUnit.value?.refId !== selectedTeacher.value?.unitRefId) {
		selectedTeacher.value = undefined;
	}
});

onMounted(async () => {
	if (userIsTester.value) {
		// Fetch school units if we don't already have them in store
		if (!units.value.length) {
			isBusyFetchingUnits.value = true;
			await store.dispatch(DispatchType.GetTesterSchoolUnits);
			isBusyFetchingUnits.value = false;
		}
		if (testingAsPerson.value) {
			// Testing person already selected, update modal fields to match
			selectedUnit.value = units.value.find(
				(unit) => unit.refId === testingAsPerson.value?.unitRefId
			);
			selectedTeacher.value = testingAsPerson.value;
		}
	}
});
</script>

<style scoped lang="scss">
.consumer-tester {
	.tester-button {
		font-size: size(16);
		text-transform: none;
		letter-spacing: normal;
		max-width: 80%;
		position: fixed;
		bottom: 0;
		right: 0;
		margin: 0;
		z-index: 100;
		background-color: #fff;
		padding: 10px;
		border-radius: 8px 0 0 0;
		overflow: hidden;
		height: auto;

		:deep(.v-btn__content) {
			white-space: break-spaces;
		}
	}
}
</style>

<style lang="scss">
.consumer-tester-modal {
	p {
		letter-spacing: normal;
	}
}
</style>
