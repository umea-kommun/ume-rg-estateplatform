import { configure, defineRule } from 'vee-validate';
import AllRules from '@vee-validate/rules';
import moment from 'moment';
import Organisationsnummer from 'organisationsnummer';
import i18nInstance from './i18next';

/**
 * Installera Vee-Validate
 * @link https://baianat.github.io/vee-validate
 * Configuration of vee: https://baianat.github.io/vee-validate/configuration.html
 */

function initialize(i18n: typeof i18nInstance): void {
	Object.keys(AllRules).forEach((rule) => {
		defineRule(rule, AllRules[rule]);
	});

	configure({
		generateMessage: (context): string => {
			let params: unknown[] = [];
			if (
				context.rule?.params?.length &&
				Array.isArray(context.rule.params)
			) {
				params = context.rule.params;
			}
			return i18n.global.t(
				'app.validation.messages.' + context?.rule?.name,
				[context.field, ...params]
			);
		},
	});

	/**
	 * Vali-date
	 */
	defineRule('validDate', (value: string) => {
		if (!value?.length) {
			return true;
		}
		const date = moment(value, 'YYYY-MM-DD', true);
		return date.isValid();
	});

	/**
	 * Vali-date min date
	 */
	defineRule('minDate', (value: string, [minDate]: string) => {
		if (!value || moment(value).isSameOrAfter(moment(minDate))) {
			return true;
		}
		return false;
	});

	/**
	 * Vali-date max date
	 */
	defineRule('maxDate', (value: string, [maxDate]: string) => {
		if (!value || moment(value).isSameOrBefore(moment(maxDate))) {
			return true;
		}
		return false;
	});

	/**
	 * Invalid date
	 * This rule is only used when the date is invalid, so it always returns false
	 */
	defineRule('invalidDate', () => {
		return false;
	});

	/**
	 * Validate phone number
	 */
	defineRule('phone', (value: string) => {
		if (!value) {
			return true;
		}

		let valid = true;
		const regexp = new RegExp(
			/^\s*(?:\+?(\d{1,3}))?([-. (]*(\d{3})[-. )]*)?((\d{3})[-. ]*(\d{2,4})(?:[-.x ]*(\d+))?)\s*$/
		);
		valid = !!regexp.exec(value);
		return valid;
	});

	/**
	 * Valid SSN
	 */
	defineRule('validPersNumber', (value: string) => {
		if (!value) {
			return true;
		}
		if (
			Organisationsnummer.valid(value) &&
			Organisationsnummer.parse(value).isPersonnummer()
		) {
			return true;
		}

		// Also allow temporary ssn
		if (
			/\d{8}-?TF\d{2}/.test(value) // 12345678-TF12 or 12345678TF12 etc.
		) {
			return true;
		}
		return false;
	});
}

export default initialize;
