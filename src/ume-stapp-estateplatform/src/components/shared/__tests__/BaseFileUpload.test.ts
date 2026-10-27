import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, test } from 'vitest';
import { createI18n } from 'vue-i18n';
import BaseFileUpload from '../BaseFileUpload.vue';
import sv from '@/locales/sv.json';

const readableFile = (name: string) =>
	new File(['content'], name, { type: 'application/pdf' });

const lockedFile = (name: string) => {
	const file = readableFile(name);
	file.slice = () =>
		({
			arrayBuffer: () =>
				Promise.reject(
					new DOMException('File is locked', 'NotReadableError')
				),
		}) as unknown as Blob;
	return file;
};

const mountUpload = () =>
	mount(BaseFileUpload, {
		props: { id: 'files', modelValue: [], accept: '.pdf' },
		global: {
			plugins: [
				createI18n({ legacy: false, locale: 'sv', messages: { sv } }),
			],
		},
	});

const drop = async (wrapper: ReturnType<typeof mountUpload>, files: File[]) => {
	await wrapper
		.find('.base-file-upload')
		.trigger('drop', { dataTransfer: { files } });
	await flushPromises();
};

describe('BaseFileUpload', () => {
	test('adds readable files', async () => {
		const wrapper = mountUpload();
		const file = readableFile('a.pdf');

		await drop(wrapper, [file]);

		expect(wrapper.emitted('update:modelValue')).toEqual([[[file]]]);
	});

	test('rejects files that cannot be read and keeps the rest', async () => {
		const wrapper = mountUpload();
		const readable = readableFile('a.pdf');

		await drop(wrapper, [readable, lockedFile('locked.pdf')]);

		expect(wrapper.emitted('update:modelValue')).toEqual([[[readable]]]);
		expect(wrapper.text()).toContain(
			'"locked.pdf" kunde inte läsas. Stäng den i andra program och välj den igen.'
		);
	});

	test('adds nothing when every file is unreadable', async () => {
		const wrapper = mountUpload();

		await drop(wrapper, [lockedFile('locked.pdf')]);

		expect(wrapper.emitted('update:modelValue')).toBeUndefined();
		expect(wrapper.text()).toContain('"locked.pdf" kunde inte läsas.');
	});
});
