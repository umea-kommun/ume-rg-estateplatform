// Pure client-side helpers for the file-upload accept/allowlist logic.
// Extracted from BaseFileUpload.vue so the branches (extension token,
// MIME wildcard, exact MIME, empty File.type fallback) are testable in
// isolation.

// Fallback map from MIME type to common file extensions. Used only when a
// browser reports an empty/unknown `File.type` (e.g. HEIC on non-Apple
// browsers), so we can still match the file against the allowlist.
//
// NOTE: The actual list of allowed file types lives in ONE place —
// the backend config at `WorkOrder:FileValidation:AllowedContentTypes`
// (see appsettings.json). The frontend consumes it via useWorkOrderConfig()
// which builds the `accept` prop passed to BaseFileUpload. To add or remove
// a format, edit that config — no changes needed here.
export const MIME_TO_EXTENSIONS: Record<string, string[]> = {
	'application/pdf': ['.pdf'],
	'image/jpeg': ['.jpg', '.jpeg'],
	'image/png': ['.png'],
	'image/gif': ['.gif'],
	'image/webp': ['.webp'],
	'image/bmp': ['.bmp'],
	'image/tiff': ['.tif', '.tiff'],
	'image/heic': ['.heic', '.heif'],
};

/**
 * Guess a MIME type from the filename extension using MIME_TO_EXTENSIONS.
 * Returns null when the extension is unknown to us.
 */
export function guessMimeFromExtension(filename: string): string | null {
	const lower = filename.toLowerCase();
	for (const [mime, exts] of Object.entries(MIME_TO_EXTENSIONS)) {
		if (exts.some((e) => lower.endsWith(e))) return mime;
	}
	return null;
}

/**
 * When the browser reports an empty `File.type` (e.g. HEIC on Chrome/Windows),
 * FormData will fall back to `application/octet-stream` for the multipart part.
 * The backend then rejects with InvalidContentType before its magic-byte
 * sniffer ever runs. Rewrap the file with a guessed MIME so the claim matches
 * the allowlist and the sniffer gets to confirm the actual bytes.
 */
export function normalizeFileType(file: File): File {
	if (file.type) return file;
	const guessed = guessMimeFromExtension(file.name);
	if (!guessed) return file;
	return new File([file], file.name, {
		type: guessed,
		lastModified: file.lastModified,
	});
}

/**
 * Returns true if the file matches the given `accept` spec (same syntax
 * as the HTML input accept attribute: comma-separated list of MIME types,
 * MIME wildcards like `image/*`, and extensions like `.pdf`).
 *
 * When the accept spec is empty/undefined the file is considered accepted
 * (matches the HTML input behaviour of "no filter").
 */
export function isFileAccepted(
	file: Pick<File, 'name' | 'type'>,
	acceptSpec?: string
): boolean {
	if (!acceptSpec) return true;
	const tokens = acceptSpec
		.split(',')
		.map((s) => s.trim().toLowerCase())
		.filter(Boolean);
	if (tokens.length === 0) return true;

	const fileType = (file.type || '').toLowerCase();
	const fileName = file.name.toLowerCase();

	for (const token of tokens) {
		if (token.startsWith('.')) {
			// Extension token, e.g. ".pdf"
			if (fileName.endsWith(token)) return true;
		} else if (token.endsWith('/*')) {
			// MIME wildcard, e.g. "image/*"
			const prefix = token.slice(0, -1); // "image/"
			if (fileType.startsWith(prefix)) return true;
			// Fallback: match by known extension in the same family.
			for (const [mime, exts] of Object.entries(MIME_TO_EXTENSIONS)) {
				if (
					mime.startsWith(prefix) &&
					exts.some((e) => fileName.endsWith(e))
				) {
					return true;
				}
			}
		} else if (token.includes('/')) {
			// Exact MIME type, e.g. "image/heic"
			if (fileType === token) return true;
			// Fallback: match by extension when browser MIME is missing/unknown.
			const exts = MIME_TO_EXTENSIONS[token];
			if (exts && exts.some((e) => fileName.endsWith(e))) return true;
		}
	}

	return false;
}
