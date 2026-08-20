import { computed, ref } from 'vue';

interface IBaseImage {
	url: string;
	title?: string;
}

const activeImage = ref<IBaseImage>();

export const useBaseImageModal = () => {
	const modalIsVisible = computed({
		get: () => !!activeImage.value,
		set: (val: boolean) => {
			if (!val) {
				activeImage.value = undefined;
			}
		},
	});

	const showImageInModal = (imageUrl: string, title: string = '') => {
		activeImage.value = {
			url: imageUrl,
			title,
		};
	};

	return { modalIsVisible, showImageInModal, activeImage };
};
