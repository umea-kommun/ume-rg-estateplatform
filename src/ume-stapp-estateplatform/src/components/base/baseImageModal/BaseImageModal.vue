<template>
	<div v-if="activeImage">
		<v-dialog
			class="base-image-modal"
			v-model="modalIsVisible"
			:max-width="1440"
		>
			<v-card v-if="activeImage">
				<v-card-title v-if="activeImage.title">
					{{ activeImage.title }}
				</v-card-title>
				<v-card-text>
					<v-progress-linear
						v-if="loadingImage"
						color="green"
						height="3"
						indeterminate
					></v-progress-linear>
					<img :src="activeImage.url" />
				</v-card-text>

				<v-divider></v-divider>

				<v-card-actions>
					<v-spacer></v-spacer>

					<v-btn color="primary" @click="modalIsVisible = false">
						{{ $t('app.nav.close') }}
					</v-btn>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useBaseImageModal } from './baseImageModal';

const { activeImage, modalIsVisible } = useBaseImageModal();

const loadingImage = ref(false);
watch(
	() => activeImage.value?.url,
	(imgUrl) => {
		if (imgUrl) {
			loadingImage.value = true;
			const img = new Image();
			img.onload = () => {
				loadingImage.value = false;
			};
			img.onerror = () => {
				loadingImage.value = false;
			};
			img.src = imgUrl;
		}
	}
);
</script>

<style scoped lang="scss">
.base-image-modal {
	img {
		width: 100%;
		max-height: calc(80vh - 74px);
		object-fit: contain;
	}
}
</style>
