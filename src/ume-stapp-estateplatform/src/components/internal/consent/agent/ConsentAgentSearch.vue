<template>
	<v-card flat>
		<v-card-text class="pa-0">
			<Form @submit="childSearch">
				<base-text-box
					id="child-ssn"
					v-model="childSSN"
					:label="
						$t(
							'component.internal.consentAgentSearch.childSSNTitle'
						)
					"
					rules="required|validPersNumber"
					:helpText="
						$t(
							'component.internal.consentAgentSearch.childSSNHelpText'
						)
					"
					class="mr-4"
				/>
				<v-btn
					type="submit"
					flat
					color="primary"
					class="ma-0 mb-2"
					size="large"
				>
					{{ $t('component.internal.consentAgentSearch.search') }}
				</v-btn></Form
			>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
import BaseTextBox from '@/components/base/BaseTextBox.vue';
import Organisationsnummer from 'organisationsnummer';
import { Form } from 'vee-validate';
import { ref } from 'vue';

const emit = defineEmits(['child-search']);

const childSSN = ref('');

const formatSSN = (rawSSN: string): string => {
	if (
		Organisationsnummer.valid(rawSSN) &&
		Organisationsnummer.parse(rawSSN).isPersonnummer()
	) {
		const ssn = Organisationsnummer.parse(rawSSN).personnummer();
		if (ssn) {
			return `${ssn.fullYear}${ssn.month}${ssn.day}${ssn.num}${ssn.check}`;
		}
	}

	// If not a valid SSN (temporary SSN etc) we still want to remove the dash if its 12 digits
	if (rawSSN.indexOf('-') === 8) {
		return rawSSN.replace('-', '');
	}
	return rawSSN;
};

function childSearch() {
	if (childSSN.value) {
		emit('child-search', formatSSN(childSSN.value));
	}
}
</script>

<style scoped lang="scss">
.v-card {
	.v-card-text form {
		display: flex;
		flex-direction: row;
		align-items: center;
		flex-wrap: wrap;
		.base-text-box {
			flex: auto;
		}
	}
}
</style>
