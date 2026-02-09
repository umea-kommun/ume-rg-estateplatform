<template>
	<div class="document-file mb-1" :class="{ root: depth === 0 }">
		<v-list-item class="pa-0" @click="emit('open', document)" rounded="lg">
			<div
				class="document-file-inner"
				:title="
					$t('component.internal.buildingDocument.downloadFile', {
						filename: document.name,
					})
				"
			>
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
						<div v-if="document.actionTypeName">
							{{ document.actionTypeName }}
						</div>
					</div>
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
const emit = defineEmits(['open']);

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
	return getHumanFileSize(props.document.sizeInBytes);
});
</script>

<style scoped lang="scss">
.document-file {
	.document-file-inner {
		display: flex;
		align-items: center;
		flex-wrap: wrap;

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
