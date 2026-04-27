<template>
	<v-dialog
		v-model="showModal"
		class="building-document-modal estate-default"
		:max-width="900"
		aria-labelledby="modal-title"
		close-on-back
	>
		<v-card>
			<v-card-title id="modal-title" class="px-6">
				{{ $t('component.internal.buildingDocument.title') }}
			</v-card-title>

			<div class="content">
				<v-card-text>
					<div class="d-flex ga-4 mx-6">
						<v-text-field
							v-model="search"
							:placeholder="
								$t(
									'component.internal.buildingDocument.searchPlaceholder'
								)
							"
							color="primary"
							prepend-inner-icon="search"
							rounded="lg"
							density="comfortable"
							clearable
							variant="outlined"
							autocomplete="off"
							class="flex-grow-1"
						/>
						<v-select
							v-if="categories.length > 0"
							v-model="selectedCategory"
							:items="categories"
							:label="
								$t(
									'component.internal.buildingDocument.category'
								)
							"
							rounded="lg"
							density="comfortable"
							variant="outlined"
							clearable
							class="flex-shrink-0"
							style="max-width: 280px"
						/>
					</div>
					<v-skeleton-loader
						v-if="isBusyFetchingDocuments"
						class="ma-4 mt-1"
						type="list-item-two-line, list-item-two-line"
					/>
					<v-list
						v-else-if="filteredDocuments.length > 0"
						class="px-6 mt-4"
					>
						<document-file
							v-for="doc in filteredDocuments"
							:key="doc.id"
							:document="doc"
							:depth="0"
							@open="previewDocument = $event"
							@download="downloadDocument($event)"
						/>
					</v-list>
				</v-card-text>
			</div>
			<v-card-actions>
				<v-btn @click="showModal = false">
					{{ $t('app.nav.close') }}
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
	<document-preview-modal
		v-model="showPreview"
		:building="building"
		:document="previewDocument"
	/>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import {
	IBuildingDetails,
	IBuildingDocument,
} from '@/models/estate/Interfaces';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { DispatchType } from '@/models/Enums';
import DocumentFile from './document/DocumentFile.vue';
import DocumentPreviewModal from './document/DocumentPreviewModal.vue';
import ErrorService from '@/utils/ErrorService';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	modelValue: boolean;
	building: IBuildingDetails;
}>();

const emit = defineEmits(['update:modelValue']);

const store = useStore<IRootState>();
const { t } = useI18n();

const showModal = computed({
	get: () => props.modelValue,
	set: (show) => emit('update:modelValue', show),
});

const previewDocument = ref<IBuildingDocument | null>(null);
const showPreview = computed({
	get: () => previewDocument.value !== null,
	set: (show) => {
		if (!show) {
			previewDocument.value = null;
		}
	},
});

const search = ref('');
const selectedCategory = ref<string | null>(null);
const documents = ref<IBuildingDocument[]>([]);

const categories = computed(() => {
	const names = new Set(
		documents.value
			.map((d) => d.categoryName)
			.filter((n): n is string => n != null)
	);
	return [...names].sort();
});

const filteredDocuments = computed(() => {
	let result = documents.value;

	if (selectedCategory.value) {
		result = result.filter(
			(d) => d.categoryName === selectedCategory.value
		);
	}

	if (search.value) {
		const term = search.value.toLowerCase();
		result = result.filter((d) => d.name.toLowerCase().includes(term));
	}

	return result;
});

const isBusyFetchingDocuments = ref(false);
const fetchDocuments = async () => {
	isBusyFetchingDocuments.value = true;
	try {
		documents.value = await store.dispatch(
			DispatchType.GetBuildingDocuments,
			{
				buildingId: props.building.id,
			}
		);
	} catch (err) {
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchBuildingDocuments'),
		});
	} finally {
		isBusyFetchingDocuments.value = false;
	}
};

const downloadDocument = async (doc: IBuildingDocument) => {
	const blobData = await store.dispatch(
		DispatchType.DownloadBuildingDocument,
		{
			buildingId: props.building.id,
			documentId: doc.id,
		}
	);
	const blob = new Blob([blobData]);
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = doc.name;
	a.click();
	URL.revokeObjectURL(url);
};

watch(
	() => props.modelValue,
	(newVal) => {
		if (newVal) {
			fetchDocuments();
		}
	}
);
</script>

<style scoped lang="scss">
.content {
	overflow-y: auto;
	height: calc(80vh - 100px);
}
</style>
