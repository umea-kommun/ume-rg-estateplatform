<template>
	<div class="building-document-panel">
		<h2 class="mt-0 mb-2">
			{{ $t('component.buildingDetails.documentsButton') }}
		</h2>
		<div v-if="hasDocuments" class="d-flex flex-wrap ga-4">
			<v-text-field
				v-model="search"
				:placeholder="
					$t('component.buildingDocument.searchPlaceholder')
				"
				color="primary"
				prepend-inner-icon="search"
				rounded="lg"
				density="comfortable"
				clearable
				variant="outlined"
				autocomplete="off"
				class="flex-grow-1 document-search"
			/>
			<v-select
				v-if="categories.length > 0"
				v-model="selectedCategory"
				:items="categories"
				:label="$t('component.buildingDocument.category')"
				rounded="lg"
				density="comfortable"
				variant="outlined"
				clearable
				class="flex-shrink-0 category-select"
			/>
		</div>
		<v-skeleton-loader
			v-if="isBusyFetchingDocuments || !hasLoaded"
			type="list-item-two-line, list-item-two-line"
		/>
		<p v-else-if="!hasDocuments">
			{{ $t('component.buildingDocument.noDocuments') }}
		</p>
		<v-list v-else-if="filteredDocuments.length > 0" class="document-list">
			<document-file
				v-for="doc in filteredDocuments"
				:key="doc.id"
				:document="doc"
				:depth="0"
				@open="previewDocument = $event"
				@download="downloadDocument($event)"
			/>
		</v-list>
		<v-alert type="info" variant="tonal" rounded="lg" class="mt-4">
			<p
				v-for="(paragraph, index) in infoParagraphs"
				:key="index"
				:class="{
					'mb-0': index === infoParagraphs.length - 1,
				}"
			>
				{{ paragraph }}
			</p>
		</v-alert>
		<document-preview-modal
			v-model="showPreview"
			:building="building"
			:document="previewDocument"
		/>
	</div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { IBuildingDetails, IBuildingDocument } from '@/models/Interfaces';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { DispatchType } from '@/models/Enums';
import DocumentFile from './document/DocumentFile.vue';
import DocumentPreviewModal from './document/DocumentPreviewModal.vue';
import ErrorService from '@/utils/ErrorService';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	building: IBuildingDetails;
	active: boolean;
}>();

const store = useStore<IRootState>();
const { t, tm } = useI18n();

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

const hasDocuments = computed(() => documents.value.length > 0);

const infoParagraphs = computed<string[]>(
	() => tm('component.buildingDocument.info') as unknown as string[]
);

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
// Until the first response the panel has nothing to say, so it stays on the
// skeleton rather than claiming the building has no documents
const hasLoaded = ref(false);
let fetchedBuildingId: number | null = null;

const fetchDocuments = async (buildingId: number) => {
	isBusyFetchingDocuments.value = true;
	try {
		const fetched = await store.dispatch(
			DispatchType.GetBuildingDocuments,
			{
				buildingId,
			}
		);
		if (buildingId !== props.building.id) {
			return;
		}
		documents.value = fetched;
	} catch (err) {
		if (buildingId !== props.building.id) {
			return;
		}
		// Clears the marker so opening the tab again retries a failed fetch
		fetchedBuildingId = null;
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchBuildingDocuments'),
		});
	} finally {
		if (buildingId === props.building.id) {
			isBusyFetchingDocuments.value = false;
			hasLoaded.value = true;
		}
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

// Prefetched once the page has gone quiet so it does not compete with the
// building details and the blueprint, but opening the tab never waits for that
const PREFETCH_TIMEOUT_MS = 2000;

const fetchDocumentsOnce = () => {
	if (fetchedBuildingId === props.building.id) {
		return;
	}
	fetchedBuildingId = props.building.id;
	fetchDocuments(props.building.id);
};

const schedulePrefetch = (task: () => void) => {
	if (typeof requestIdleCallback !== 'function') {
		const timer = setTimeout(task, PREFETCH_TIMEOUT_MS);
		return () => clearTimeout(timer);
	}
	const handle = requestIdleCallback(task, { timeout: PREFETCH_TIMEOUT_MS });
	return () => cancelIdleCallback(handle);
};

let cancelPrefetch: (() => void) | null = null;

watch(
	() => props.building.id,
	() => {
		documents.value = [];
		hasLoaded.value = false;
		isBusyFetchingDocuments.value = false;
		fetchedBuildingId = null;
		cancelPrefetch?.();
		cancelPrefetch = schedulePrefetch(fetchDocumentsOnce);
	},
	{ immediate: true }
);

watch(
	() => props.active,
	(active) => {
		if (active) {
			cancelPrefetch?.();
			fetchDocumentsOnce();
		}
	}
);

onBeforeUnmount(() => cancelPrefetch?.());
</script>

<style scoped lang="scss">
.building-document-panel {
	.category-select {
		max-width: 280px;
	}
	.document-list {
		max-height: 60vh;
		overflow-y: auto;
	}

	@media only screen and (max-width: $estate-mobile-threshold) {
		.document-search {
			flex: 1 1 100%;
		}
		.category-select {
			max-width: none;
			flex: 1 1 100%;
		}
		// body sets overscroll-behavior-y: none, so an inner scroll area traps
		// the gesture at its end
		.document-list {
			max-height: none;
			overflow-y: visible;
		}
	}
}
</style>
