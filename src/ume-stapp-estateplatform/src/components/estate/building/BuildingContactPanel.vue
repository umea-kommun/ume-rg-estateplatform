<template>
	<div class="building-contact-panel">
		<h2 class="mt-0 mb-2">
			{{ $t('component.buildingDetails.contactPersonsButton') }}
		</h2>
		<div v-for="person in persons" :key="person.label" class="person">
			<h3 class="role">{{ person.label }}</h3>
			<div class="value">
				<template v-if="person.name">
					<div class="name">
						<v-icon icon="person" :size="18" />
						{{ person.name }}
					</div>
					<div v-if="person.email" class="contact">
						<v-icon icon="mail" :size="18" />
						<base-auto-link-text
							:text="person.email"
							:link-aria-label="
								$t('component.buildingContact.emailAriaLabel', {
									name: person.name,
								})
							"
						/>
					</div>
					<div v-if="person.phone" class="contact">
						<v-icon icon="phone" :size="18" />
						<base-auto-link-text
							:text="person.phone"
							:link-aria-label="
								$t('component.buildingContact.callAriaLabel', {
									name: person.name,
								})
							"
						/>
					</div>
				</template>
				<div v-else class="missing">
					{{ $t('component.buildingContact.valueMissing') }}
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { IBuildingDetails } from '@/models/Interfaces';
import { useI18n } from 'vue-i18n';
import BaseAutoLinkText from '@/components/shared/BaseAutoLinkText.vue';

const props = defineProps<{
	building: IBuildingDetails;
}>();

const { t } = useI18n();

const persons = computed(() => {
	const contacts = props.building.contactPersons;

	return [
		{
			label: t('component.buildingContact.propertyManager'),
			value: contacts?.propertyManager,
		},
		{
			label: t('component.buildingContact.operationsManager'),
			value: contacts?.operationsManager,
		},
		{
			label: t('component.buildingContact.operationCoordinator'),
			value: contacts?.operationCoordinator,
		},
		{
			label: t('component.buildingContact.rentalAdministrator'),
			value: contacts?.rentalAdministrator,
		},
		{
			label: t('component.buildingContact.caretaker'),
			value: contacts?.caretaker,
		},
		{
			label: t('component.buildingContact.operationsTechnician'),
			value: contacts?.operationsTechnician,
		},
	].map((person) => ({
		label: person.label,
		name: person.value?.name?.trim() ? person.value.name : null,
		email: person.value?.email,
		phone: person.value?.phone,
	}));
});
</script>

<style scoped lang="scss">
.building-contact-panel {
	.person {
		padding: 12px 0;

		& + .person {
			border-top: solid 1px $grey-lighten-3;
		}

		.role {
			margin: 0 0 2px;
			font-size: size(17);
			font-weight: 600;
			color: $black;
		}

		.value {
			font-size: size(16);

			.name,
			.contact {
				display: flex;
				align-items: center;
				gap: 8px;
				min-width: 0;
				overflow-wrap: anywhere;
			}
			.contact {
				margin-top: 2px;
			}
			.v-icon {
				color: $grey-darken-1;
				flex-shrink: 0;
			}
			.missing {
				color: $grey-darken-1;
			}
		}
	}
}
</style>
