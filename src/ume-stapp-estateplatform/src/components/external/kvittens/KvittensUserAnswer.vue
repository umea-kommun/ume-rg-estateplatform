<template>
	<v-chip
		class="kvittens-user-answer"
		:class="{
			approved: userHasAnswered,
			unanswered: !userHasAnswered,
		}"
		variant="outlined"
		>{{ answerText }}</v-chip
	>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps({
	userHasAnswered: {
		required: true,
		type: Boolean,
	},
	statusOfSelf: {
		type: Boolean,
		default: false,
	},
});

const { t } = useI18n();

const answerText = computed(() => {
	if (props.userHasAnswered) {
		if (props.statusOfSelf) {
			return t('component.kvittensUserAnswer.iApproved');
		} else {
			return t('component.kvittensUserAnswer.approved');
		}
	} else {
		if (props.statusOfSelf) {
			return t('component.kvittensUserAnswer.iHaveNotAnswered');
		} else {
			return t('component.kvittensUserAnswer.unanswered');
		}
	}
});
</script>

<style scoped lang="scss">
.v-chip {
	height: auto;
	padding: 2px 12px;
	font-size: size(16);
	&.approved {
		color: $white;
		background-color: $primary;
	}
	&.unanswered {
		color: $black;
		border: solid 1px $grey-lighten-5;
		background-color: $grey-lighten-3;
	}
}
</style>
