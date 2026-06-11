<template>
	<app-content
		:isLoading="isBusyLoadingFromServer"
		class="kvittens-template-list"
		:pageTitle="$t('component.internal.kvittensTemplateList.title')"
	>
		<base-back-button />
		<div v-if="!isBusyLoadingFromServer">
			<h1 class="my-3">
				{{ $t('component.internal.kvittensTemplateList.title') }}
			</h1>

			<div class="filter d-flex align-center ga-4">
				<v-text-field
					id="search"
					v-model="searchValue"
					color="primary"
					:label="
						$t('component.internal.consentTemplateAdmin.search')
					"
					prepend-inner-icon="search"
					density="comfortable"
					hide-details
					clearable
				/>
				<v-btn
					class="create-button ma-0"
					color="primary"
					size="large"
					prepend-icon="add"
					:to="{
						name: MyPagesRoutes.InternalKvittensTemplateEdit,
						params: { id: 'new' },
					}"
					flat
				>
					{{ $t('component.internal.consentTemplateAdmin.create') }}
				</v-btn>
			</div>

			<v-alert
				v-if="filteredTemplates.length === 0"
				icon="info"
				class="mt-4"
			>
				{{ $t('component.internal.kvittensTemplateList.noTemplates') }}
			</v-alert>
			<v-list lines="two" class="mt-3 overflow-visible">
				<v-list-item
					v-for="template in filteredTemplates"
					:key="template.id"
					:title="template.shortTitle"
					:subtitle="template.title"
					:to="{
						name: MyPagesRoutes.InternalKvittensTemplateEdit,
						params: { id: template.id },
					}"
					class="mb-4"
					rounded="lg"
					elevation="1"
				>
					<div class="template-targets">
						<v-chip
							v-for="group in template.displayTargets"
							:key="group.schoolForm"
							class="template-target"
						>
							{{ group.schoolFormLabel }}
							{{ group.schoolYearsLabel }}
						</v-chip>
					</div>
					<template #append>
						<v-btn variant="text" color="primary">
							{{
								$t(
									'component.internal.kvittensTemplateList.open'
								)
							}}
						</v-btn>
					</template>
				</v-list-item>
			</v-list>
		</div>
	</app-content>
</template>

<script lang="ts" setup>
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import { IRootState } from '@/models/Interfaces';
import { IKvittensTemplate } from '@/models/kvittens/Interfaces';
import { MyPagesRoutes } from '@/router/routes';
import { computed, onMounted, ref } from 'vue';
import { useStore } from 'vuex';
import { useKvittensTemplateTarget } from './KvittensTemplateTarget';
const store = useStore<IRootState>();

const isBusyLoadingFromServer = ref(false);
const templates = ref<IKvittensTemplate[]>([]);

const searchValue = ref<string>('');

const compareTemplates = (
	a: IKvittensTemplate,
	b: IKvittensTemplate
): number => {
	const aFirst = a.targets[0];
	const bFirst = b.targets[0];

	const formCompare = (aFirst?.schoolForm ?? '').localeCompare(
		bFirst?.schoolForm ?? ''
	);
	if (formCompare !== 0) return formCompare;

	const yearCompare =
		Number(aFirst?.schoolYear ?? 0) - Number(bFirst?.schoolYear ?? 0);
	if (yearCompare !== 0) return yearCompare;

	return a.shortTitle.localeCompare(b.shortTitle);
};

const { addDisplayTargetsToTemplate } = useKvittensTemplateTarget();

const filteredTemplates = computed(() => {
	const displayTemplates = templates.value.map((template) =>
		addDisplayTargetsToTemplate(template)
	);

	const sorted = [...displayTemplates].sort(compareTemplates);

	if (!searchValue.value) {
		return sorted;
	}
	const search = searchValue.value.toLowerCase();
	return sorted.filter(
		(template) =>
			template.title.toLowerCase().includes(search) ||
			template.shortTitle.toLowerCase().includes(search)
	);
});

const fetchTemplates = async () => {
	isBusyLoadingFromServer.value = true;
	try {
		templates.value = await store.dispatch('getKvittensTemplates');
	} finally {
		isBusyLoadingFromServer.value = false;
	}
};

onMounted(() => {
	fetchTemplates();
});
</script>

<style lang="scss" scoped>
.kvittens-template-list {
	&.app-content {
		:deep(.v-container) {
			padding-top: calc($site-content-vertical-padding - 20px);
		}
	}

	.template-targets {
		display: flex;
		flex-wrap: wrap;
		gap: 8px;
		margin-top: 8px;
	}
	.filter {
		.v-btn {
			height: size(48);
			color: #fff;
		}
	}
}
</style>
