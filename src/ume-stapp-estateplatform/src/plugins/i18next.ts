// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/src/plugins/i18next.ts
import Config from '@/Config';
import { createI18n } from 'vue-i18n';
import sv from '@/locales/sv.json';
import en from '@/locales/en.json';
import moment from 'moment';

/**
 * Translation plugin using VueI18n https://kazupon.github.io/vue-i18n
 */

function loadLocaleMessages() {
	const messages = {
		sv,
		en,
	};
	return messages;
}

/**
 * The locale is kept in localStorage, not sessionStorage, so the choice
 * survives closing the browser and applies on the next visit.
 */
export const getLocale = (): string => {
	const storedLocale = localStorage.getItem('locale');
	const isSupported =
		storedLocale !== null &&
		Object.hasOwn(loadLocaleMessages(), storedLocale);

	return isSupported ? storedLocale : Config.VUE_APP_I18N_LOCALE;
};
export const setLocale = (locale: string): void => {
	moment.locale(locale);
	localStorage.setItem('locale', locale);
	document.documentElement.lang = locale;
};

const locale = getLocale();
document.documentElement.lang = locale;

const i18n = createI18n({
	locale,
	fallbackLocale: Config.VUE_APP_I18N_FALLBACK_LOCALE,
	messages: loadLocaleMessages(),
	silentTranslationWarn: true,
});

export default i18n;
