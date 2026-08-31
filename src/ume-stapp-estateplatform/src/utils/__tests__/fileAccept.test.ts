import { describe, it, expect } from 'vitest';
import {
	isFileAccepted,
	guessMimeFromExtension,
	normalizeFileType,
	MIME_TO_EXTENSIONS,
} from '../fileAccept';

// isFileAccepted only reads `name` and `type`, so a plain object is enough for
// the branch-coverage tests and avoids depending on File constructor semantics.
function fakeFile(name: string, type = ''): Pick<File, 'name' | 'type'> {
	return { name, type };
}

describe('isFileAccepted', () => {
	it('accepts everything when acceptSpec is undefined', () => {
		expect(isFileAccepted(fakeFile('anything.xyz'), undefined)).toBe(true);
	});

	it('accepts everything when acceptSpec is an empty string', () => {
		expect(isFileAccepted(fakeFile('anything.xyz'), '')).toBe(true);
	});

	it('accepts everything when acceptSpec contains only whitespace/commas', () => {
		expect(isFileAccepted(fakeFile('anything.xyz'), ' , , ')).toBe(true);
	});

	describe('extension token', () => {
		it('accepts a matching extension regardless of MIME', () => {
			expect(
				isFileAccepted(fakeFile('report.PDF', ''), '.pdf')
			).toBe(true);
		});

		it('rejects a non-matching extension', () => {
			expect(isFileAccepted(fakeFile('report.docx'), '.pdf')).toBe(false);
		});

		it('matches case-insensitively on both sides', () => {
			expect(isFileAccepted(fakeFile('IMG.JPG'), '.JPG')).toBe(true);
		});
	});

	describe('MIME wildcard token', () => {
		it('accepts a MIME in the same family', () => {
			expect(
				isFileAccepted(fakeFile('photo.png', 'image/png'), 'image/*')
			).toBe(true);
		});

		it('rejects a MIME in a different family', () => {
			expect(
				isFileAccepted(
					fakeFile('doc.pdf', 'application/pdf'),
					'image/*'
				)
			).toBe(false);
		});

		it('falls back to extension when MIME is empty (e.g. HEIC on Chrome/Windows)', () => {
			expect(
				isFileAccepted(fakeFile('photo.heic', ''), 'image/*')
			).toBe(true);
		});

		it('does not fall back for extensions outside the family', () => {
			// .pdf lives under application/pdf, so it should not slip through image/*
			expect(isFileAccepted(fakeFile('doc.pdf', ''), 'image/*')).toBe(
				false
			);
		});
	});

	describe('exact MIME token', () => {
		it('accepts an exact MIME match', () => {
			expect(
				isFileAccepted(fakeFile('a.heic', 'image/heic'), 'image/heic')
			).toBe(true);
		});

		it('rejects a different exact MIME', () => {
			expect(
				isFileAccepted(fakeFile('a.png', 'image/png'), 'image/heic')
			).toBe(false);
		});

		it('falls back to extension when browser reports empty MIME', () => {
			expect(
				isFileAccepted(fakeFile('photo.heic', ''), 'image/heic')
			).toBe(true);
			expect(
				isFileAccepted(fakeFile('photo.heif', ''), 'image/heic')
			).toBe(true);
		});

		it('does not accept when neither MIME nor extension matches', () => {
			expect(
				isFileAccepted(fakeFile('malware.exe', ''), 'image/heic')
			).toBe(false);
		});
	});

	describe('mixed acceptSpec', () => {
		const spec = 'image/jpeg,image/png,image/heic,application/pdf';

		it('accepts a matching exact MIME within a comma list', () => {
			expect(
				isFileAccepted(fakeFile('a.pdf', 'application/pdf'), spec)
			).toBe(true);
		});

		it('accepts HEIC with empty MIME via extension fallback', () => {
			expect(isFileAccepted(fakeFile('photo.heic', ''), spec)).toBe(true);
		});

		it('rejects an unknown extension with empty MIME', () => {
			expect(isFileAccepted(fakeFile('malware.exe', ''), spec)).toBe(
				false
			);
		});
	});
});

describe('guessMimeFromExtension', () => {
	it('returns the MIME for a known extension', () => {
		expect(guessMimeFromExtension('photo.heic')).toBe('image/heic');
		expect(guessMimeFromExtension('photo.HEIF')).toBe('image/heic');
		expect(guessMimeFromExtension('report.pdf')).toBe('application/pdf');
		expect(guessMimeFromExtension('a.jpg')).toBe('image/jpeg');
		expect(guessMimeFromExtension('a.jpeg')).toBe('image/jpeg');
	});

	it('is case-insensitive on the extension', () => {
		expect(guessMimeFromExtension('IMG.PNG')).toBe('image/png');
	});

	it('returns null for unknown extensions', () => {
		expect(guessMimeFromExtension('malware.exe')).toBeNull();
		expect(guessMimeFromExtension('noext')).toBeNull();
	});

	it('covers every extension listed in MIME_TO_EXTENSIONS', () => {
		for (const [mime, exts] of Object.entries(MIME_TO_EXTENSIONS)) {
			for (const ext of exts) {
				expect(guessMimeFromExtension(`file${ext}`)).toBe(mime);
			}
		}
	});
});

describe('normalizeFileType', () => {
	it('returns the same instance when File.type is already set', () => {
		const f = new File(['x'], 'photo.heic', { type: 'image/heic' });
		expect(normalizeFileType(f)).toBe(f);
	});

	it('rewraps with a guessed MIME when File.type is empty', () => {
		const f = new File(['x'], 'photo.heic', { type: '' });
		const out = normalizeFileType(f);
		expect(out).not.toBe(f);
		expect(out.type).toBe('image/heic');
		expect(out.name).toBe('photo.heic');
	});

	it('returns the same instance when extension is unknown', () => {
		const f = new File(['x'], 'malware.exe', { type: '' });
		expect(normalizeFileType(f)).toBe(f);
	});

	it('preserves lastModified when rewrapping', () => {
		const f = new File(['x'], 'photo.heic', {
			type: '',
			lastModified: 1_700_000_000_000,
		});
		const out = normalizeFileType(f);
		expect(out.lastModified).toBe(1_700_000_000_000);
	});
});
