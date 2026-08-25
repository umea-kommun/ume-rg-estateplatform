<template>
	<div class="fault-contact-info">
		<base-text-box
			id="contact-name"
			:label="$t('component.faultReport.general.contactNameLabel')"
			v-model="contactName"
			rules="required"
			variant="outlined"
			rounded="lg"
			:error-message="nameError"
		/>
		<div class="contact-fields">
			<base-text-box
				id="contact-email"
				:label="$t('component.faultReport.general.contactEmailLabel')"
				v-model="contactEmail"
				rules="required|email"
				variant="outlined"
				rounded="lg"
				:error-message="emailError"
			/>
			<base-text-box
				id="contact-phone"
				:label="$t('component.faultReport.general.contactPhoneLabel')"
				v-model="contactPhone"
				rules="required|phone"
				variant="outlined"
				rounded="lg"
				:error-message="phoneError"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import BaseTextBox from '@/components/shared/BaseTextBox.vue';
import { computed, type ComputedRef } from 'vue';
import { useWorkOrderDefaults } from '@/utils/useWorkOrderDefaults';

const props = defineProps<{
	contactName: string;
	contactEmail: string;
	contactPhone: string;
	fieldError: (field: string) => ComputedRef<string>;
}>();

const emit = defineEmits([
	'update:contactName',
	'update:contactEmail',
	'update:contactPhone',
]);

const nameError = props.fieldError('notifierName');
const emailError = props.fieldError('notifierEmail');
const phoneError = props.fieldError('notifierPhone');

const contactName = computed({
	get: () => props.contactName,
	set: (newValue: string) => emit('update:contactName', newValue),
});

const contactEmail = computed({
	get: () => props.contactEmail,
	set: (newValue: string) => emit('update:contactEmail', newValue),
});

const contactPhone = computed({
	get: () => props.contactPhone,
	set: (newValue: string) => emit('update:contactPhone', newValue),
});

// Prefill the phone from the user's most recent work order submission. Lives here
// so every form that renders this component (fault report, order, space requirement)
// gets the prefill automatically, rather than each parent wiring it up separately.
useWorkOrderDefaults(contactPhone);
</script>

<style lang="scss" scoped>
.fault-contact-info {
	.contact-fields {
		display: flex;
		flex-wrap: wrap;
		gap: 16px;

		> .base-text-box {
			flex: 1;
		}

		@media only screen and (max-width: 700px) {
			display: block;
		}
	}
}
</style>
