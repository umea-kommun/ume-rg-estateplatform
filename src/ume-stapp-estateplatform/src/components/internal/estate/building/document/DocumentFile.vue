<template>
	<div class="document-file mb-1" :class="{ root: depth === 0 }">
		<v-list-item class="pa-0" rounded="lg">
			<div class="document-file-inner">
				<v-icon
					icon="insert_drive_file"
					class="mx-2"
					:size="24"
					color="grey-darken-3"
				/>
				<div class="content pb-1">
					<div class="title">{{ document.name }}</div>
					<div class="subtitle d-flex flex-wrap">
						<div>
							{{ humanFileSize }}
						</div>
						<div v-if="document.categoryName">
							{{ document.categoryName }}
						</div>
					</div>
				</div>
				<div class="actions">
					<v-btn
						v-if="canPreview"
						icon="visibility"
						variant="text"
						size="small"
						@click="emit('open', document)"
					/>
					<v-btn
						icon="download"
						variant="text"
						size="small"
						@click="emit('download', document)"
					/>
				</div>
			</div>
		</v-list-item>
	</div>
</template>

<script setup lang="ts">
import { IBuildingDocument } from '@/models/estate/Interfaces';
import { computed } from 'vue';

const props = defineProps<{
	document: IBuildingDocument;
	depth: number;
}>();
const emit = defineEmits(['open', 'download']);

const getHumanFileSize = (sizeInBytes: number): string => {
	const i =
		sizeInBytes == 0
			? 0
			: Math.floor(Math.log(sizeInBytes) / Math.log(1024));
	return (
		+(sizeInBytes / Math.pow(1024, i)).toFixed(2) * 1 +
		' ' +
		['B', 'kB', 'MB', 'GB', 'TB'][i]
	);
};

const humanFileSize = computed(() => {
	return props.document.sizeInBytes != null
		? getHumanFileSize(props.document.sizeInBytes)
		: '';
});

const previewExtensions = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'bmp', 'svg', 'pdf'];
const canPreview = computed(() => {
	const ext = props.document.name.toLowerCase().match(/\.([a-z0-9]+)$/)?.[1] ?? '';
	return previewExtensions.includes(ext);
});
</script>

<style scoped lang="scss">
.document-file {
	.document-file-inner {
		display: flex;
		align-items: center;

		.actions {
			flex-shrink: 0;
			display: flex;
		}

		.content {
			flex: 1;
			.title {
				font-size: size(18);
				word-break: break-all;
			}
			.subtitle {
				color: $grey-darken-1;

				& > div:not(:last-child)::after {
					content: '•';
					padding: 0 6px;
				}
			}
		}
	}
}
</style>
