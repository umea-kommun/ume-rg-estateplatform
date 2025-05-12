<template>
	<v-chip
		class="kvittens-answer"
		:class="{
			approved: status === KvittensStatus.Approved,
			unanswered: status !== KvittensStatus.Approved,
		}"
		variant="outlined"
		>{{ answerText }}</v-chip
	>
</template>

<script setup lang="ts">
import { KvittensStatus } from '@/models/kvittens/Enums';
import { PropType, computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps({
	status: {
		required: true,
		type: Number as PropType<KvittensStatus>,
	},
});

const { t } = useI18n();

const answerText = computed(() => {
	switch (props.status) {
		case KvittensStatus.Approved:
			return t('component.kvittensUserAnswer.approved');
		case KvittensStatus.NotAnsweredByAll:
			return t('component.kvittensUserAnswer.notAnsweredByAll');
		default:
			return t('component.kvittensUserAnswer.unanswered');
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
