import { describe, expect, it } from 'vitest';
import { findUnreadableFiles } from '../unreadableFiles';

const readableFile = (name: string) => new File(['content'], name);

const lockedFile = (name: string) => {
	const file = new File(['content'], name);
	file.slice = () =>
		({
			arrayBuffer: () =>
				Promise.reject(
					new DOMException('File is locked', 'NotReadableError')
				),
		}) as unknown as Blob;
	return file;
};

describe('findUnreadableFiles', () => {
	it('returns no indexes when all files are readable', async () => {
		expect(
			await findUnreadableFiles([
				readableFile('a.pdf'),
				readableFile('b.png'),
			])
		).toEqual([]);
	});

	it('returns the indexes of files that cannot be read', async () => {
		expect(
			await findUnreadableFiles([
				readableFile('a.pdf'),
				lockedFile('b.docx'),
				readableFile('c.png'),
				lockedFile('d.xlsx'),
			])
		).toEqual([1, 3]);
	});

	it('returns no indexes for an empty list', async () => {
		expect(await findUnreadableFiles([])).toEqual([]);
	});
});
