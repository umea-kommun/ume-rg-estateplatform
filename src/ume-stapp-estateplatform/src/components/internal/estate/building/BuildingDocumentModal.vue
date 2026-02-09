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
				<v-card-text class="pa-0">
					<v-text-field
						v-model="search"
						:placeholder="
							$t(
								'component.internal.buildingDocument.searchPlaceholder'
							)
						"
						color="primary"
						class="mx-6"
						prepend-inner-icon="search"
						rounded="lg"
						density="comfortable"
						clearable
						variant="outlined"
						autocomplete="off"
					/>
					<v-skeleton-loader
						v-if="isBusyFetchingDocuments"
						class="ma-4 mt-1"
						type="list-item-two-line, list-item-two-line"
					/>
					<document-tree
						class="px-6 mt-4"
						v-else-if="nodeTree"
						:tree="nodeTree"
						:search="search"
						@open="previewDocument = $event"
					/>
				</v-card-text>
			</div>
			<v-card-actions>
				<hr class="mb-4 mt-4" />
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
	IBuildingDocumentTree,
} from '@/models/estate/Interfaces';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import { DispatchType } from '@/models/Enums';
import DocumentTree from './document/DocumentTree.vue';
import DocumentPreviewModal from './document/DocumentPreviewModal.vue';

const props = defineProps<{
	modelValue: boolean;
	building: IBuildingDetails;
}>();

const emit = defineEmits(['update:modelValue']);

const store = useStore<IRootState>();

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
const nodeTree = ref<IBuildingDocumentTree | null>(null);

const isBusyFetchingDocuments = ref(false);
const fetchDocuments = async () => {
	isBusyFetchingDocuments.value = true;
	try {
		nodeTree.value = await store.dispatch(
			DispatchType.GetBuildingDocuments,
			{
				buildingId: props.building.id,
			}
		);
	} finally {
		isBusyFetchingDocuments.value = false;
	}
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
