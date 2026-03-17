<template>
	<div class="fault-contact-info">
		<base-text-box
			id="contact-name"
			:label="
				$t('component.internal.faultReport.general.contactNameLabel')
			"
			v-model="contactName"
			rules="required"
			variant="outlined"
			rounded="lg"
		/>
		<div class="contact-fields">
			<base-text-box
				id="contact-email"
				:label="
					$t(
						'component.internal.faultReport.general.contactEmailLabel'
					)
				"
				v-model="contactEmail"
				rules="required|email"
				variant="outlined"
				rounded="lg"
			/>
			<base-text-box
				id="contact-phone"
				:label="
					$t(
						'component.internal.faultReport.general.contactPhoneLabel'
					)
				"
				v-model="contactPhone"
				rules="phone"
				variant="outlined"
				rounded="lg"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import BaseTextBox from '@/components/base/BaseTextBox.vue';
import { computed } from 'vue';

const props = defineProps<{
	contactName: string;
	contactEmail: string;
	contactPhone: string;
}>();

const emit = defineEmits([
	'update:contactName',
	'update:contactEmail',
	'update:contactPhone',
]);

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
