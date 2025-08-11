<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="grade-start"
		:pageTitle="$t('component.external.gradeStart.title')"
	>
		<base-back-button
			:to="{ name: MyPagesRoutes.AppStart, replace: true }"
		/>
		<div class="d-flex flex-wrap justify-space-between top-wrap">
			<h1>{{ $t('component.external.gradeStart.title') }}</h1>
		</div>
		<p>
			{{ $t('component.external.gradeStart.description') }}
			{{ $t('component.external.gradeStart.explanation') }}
			<a :href="Config.VUE_APP_GRADE_HELP_URL" target="_blank">
				{{ $t('component.external.gradeStart.explanationLink') }}
			</a>
		</p>

		<v-alert v-if="!grades.length" icon="warning" class="mt-6">
			{{ $t('component.external.gradeStart.noResults') }}
		</v-alert>
		<div v-else class="mt-4">
			<div
				v-for="(schoolGrades, schoolName) in gradesGroupedBySchool"
				:key="schoolName"
				class="mb-8"
			>
				<h2>{{ schoolName }}</h2>
				<v-divider class="mb-4 mt-4" />
				<grade-list-item
					v-for="grade in schoolGrades"
					:key="grade.documentId"
					:grade="grade"
				/>
			</div>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import { useRoute } from 'vue-router';
import { AppContentSize } from '@/models/Enums';
import { DispatchType } from '@/models/Enums';
import { MyPagesRoutes } from '@/router/routes';
import GradeListItem from './GradeListItem.vue';
import { IGrade } from '@/models/grade/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { useStore } from 'vuex';
import Config from '@/Config';

const route = useRoute();

const store = useStore<IRootState>();

const isBusyLoadingFromServer = ref<boolean>(false);
const grades = ref<IGrade[]>([]);

onMounted(async () => {
	isBusyLoadingFromServer.value = true;
	grades.value = await store.dispatch(DispatchType.GetGrades);
	isBusyLoadingFromServer.value = false;
});

const gradesGroupedBySchool = computed(() => {
	const schoolGrades: { [key: string]: IGrade[] } = {};
	grades.value.forEach((grade) => {
		if (!schoolGrades[grade.schoolName]) {
			schoolGrades[grade.schoolName] = [];
		}
		schoolGrades[grade.schoolName].push(grade);
	});
	return schoolGrades;
});

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);
</script>

<style scoped lang="scss">
.grade-start {
	.top-wrap {
		h1 {
			font-size: size(38);
		}
	}
}
.kvittens-start.app-content {
	:deep(.v-container) {
		padding-top: calc($site-content-vertical-padding - 20px);
	}
}
</style>
