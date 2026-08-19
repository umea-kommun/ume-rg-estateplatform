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

export const getLocale = (): string => {
	const storedLocale = sessionStorage.getItem('locale');

	return storedLocale ?? Config.VUE_APP_I18N_LOCALE;
};
export const setLocale = (locale: string): void => {
	moment.locale(locale);
	sessionStorage.setItem('locale', locale);
};

const i18n = createI18n({
	locale: getLocale(),
	fallbackLocale: Config.VUE_APP_I18N_FALLBACK_LOCALE,
	messages: loadLocaleMessages(),
	silentTranslationWarn: true,
});

export default i18n;
