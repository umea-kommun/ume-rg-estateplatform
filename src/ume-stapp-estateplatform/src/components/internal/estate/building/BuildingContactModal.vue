<template>
	<v-dialog
		v-model="showModal"
		class="building-contact-modal estate-default"
		:max-width="820"
		aria-labelledby="modal-title"
	>
		<v-card>
			<div class="content">
				<v-card-title id="modal-title" class="px-6">
					{{ $t('component.internal.buildingContact.title') }}
				</v-card-title>
				<v-card-text class="px-6 pt-2">
					<div class="persons-wrap">
						<div
							v-for="person in persons"
							:key="person.label + person.value"
							class="person"
						>
							<h3>
								{{ person.label }}
							</h3>
							<div class="value">
								{{
									person.value ??
									$t(
										'component.internal.buildingContact.valueMissing'
									)
								}}
							</div>
						</div>
					</div>
				</v-card-text>
			</div>
			<v-card-actions>
				<hr class="mb-4 mt-4" />
				<v-btn @click="showModal = false">{{
					$t('app.nav.close')
				}}</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { IBuildingDetails } from '@/models/estate/Interfaces';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	modelValue: boolean;
	building: IBuildingDetails;
}>();

const emit = defineEmits(['update:modelValue']);

const { t } = useI18n();

const showModal = computed({
	get: () => props.modelValue,
	set: (show) => emit('update:modelValue', show),
});

const persons = computed(() => {
	return [
		{
			label: t('component.internal.buildingContact.propertyManager'),
			value: props.building.contactPersons?.propertyManager,
		},
		{
			label: t('component.internal.buildingContact.operationsManager'),
			value: props.building.contactPersons?.operationsManager,
		},
		{
			label: t('component.internal.buildingContact.operationCoordinator'),
			value: props.building.contactPersons?.operationCoordinator,
		},
		{
			label: t('component.internal.buildingContact.rentalAdministrator'),
			value: props.building.contactPersons?.rentalAdministrator,
		},
	];
});
</script>

<style scoped lang="scss">
.content {
	overflow-y: auto;
	max-height: calc(80vh - 100px);

	.persons-wrap {
		display: flex;
		flex-wrap: wrap;

		gap: 24px 32px;

		.person {
			flex: 11 45%;
			.value {
				font-size: size(16);
			}
		}
	}
}
</style>
