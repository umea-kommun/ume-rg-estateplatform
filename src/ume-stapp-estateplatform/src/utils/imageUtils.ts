import { fromBlob } from 'image-resize-compress';

export const getImageFileSize = (
	imageFile: File
): Promise<{ width: number; height: number }> => {
	return new Promise((resolve, reject) => {
		const img = new Image();
		const objectUrl = URL.createObjectURL(imageFile);
		img.onload = () => {
			URL.revokeObjectURL(objectUrl);
			resolve({ width: img.width, height: img.height });
		};
		img.onerror = () => {
			reject(
				'Unable to get image size of selected consent agent image, file type: ' +
					imageFile.type
			);
		};
		img.src = objectUrl;
	});
};

export const compressImageFile = async (
	image: File,
	maxWidthOrHeight = 1500
): Promise<File> => {
	const imageSize = await getImageFileSize(image);
	const isHorizontalImage = imageSize.width > imageSize.height;
	// quality value for webp and jpeg formats.
	const quality = 85;
	// output width. 0 will keep its original width and 'auto' will calculate its scale from height.
	const width = isHorizontalImage
		? Math.min(maxWidthOrHeight, imageSize.width)
		: 'auto';
	// output height. 0 will keep its original height and 'auto' will calculate its scale from width.
	const height = isHorizontalImage
		? 'auto'
		: Math.min(maxWidthOrHeight, imageSize.height);

	// note only the blobFile argument is required
	const imageBlob = await fromBlob(image, quality, width, height);
	return new File([imageBlob], image.name);
};
