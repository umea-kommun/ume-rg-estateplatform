/**
 * Returns the indexes of files the browser can no longer read, e.g. a document
 * locked by another program (Word on Windows) or changed on disk after it was
 * picked. Uploading such a file aborts the request client-side with a bare
 * "Network Error", so check before sending. Reading one byte is enough to
 * trigger the failure.
 */
export const findUnreadableFiles = async (files: File[]): Promise<number[]> => {
	const results = await Promise.all(
		files.map(async (file) => {
			try {
				await file.slice(0, 1).arrayBuffer();
				return true;
			} catch {
				return false;
			}
		})
	);
	return results.flatMap((readable, index) => (readable ? [] : [index]));
};
