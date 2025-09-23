<template>
	<v-dialog
		v-model="showDialog"
		:max-width="fullScreenModal ? undefined : 800"
		:fullscreen="fullScreenModal"
	>
		<v-card
			:title="
				t('component.internal.kvittensAgent.dialogTitle', {
					name: kvittensDetails?.personName,
				})
			"
			:subtitle="kvittensDetails?.title"
		>
			<app-loading-spinner
				v-if="isBusyFetchingData"
				:isVisible="isBusyFetchingData"
			></app-loading-spinner>
			<v-card-text v-else-if="kvittensDetails">
				<base-expandable-content>
					<h3>
						{{
							t(
								'component.internal.kvittensAgent.dialogDetailsTitle'
							)
						}}
					</h3>

					<div v-html="kvittensDetails.text"></div>
				</base-expandable-content>

				<kvittens-agent-register
					:linked-persons="kvittensDetails.linkedPersons"
					:subject-ssno="kvittensDetails.personSSNo"
					:template-id="kvittensDetails.templateId"
					@update:kvittens="
						(updatedKvittens) => (kvittensDetails = updatedKvittens)
					"
				></kvittens-agent-register>
				<div v-if="kvittensDetails.history.length">
					<h3 class="mb-2">
						{{ t('component.internal.kvittensAgent.historyTitle') }}
					</h3>
					<kvittens-history
						:history="kvittensDetails.history"
					></kvittens-history>
				</div>
			</v-card-text>
			<v-card-actions>
				<v-spacer></v-spacer>
				<v-btn
					:text="
						t('component.internal.kvittensAgent.closeDialogButton')
					"
					@click="showDialog = false"
				></v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import BaseExpandableContent from '@/components/base/BaseExpandableContent.vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import KvittensAgentRegister from './KvittensAgentRegister.vue';
import KvittensHistory from '@/components/external/kvittens/KvittensHistory.vue';
import { DispatchType } from '@/models/Enums';
import { IRootState } from '@/models/Interfaces';
import { IKvittensDetails } from '@/models/kvittens/Interfaces';
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import { useDisplay } from 'vuetify';
import ErrorService from '@/utils/ErrorService';

const props = defineProps<{
	modelValue: { studentSsno: string; templateId: string } | null;
}>();

const emit = defineEmits(['update:modelValue']);

const { t } = useI18n();
const store = useStore<IRootState>();

const isBusyFetchingData = ref(false);
const kvittensDetails = ref<IKvittensDetails>();

const showDialog = computed({
	get: () => !!props.modelValue,
	set: (isVisible: boolean) => {
		if (!isVisible) {
			emit('update:modelValue', null);
		}
	},
});
const { width } = useDisplay();
const fullScreenModal = computed(() => width.value < 820);

const fetchKvittensDetails = async () => {
	if (!props.modelValue) return;

	isBusyFetchingData.value = true;
	try {
		kvittensDetails.value = await store.dispatch(
			DispatchType.GetAgentKvittensDetails,
			{
				studentSsno: props.modelValue.studentSsno,
				templateId: props.modelValue.templateId,
			}
		);
	} catch (err) {
		ErrorService.onError({ err });
	} finally {
		isBusyFetchingData.value = false;
	}
};

watch(
	() => props.modelValue,
	(newValue, oldValue) => {
		if (newValue && !oldValue) {
			fetchKvittensDetails();
		}
	}
);
</script>

<style scoped lang="scss">
.v-card-text {
	overflow-y: auto;
}
</style>
