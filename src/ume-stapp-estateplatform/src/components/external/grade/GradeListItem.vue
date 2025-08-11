<template>
	<v-card
		elevation="2"
		class="mb-2 d-flex justify-space-between align-center flex-wrap"
		@click="downloadGradeFile"
	>
		<div>
			<v-card-text>
				{{ grade.documentName }}
			</v-card-text>
		</div>
		<div class="d-flex flex-fill justify-end align-center">
			<v-btn
				color="primary"
				flat
				prepend-icon="open_in_new"
				class="regular-text ma-4"
				rounded="lg"
				:loading="isBusyFetchingGrade"
			>
				{{ $t('component.external.gradeListItem.download') }}
			</v-btn>
		</div>
	</v-card>
</template>

<script lang="ts" setup>
import { DispatchType } from '@/models/Enums';
import { IGrade } from '@/models/grade/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { ref } from 'vue';
import { useStore } from 'vuex';

const props = defineProps<{
	grade: IGrade;
}>();

const store = useStore<IRootState>();

const isBusyFetchingGrade = ref(false);

const downloadGradeFile = async () => {
	isBusyFetchingGrade.value = true;
	await store.dispatch(DispatchType.DownloadGrade, props.grade.documentId);
	isBusyFetchingGrade.value = false;
};
</script>

<style scoped lang="scss">
.v-card {
	word-break: break-all;

	.v-card-text {
		font-size: size(18);
	}
}
</style>
