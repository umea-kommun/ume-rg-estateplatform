// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/src/utils/validationErrors.ts
import { isAxiosError } from 'axios';

/**
 * Validation errors from a ProblemDetails / ValidationProblemDetails response.
 * Keys are field names (e.g. "description", "files[0]"), values are arrays of error codes.
 */
export type ValidationErrors = Record<string, string[]>;

/**
 * Extracts structured validation errors from an Axios 400 response
 * that returns a ValidationProblemDetails body (RFC 7807 with "errors" dict).
 *
 * Returns null if the error is not a validation error.
 */
export function getValidationErrors(err: unknown): ValidationErrors | null {
	if (!isAxiosError(err) || err.response?.status !== 400) {
		return null;
	}

	const errors = err.response.data?.errors;
	if (
		!errors ||
		typeof errors !== 'object' ||
		Array.isArray(errors) ||
		Object.keys(errors).length === 0
	) {
		return null;
	}

	return errors as ValidationErrors;
}

/**
 * Returns true if the error is a validation error with field-level errors.
 */
export function isValidationError(err: unknown): boolean {
	return getValidationErrors(err) !== null;
}
