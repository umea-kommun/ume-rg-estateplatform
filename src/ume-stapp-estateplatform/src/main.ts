// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/main.ts @ 84b4a5dc
import './polyfills';
import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import store from './store/store';
import i18n from './plugins/i18next';
import vuetify from './plugins/vuetify';
import Config from '@/utils/Config';
import IAuthManager from './plugins/auth/IAuthManager';
import Validation from './plugins/validation';
import Auth from '@/plugins/auth/index';
import '@turkos/components/styles';
import moment from 'moment';
import 'moment/dist/locale/sv';
import ErrorService from './utils/ErrorService';
import appInsights from './plugins/appInsights';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { trackAppInsightsContext } from '@/utils/appInsightsContext';
// import './registerServiceWorker';

moment.locale(i18n.global.locale);

// eslint-disable-next-line @typescript-eslint/no-explicit-any
Config.loadVarsFromServer((window as any).vueAppServerConfig || {});

const app = createApp(App);

const auth = Auth({ config: Config });
app.provide<IAuthManager>('$auth', auth);

/** Error handler */
// Catches vue errors
app.config.errorHandler = (err, instance, info) => {
	ErrorService.onError({ err, log: true, instance, info });
};
// Catches general errors
window.addEventListener('error', (e: ErrorEvent) => {
	if (ErrorService.isSuppressedError(e.message)) {
		e.stopImmediatePropagation();
		return;
	}
	ErrorService.onError({
		err: e.error,
		log: true,
		info: e.type,
	});
});

app.use(appInsights, {
	baseName: 'ume-stapp-minasidor',
	router,
	appInsightsConfig: {
		connectionString: Config.VUE_APP_APPINSIGHT_CONNECTION_STRING,
		loggingLevelTelemetry: 2,
		enableCorsCorrelation: true,
	},
	onAfterScriptLoaded: (appInsights: ApplicationInsights) => {
		trackAppInsightsContext(appInsights);
	},
});

Validation(i18n);
app.use(store)
	.use(router)
	.use(i18n)
	.use(vuetify)
	.mount('#app');
