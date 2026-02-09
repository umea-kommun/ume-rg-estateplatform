<template>
	<div class="document-tree">
		<v-alert
			v-if="
				search &&
				filteredDirectories.directories.length === 0 &&
				filteredDirectories.rootDocuments.length === 0
			"
			rounded="lg"
			icon="info"
		>
			{{ $t('component.internal.buildingDocument.searchNoResults') }}
		</v-alert>

		<document-folder
			v-for="directory in filteredDirectories.directories"
			:key="directory.id"
			:depth="0"
			:directory="directory"
			@open="emit('open', $event)"
		/>
		<document-file
			v-for="document in filteredDirectories.rootDocuments"
			:key="document.id"
			:document="document"
			:depth="0"
			@open="emit('open', $event)"
		/>
	</div>
</template>

<script setup lang="ts">
import {
	IBuildingDirectory,
	IBuildingDocument,
	IBuildingDocumentTree,
} from '@/models/estate/Interfaces';
import DocumentFolder from './DocumentFolder.vue';
import DocumentFile from './DocumentFile.vue';
import { computed } from 'vue';

const props = defineProps<{
	tree: IBuildingDocumentTree;
	search: string | null;
}>();
const emit = defineEmits(['open']);

function filterBuildingDocumentTree(
	tree: IBuildingDocumentTree,
	predicate: (doc: IBuildingDocument) => boolean
): IBuildingDocumentTree {
	const filterDir = (dir: IBuildingDirectory): IBuildingDirectory | null => {
		const keptDocuments = dir.documents.filter(predicate);

		const keptSubDirs: IBuildingDirectory[] = [];
		for (const sub of dir.subDirectories) {
			const filtered = filterDir(sub);
			if (filtered) {
				keptSubDirs.push(filtered);
			}
		}

		// Keep this directory only if it contains matching docs or any surviving subdirs
		if (keptDocuments.length === 0 && keptSubDirs.length === 0) {
			return null;
		}

		return {
			...dir,
			documents: keptDocuments,
			subDirectories: keptSubDirs,
		};
	};

	const directories: IBuildingDirectory[] = [];
	for (const d of tree.directories) {
		const filtered = filterDir(d);
		if (filtered) {
			directories.push(filtered);
		}
	}

	const rootDocuments = tree.rootDocuments.filter(predicate);

	return {
		...tree,
		directories,
		rootDocuments,
	};
}

const filteredDirectories = computed(() => {
	if (!props.search?.trim()) {
		return props.tree;
	}

	return filterBuildingDocumentTree(props.tree, (doc) =>
		doc.name
			.toLowerCase()
			.includes(props.search?.toLowerCase().trim() ?? '')
	);
});
</script>

<style scoped lang="scss">
.document-tree {
	:deep(.v-alert .v-icon) {
		color: $grey-darken-3;
	}

	:deep(.document-file),
	:deep(.document-folder) {
		padding-left: 28px;

		&.root {
			padding-left: 0;
		}

		@media only screen and (max-width: $estate-mobile-threshold) {
			padding-left: 0;
			padding-bottom: 8px;
		}
	}
}
</style>
