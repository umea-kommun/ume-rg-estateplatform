// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/src/utils/appInsightsContext.ts
import type {
	ApplicationInsights,
	ITelemetryItem,
} from '@microsoft/applicationinsights-web';
import Config from '@/utils/Config';

// The concrete SDK context exposes session helpers that aren't on the public
// ITelemetryContext type, so we reach them through this minimal shape.
interface SessionContext {
	getSessionId?: () => string | null;
	sessionManager?: {
		update?: () => void;
		automaticSession?: { id?: string };
	};
}

let appInsights: ApplicationInsights | undefined;
let capturedSessionId = '';

/**
 * Stamp cloud_RoleName on the frontend's own telemetry, and keep a reference to
 * the SDK so the session and anonymous user ids it manages can be read at
 * request time and propagated to backends (landing in the same session_Id /
 * user_Id columns). The initializer also captures the session id from ext.app
 * as a fallback (the ai.session.id tag isn't populated until the sender stage).
 */
export function trackAppInsightsContext(instance: ApplicationInsights): void {
	appInsights = instance;
	const cloudRoleName = Config.VUE_APP_CLOUD_ROLE_NAME;
	instance.addTelemetryInitializer((item: ITelemetryItem) => {
		item.tags ??= [] as unknown as NonNullable<ITelemetryItem['tags']>;
		if (cloudRoleName) {
			(item.tags as unknown as Record<string, string>)['ai.cloud.role'] =
				cloudRoleName;
		}
		const sesId = (item as { ext?: { app?: { sesId?: string } } }).ext?.app
			?.sesId;
		if (sesId) capturedSessionId = sesId;
	});
}

export function getAppInsightsContext(): { sessionId: string; userId: string } {
	const context = appInsights?.context;
	let sessionId = capturedSessionId;
	try {
		const sessionCtx = context as unknown as SessionContext | undefined;
		sessionCtx?.sessionManager?.update?.();
		sessionId =
			sessionCtx?.getSessionId?.() ||
			sessionCtx?.sessionManager?.automaticSession?.id ||
			capturedSessionId;
	} catch {
		sessionId = capturedSessionId;
	}
	return {
		sessionId: sessionId || '',
		userId: context?.user?.id || '',
	};
}
