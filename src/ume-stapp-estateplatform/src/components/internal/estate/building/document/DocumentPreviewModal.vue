<template>
	<v-dialog
		v-model="showModal"
		class="document-preview-modal estate-default"
		:max-width="1300"
		aria-labelledby="modal-title"
		close-on-back
	>
		<v-card :loading="isBusyFetchingDocument">
			<v-card-title id="modal-title" class="px-6">
				{{ document?.name }}
			</v-card-title>

			<div class="content">
				<div
					v-if="isBusyFetchingDocument"
					class="d-flex justify-center w-100 align-center"
				>
					<v-progress-circular
						indeterminate
						:width="3"
						:size="48"
						color="green"
					/>
				</div>
				<div v-else-if="canPreview" class="preview-wrap">
					<!-- IMAGE PREVIEW -->
					<img
						v-if="isImage"
						:src="blobUrl"
						class="preview"
						:alt="document?.name"
					/>

					<!-- PDF PREVIEW -->
					<object
						v-if="isPdf"
						class="preview"
						:data="blobUrl"
						type="application/pdf"
					>
						<p>
							{{
								$t(
									'component.internal.buildingDocument.noPreview'
								)
							}}
							<a :href="blobUrl" target="_blank" rel="noopener">{{
								$t(
									'component.internal.buildingDocument.openPdf'
								)
							}}</a>
						</p>
					</object>
				</div>
				<v-card-text
					v-else
					class="d-flex justify-center w-100 align-center px-4"
				>
					<div
						class="d-flex flex-column ga-4 justify-center align-center"
					>
						<v-alert rounded="lg">
							{{
								$t(
									'component.internal.buildingDocument.noPreview'
								)
							}}
						</v-alert>
						<v-btn
							:href="blobUrl"
							:download="document?.name"
							color="primary"
							prepend-icon="download"
							size="large"
							flat
							class="ma-0 regular-text"
							:disabled="!blobUrl"
						>
							{{
								$t(
									'component.internal.buildingDocument.downloadFile',
									{ filename: document?.name }
								)
							}}
						</v-btn>
					</div>
				</v-card-text>
			</div>
			<v-card-actions>
				<v-btn
					:href="blobUrl"
					:download="document?.name"
					color="primary"
					prepend-icon="download"
					:disabled="!blobUrl"
				>
					{{ $t('component.internal.buildingDocument.downloadFile') }}
				</v-btn>
				<v-spacer />
				<v-btn @click="showModal = false">
					{{ $t('app.nav.close') }}
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import {
	IBuildingDetails,
	IBuildingDocument,
} from '@/models/estate/Interfaces';
import { IRootState } from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';

const props = defineProps<{
	modelValue: boolean;
	building: IBuildingDetails;
	document: IBuildingDocument | null;
}>();

const emit = defineEmits(['update:modelValue']);

const store = useStore<IRootState>();
const { t } = useI18n();

const showModal = computed({
	get: () => props.modelValue,
	set: (show) => emit('update:modelValue', show),
});

function extFromName(name?: string) {
	if (!name) return '';
	const m = name.toLowerCase().match(/\.([a-z0-9]+)$/);
	return m?.[1] ?? '';
}

const fileBlob = ref<Blob | null>(null);
const blobUrl = ref('');

const ext = computed(() => extFromName(props.document?.name));

const isImage = computed(() => {
	return ['png', 'jpg', 'jpeg', 'gif', 'webp', 'bmp', 'svg'].includes(
		ext.value
	);
});

const isPdf = computed(() => {
	return ext.value === 'pdf';
});

const canPreview = computed(
	() => !!blobUrl.value && (isImage.value || isPdf.value)
);

const isBusyFetchingDocument = ref(false);
const fetchFile = async (document: IBuildingDocument) => {
	isBusyFetchingDocument.value = true;

	if (blobUrl.value) {
		URL.revokeObjectURL(blobUrl.value);
		blobUrl.value = '';
	}
	try {
		const blobData = await store.dispatch(
			DispatchType.DownloadBuildingDocument,
			{
				buildingId: props.building.id,
				documentId: document.id,
			}
		);

		const blob = new Blob([blobData], {
			type: isPdf.value ? 'application/pdf' : blobData.type,
		});

		const href = URL.createObjectURL(blob);
		blobUrl.value = href;
		fileBlob.value = blob;
	} catch (err) {
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchBuildingDocument'),
		});
	} finally {
		isBusyFetchingDocument.value = false;
	}
};

const revokeBlob = () => {
	if (blobUrl.value) {
		URL.revokeObjectURL(blobUrl.value);
		blobUrl.value = '';
		fileBlob.value = null;
	}
};

watch(
	() => showModal.value,
	(newVal) => {
		if (newVal && props.document) {
			fetchFile(props.document);
		} else {
			revokeBlob();
		}
	}
);

onBeforeUnmount(() => {
	revokeBlob();
});
</script>

<style scoped lang="scss">
.content {
	overflow-y: auto;
	height: calc(90vh - 100px);
	display: flex;

	.preview-wrap {
		display: flex;
		flex: 1;
	}
	.preview {
		width: 100%;
		flex: 1;
		background-color: $grey-darken-4;
		object-fit: scale-down;
	}
	a {
		color: #fff !important;
	}
}
</style>
