<template>
	<div
		class="base-file-upload py-4"
		:class="{ dragging: draggingOver, 'has-error': hasErrors }"
		variant="outlined"
		@dragover.prevent
		@drop.prevent="onDrop"
	>
		<div
			class="instruction d-flex flex-column align-center justify-center py-2"
		>
			<v-icon icon="upload_file" class="mb-4" :size="32" />
			<div class="d-flex align-center">
				<button type="button" @click="openPicker" class="select-button">
					{{ $t('component.baseFileUpload.selectFiles') }}
				</button>
				<p class="ma-0">
					{{
						$t('component.baseFileUpload.dropFiles', {
							count: maxFiles,
						})
					}}
				</p>
			</div>

			<p class="text-medium-emphasis mb-0 mt-2">
				{{ rulesHint }}
			</p>
			<p v-if="formatsHint" class="text-medium-emphasis mb-0">
				{{ formatsHint }}
			</p>
		</div>

		<!-- Hidden native file input -->
		<input
			ref="fileInput"
			type="file"
			class="d-none"
			:accept="accept"
			:multiple="maxFiles > 1"
			@change="onPicked"
		/>

		<v-alert
			v-if="error"
			type="error"
			variant="tonal"
			class="mx-4 my-2"
			rounded="lg"
			:text="error"
		/>

		<v-alert
			v-if="generalFileErrors.length"
			type="error"
			variant="tonal"
			class="mx-4 my-2"
			rounded="lg"
		>
			<div v-for="msg in generalFileErrors" :key="msg">{{ msg }}</div>
		</v-alert>

		<v-list v-if="files.length" density="compact" class="mx-4 mt-4 py-0">
			<v-list-item
				v-for="(f, i) in files"
				:key="fileKey(f)"
				rounded="lg"
				class="mt-2 pl-4 pr-2 py-2"
			>
				<template #prepend>
					<v-icon icon="insert_drive_file" :size="24" />
				</template>
				<v-list-item-title>
					{{ f.name }}
				</v-list-item-title>
				<v-list-item-subtitle>
					{{ formatBytes(f.size) }}
				</v-list-item-subtitle>
				<div
					v-if="fileErrorMessages(i).length"
					class="file-error text-error mt-1"
				>
					<div v-for="msg in fileErrorMessages(i)" :key="msg">
						{{ msg }}
					</div>
				</div>

				<template #append>
					<v-btn
						icon="close"
						variant="text"
						class="ma-0"
						rounded="xl"
						@click="removeAt(i)"
					/>
				</template>
			</v-list-item>
		</v-list>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	modelValue: File[];
	accept?: string; // e.g. "image/*,.pdf"
	multiple?: boolean;
	maxFiles?: number;
	maxSizeMegaBytes?: number;
	/** Server validation errors keyed by field (e.g. "files" or "files[0]") with error message arrays. */
	serverErrors?: Record<string, string[]>;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', v: File[]): void;
}>();

const { t } = useI18n();

const maxFiles = computed(() => props.maxFiles ?? 20);
const maxSizeBytes = computed(() =>
	props.maxSizeMegaBytes
		? props.maxSizeMegaBytes * 1024 * 1024
		: 25 * 1024 * 1024
);

const files = computed(() => props.modelValue ?? []);
const fileInput = ref<HTMLInputElement | null>(null);
const error = ref<string>('');

const hasErrors = computed(
	() => !!error.value || Object.keys(props.serverErrors ?? {}).length > 0
);

const generalFileErrors = computed(() => {
	const errs = props.serverErrors ?? {};
	return errs['files'] ?? [];
});

function fileErrorMessages(index: number): string[] {
	const errs = props.serverErrors ?? {};
	return errs[`files[${index}]`] ?? [];
}

function formatBytes(bytes: number, decimals = 1) {
	const units = ['B', 'KB', 'MB', 'GB'];
	let i = 0;
	let n = bytes;
	while (n >= 1024 && i < units.length - 1) {
		n /= 1024;
		i++;
	}
	const localNumber = n.toLocaleString(undefined, {
		minimumFractionDigits: decimals,
		maximumFractionDigits: decimals,
	});
	return `${localNumber} ${units[i]}`;
}

const rulesHint = computed(() => {
	return t('component.baseFileUpload.rulesHint', {
		maxFiles: maxFiles.value,
		size: formatBytes(maxSizeBytes.value, 0),
		count: maxFiles.value,
	});
});

const formatsHint = computed(() => {
	if (!props.accept) return '';

	const mimeToLabel: Record<string, string> = {
		'image/*': 'Bilder',
		'image/png': 'PNG',
		'image/jpeg': 'JPEG',
		'image/gif': 'GIF',
		'image/webp': 'WebP',
		'image/bmp': 'BMP',
		'image/tiff': 'TIFF',
		'image/heic': 'HEIC',
		'application/pdf': 'PDF',
	};

	const labels = props.accept
		.split(',')
		.map((s) => s.trim())
		.map((s) => {
			if (mimeToLabel[s]) return mimeToLabel[s];
			if (s.startsWith('.')) return s.substring(1).toUpperCase();
			return s;
		});

	if (labels.length === 0) return '';
	return t('component.baseFileUpload.acceptedFormats', {
		formats: labels.join(', '),
	});
});

function openPicker() {
	error.value = '';
	fileInput.value?.click();
}

function fileKey(f: File) {
	return `${f.name}|${f.size}|${f.lastModified}`;
}

function appendFiles(newOnes: File[]) {
	error.value = '';

	// Basic per-file validation
	for (const f of newOnes) {
		if (f.size > maxSizeBytes.value) {
			error.value = t('component.baseFileUpload.error.fileTooLarge', {
				filename: f.name,
			});
			return;
		}
	}

	// Merge + de-dupe
	const existing = files.value.slice();
	const existingKeys = new Set(existing.map(fileKey));

	const merged: File[] = [...existing];
	for (const f of newOnes) {
		const k = fileKey(f);
		if (!existingKeys.has(k)) {
			merged.push(f);
			existingKeys.add(k);
		}
	}

	if (merged.length > maxFiles.value) {
		error.value = t('component.baseFileUpload.error.tooManyFiles', {
			maxFiles: maxFiles.value,
		});
		return;
	}

	emit('update:modelValue', merged);
}

function onPicked(e: Event) {
	const input = e.target as HTMLInputElement;
	const picked = Array.from(input.files ?? []);
	appendFiles(picked);

	// Reset so picking the same file again fires change
	input.value = '';
}

function onDrop(e: DragEvent) {
	const dropped = Array.from(e.dataTransfer?.files ?? []);
	appendFiles(dropped);
}

function removeAt(i: number) {
	const next = files.value.slice();
	next.splice(i, 1);
	emit('update:modelValue', next);
}

const draggingOver = ref(false);

const onDragOver = (e: DragEvent) => {
	e.preventDefault();
	draggingOver.value = true;
};
const onDragLeave = (e: DragEvent) => {
	const next = e.relatedTarget as Node | null;
	if (next && document.contains(next)) {
		return; // still inside
	}
	draggingOver.value = false;
};

onMounted(() => {
	window.addEventListener('dragover', onDragOver);
	window.addEventListener('dragleave', onDragLeave);
	window.addEventListener('drop', onDragLeave);
});

onUnmounted(() => {
	window.removeEventListener('dragover', onDragOver);
	window.removeEventListener('dragleave', onDragLeave);
	window.removeEventListener('drop', onDragLeave);
});
</script>

<style scoped lang="scss">
.base-file-upload {
	position: relative;
	border-radius: $border-radius;

	outline: dashed 2px $grey-lighten-4;
	&.dragging {
		outline: dashed 2px rgba($primary, 0.5);
		background-color: rgba($primary, 0.1);
	}
	&.has-error {
		outline: dashed 2px rgb(var(--v-theme-error));
	}
	.instruction {
		font-size: size(16);

		.v-icon {
			color: $grey-darken-2;
			color: $primary;
		}

		.select-button {
			color: $primary;
			background: none;
			border: none;
			padding: 4px 0;
			margin-right: 4px;
			cursor: pointer;
			text-decoration: underline;

			&:focus-visible {
				outline: 2px solid $primary;
				outline-offset: 2px;
			}
		}
		.text-medium-emphasis {
			font-size: size(14);
		}
	}
	.v-list {
		background-color: transparent;
		overflow: visible;

		.v-list-item {
			background-color: $grey-lighten-2;
			border-radius: $border-radius;

			:deep(.v-list-item__prepend) {
				display: block;
			}

			.v-btn {
				color: $grey-darken-2;
			}

			.file-error {
				font-size: size(13);
			}
		}
	}
}
</style>
