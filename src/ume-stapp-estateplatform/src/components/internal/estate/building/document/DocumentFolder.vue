<template>
	<div
		class="document-folder"
		:class="{
			root: depth === 0,
			opened: opened,
			empty: empty,
		}"
	>
		<v-list-item
			@click="opened = !opened"
			rounded="lg"
			class="pa-0"
			:disabled="empty"
		>
			<h3 class="py-2 d-flex align-center flex-wrap ga-2">
				<div class="d-flex">
					<v-icon
						:icon="opened ? 'arrow_drop_down' : 'arrow_right'"
						:size="24"
						class="ml-2 mr-1"
						color="grey-darken-3"
					/>
					<v-icon
						:icon="opened ? 'folder_open' : 'folder'"
						:size="24"
						color="grey-darken-3"
					/>
				</div>
				{{ directory.name }}
				<v-chip v-if="empty" class="ml-4">
					{{ $t('component.internal.buildingDocument.noContent') }}
				</v-chip>
			</h3>
		</v-list-item>

		<div
			v-if="
				opened &&
				(directory.subDirectories.length || directory.documents.length)
			"
			class="document-contents"
		>
			<document-folder
				v-for="subDirectory in directory.subDirectories"
				:key="subDirectory.id"
				:directory="subDirectory"
				:depth="depth + 1"
				:loading-id="loadingId"
				@open="emit('open', $event)"
			/>
			<document-file
				v-for="document in directory.documents"
				:key="document.id"
				:document="document"
				:depth="depth + 1"
				:loading-id="loadingId"
				@open="emit('open', $event)"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { IBuildingDirectory } from '@/models/estate/Interfaces';
import DocumentFile from './DocumentFile.vue';
import { computed, ref } from 'vue';

const props = defineProps<{
	directory: IBuildingDirectory;
	depth: number;
	loadingId?: number | null;
}>();
const emit = defineEmits(['open']);

const opened = ref(true);

const empty = computed(() => {
	return (
		props.directory.subDirectories.length === 0 &&
		props.directory.documents.length === 0
	);
});
</script>

<style scoped lang="scss">
.document-folder {
	h3 {
		font-size: size(16);
		font-weight: normal;
		word-break: break-all;
	}

	@media only screen and (max-width: $estate-mobile-threshold) {
		padding-left: 0;
		padding-bottom: 8px;

		&.opened {
			border-left: solid 2px $grey-lighten-4;
			margin-bottom: 16px;
		}
		.document-contents {
			margin-left: 10px;
		}
	}
}
</style>
