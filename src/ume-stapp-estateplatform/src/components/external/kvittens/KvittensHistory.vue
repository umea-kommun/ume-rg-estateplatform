<template>
	<div
		v-for="historyPost in sortedHistory"
		:key="historyPost.date + historyPost.name"
		class="kvittens-history pt-4 pb-4"
	>
		<div>
			{{
				$t('component.external.kvittensDetails.history.answeredYes', {
					name: historyPost.name,
				})
			}}
		</div>
		<div>
			{{
				moment
					.utc(historyPost.date)
					.local()
					.format('Do MMMM YYYY, HH:mm')
			}}
		</div>
	</div>
</template>

<script setup lang="ts">
import { IKvittensHistory } from '@/models/kvittens/Interfaces';
import { PropType, computed } from 'vue';
import moment from 'moment';

const props = defineProps({
	history: {
		type: Array as PropType<IKvittensHistory[]>,
		required: true,
	},
});

const sortedHistory = computed(() => {
	return [...props.history].sort((a, b) => {
		return moment(b.date).local().diff(moment(a.date).local());
	});
});
</script>

<style scoped lang="scss">
.kvittens-history {
	display: flex;
	justify-content: space-between;
	flex-wrap: wrap;

	border-bottom: solid 2px $grey-lighten-3;
	&:first-of-type {
		border-top: solid 2px $grey-lighten-3;
	}
}
</style>
