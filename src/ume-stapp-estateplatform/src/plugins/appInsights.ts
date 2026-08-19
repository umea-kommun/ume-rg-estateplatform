// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/plugins/appInsights.ts @ 84b4a5dc
import { ApplicationInsights, Util } from '@microsoft/applicationinsights-web';
import { RouteLocationNormalizedLoadedGeneric, Router } from 'vue-router';

export let appInsights: ApplicationInsights;

function isSamePage(
	to: RouteLocationNormalizedLoadedGeneric,
	from: RouteLocationNormalizedLoadedGeneric
) {
	return (
		to.name === from.name &&
		to.path === from.path &&
		JSON.stringify(to.params ?? {}) === JSON.stringify(from.params ?? {})
	);
}

export default {
	install(
		app: unknown,
		{
			baseName,
			appInsightsConfig,
			router,
			onAfterScriptLoaded,
		}: {
			baseName: string;
			appInsightsConfig: { [key: string]: string | boolean | number };
			router: Router;
			onAfterScriptLoaded: unknown;
		}
	): void {
		if (!appInsightsConfig.connectionString) {
			console.warn(
				'Please provide connection string for Application Insights, logging is disabled.'
			);
			return;
		}
		/** App Insights */
		if (!appInsights) {
			appInsights = new ApplicationInsights({
				config: appInsightsConfig,
			});
			appInsights.loadAppInsights();
			if (typeof onAfterScriptLoaded === 'function') {
				onAfterScriptLoaded(appInsights);
			}
		}

		// Track route change (same way as done in 'vue-application-insights' that we used before)
		if (router) {
			router.beforeEach((route, from, next) => {
				if (from && isSamePage(route, from)) {
					return next();
				}

				const name = baseName + ' / ' + route.name?.toString();
				appInsights.context.telemetryTrace.traceID =
					Util.generateW3CId();
				appInsights.context.telemetryTrace.name =
					route.name?.toString();
				appInsights.startTrackPage(name);
				next();
			});

			router.afterEach((route, from) => {
				if (from && isSamePage(route, from)) {
					return;
				}

				const name = baseName + ' / ' + route.name?.toString();
				const url =
					location.protocol + '//' + location.host + route.fullPath;
				appInsights.stopTrackPage(name, url);
				appInsights.flush();
			});
		}
	},
};
