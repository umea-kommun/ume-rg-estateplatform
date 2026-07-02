import { computed, ref, type ComputedRef } from 'vue';
import { useI18n } from 'vue-i18n';
import { getValidationErrors, type ValidationErrors } from './validationErrors';

/**
 * Composable for handling server-side validation errors from ValidationProblemDetails responses.
 * Provides reactive state, field-level error extraction, and i18n translation of error codes.
 *
 * @param translationPrefix - i18n key prefix for error code lookup (e.g. "app.error.estate.validation")
 */
export function useServerValidation(translationPrefix: string) {
	const { t } = useI18n();
	const serverErrors = ref<ValidationErrors | null>(null);

	function translateCodes(codes: string[]): string[] {
		return codes.map((code) => t(`${translationPrefix}.${code}`, code));
	}

	/**
	 * Returns a reactive computed ref with translated error messages for a
	 * specific field. Intended to be called once at setup time and the
	 * returned ref stored — do not call reactively (e.g. inside another
	 * computed or directly in a template expression), which would allocate
	 * a new ComputedRef on every read.
	 */
	function fieldError(field: string): ComputedRef<string> {
		return computed(() => {
			const codes = serverErrors.value?.[field];
			if (!codes?.length) return '';
			return translateCodes(codes).join(', ');
		});
	}

	/**
	 * Get translated file errors for BaseFileUpload (keys matching "files" and "files[N]").
	 */
	const fileErrors = computed(() => {
		if (!serverErrors.value) return undefined;
		const result: Record<string, string[]> = {};
		for (const [key, codes] of Object.entries(serverErrors.value)) {
			if (key === 'files' || key.startsWith('files[')) {
				result[key] = translateCodes(codes);
			}
		}
		return Object.keys(result).length > 0 ? result : undefined;
	});

	/**
	 * Try to set validation errors from a caught error.
	 * Returns true if it was a validation error (400 with field errors), false otherwise.
	 */
	function setFromError(err: unknown): boolean {
		const errors = getValidationErrors(err);
		if (errors) {
			serverErrors.value = errors;
			return true;
		}
		return false;
	}

	function clear() {
		serverErrors.value = null;
	}

	return { serverErrors, fileErrors, fieldError, setFromError, clear };
}
