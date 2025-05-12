<template>
	<v-dialog
		v-model="showModal"
		scrollable
		:max-width="fullScreenModal ? undefined : 820"
		:fullscreen="fullScreenModal"
		aria-live="polite"
	>
		<v-card class="kvittens-gdpr-modal">
			<v-card-text class="content">
				<div v-html="gdprText"></div>
			</v-card-text>
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
import { useDisplay } from 'vuetify';

const props = defineProps({
	modelValue: {
		type: Boolean,
		required: true,
	},
	gdprText: {
		type: String,
		required: true,
	},
});
const emit = defineEmits(['update:modelValue', 'answerChanged']);

const showModal = computed({
	get: () => props.modelValue,
	set: (show) => emit('update:modelValue', show),
});
const { width } = useDisplay();
const fullScreenModal = computed(() => width.value < 820);
</script>

<style scoped lang="scss">
.kvittens-gdpr-modal {
	:deep(.content) {
		padding-bottom: 30px;
		display: flex;
		flex-direction: column;
		flex: 1;
		overflow-y: auto;
		max-height: calc(80vh - 100px);

		h2 {
			font-size: size(20);
			margin-top: 10px;
		}
		p {
			margin: 10px 0 20px;
		}
		ul,
		ol {
			list-style-position: inside;
			margin: 10px 0 20px;

			li {
				margin-bottom: 5px;
			}
		}
	}
}
</style>
<style lang="scss">
.v-dialog--fullscreen .kvittens-gdpr-modal .content {
	max-height: none;
}
</style>
