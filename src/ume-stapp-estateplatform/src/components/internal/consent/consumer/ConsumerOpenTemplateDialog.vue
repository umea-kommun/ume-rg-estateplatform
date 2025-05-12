<template>
	<v-dialog
		v-model="showDialog"
		scrollable
		:max-width="700"
		aria-live="polite"
		class="consumer-open-template-dialog"
	>
		<v-card>
			<v-card-title>
				{{
					$t(
						'component.internal.consentConsumerList.openDialog.title'
					)
				}}</v-card-title
			>
			<v-card-text class="pt-0">
				<consent-consumer-group-filter
					:schools="schools"
					:groups="groups"
					v-model:selectedSchoolId="selectedSchoolId"
					v-model:selectedGroupId="selectedGroupId"
					:show-search="false"
				/>
				<p>
					{{
						$t(
							'component.internal.consentConsumerList.openDialog.description'
						)
					}}
				</p>
			</v-card-text>
			<v-card-actions>
				<hr class="mb-4 mt-4" />
				<v-spacer />
				<v-btn @click="templateId = null">{{
					$t('app.nav.cancel')
				}}</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { computed, PropType, ref, watch } from 'vue';
import { MyPagesRoutes } from '@/router/routes';
import ConsentConsumerGroupFilter from './ConsentConsumerGroupFilter.vue';
import { IConsumerGroup } from '@/models/Interfaces';
import { useRouter } from 'vue-router';

const props = defineProps({
	templateIdToOpen: {
		type: String as PropType<string | null>,
	},
	schools: {
		type: Array as PropType<IConsumerGroup[]>,
		required: true,
	},
	groups: {
		type: Array as PropType<IConsumerGroup[]>,
		required: true,
	},
	filterSelectedSchoolId: {
		type: String,
	},
});

const emit = defineEmits(['update:templateIdToOpen']);
const router = useRouter();

const selectedSchoolId = ref<string | null>(null);
const selectedGroupId = ref<string | null>(null); // TODO: auto open template when group is selected?

const templateId = computed({
	get: () => props.templateIdToOpen ?? null,
	set: (newValue) => emit('update:templateIdToOpen', newValue),
});

const showDialog = computed({
	get: () => !!templateId.value,
	set: (newValue) => {
		if (!newValue) {
			templateId.value = null;
		}
	},
});

watch(
	() => selectedGroupId.value,
	(groupId) => {
		if (groupId) {
			router.push({
				name: MyPagesRoutes.InternalConsentConsumerDetails,
				params: {
					templateGuid: templateId.value,
					groupId: groupId,
				},
			});
		}
	}
);

watch(showDialog, (show) => {
	if (show && props.schools.length === 1) {
		selectedSchoolId.value = props.schools[0].refId;
	} else {
		selectedSchoolId.value = props.filterSelectedSchoolId ?? null;
		selectedGroupId.value = null;
	}
});
watch(
	() => props.filterSelectedSchoolId,
	(newSchoolId) => {
		selectedSchoolId.value = newSchoolId ?? null;
	}
);
</script>

<style scoped lang="scss">
.consumer-open-template-dialog .v-card {
	.v-card-title,
	.v-card-text,
	.v-card-actions {
		padding: 16px;

		flex-wrap: wrap;
	}
}
</style>
