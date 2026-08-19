<template>
	<v-alert v-if="noticeBoard" class="building-notice-board mb-4" icon="info">
		<div>
			{{ noticeBoard.text }}
		</div>
		<div class="mt-2 duration">
			{{ moment(noticeBoard.startDate).format('Do MMMM YYYY') }}
			-
			{{ moment(noticeBoard.endDate).format('Do MMMM YYYY') }}
		</div>
	</v-alert>
</template>

<script setup lang="ts">
import { IBuildingNoticeBoard } from '@/models/Interfaces';
import moment from 'moment';

defineProps<{
	noticeBoard: IBuildingNoticeBoard;
}>();
</script>

<style scoped lang="scss">
.building-notice-board {
	font-size: size(16);
	border-radius: $border-radius;
	:deep(.v-icon) {
		color: $grey-darken-3;
	}

	.duration {
		font-size: size(14);
		color: $grey-darken-3;
	}
}
</style>
