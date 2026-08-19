// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/utils/httpClient.ts @ 84b4a5dc
import Axios, { type CreateAxiosDefaults } from 'axios';
import { getAppInsightsContext } from '@/utils/appInsightsContext';

export function createHttpClient(config?: CreateAxiosDefaults) {
	const instance = Axios.create(config);
	instance.interceptors.request.use((config) => {
		const { sessionId, userId } = getAppInsightsContext();
		if (sessionId) config.headers['X-AI-Session-Id'] = sessionId;
		if (userId) config.headers['X-AI-User-Id'] = userId;
		return config;
	});
	return instance;
}
