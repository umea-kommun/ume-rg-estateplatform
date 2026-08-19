// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/src/utils/__tests__/Config.test.ts
import Config from '../Config';
import { describe, expect, test } from 'vitest';

describe('Config', () => {
	describe('constructor', () => {
		test('Values from .env are loaded', () => {
			expect(Config.VUE_APP_AUTH_CLASS).toBe('Oauth');
		});
	});

	describe('loadVarsFromServer', () => {
		test('Original value replaced with value from server', () => {
			const valueFromServer = 'OauthChanged';
			const serverEnvVars = { VUE_APP_AUTH_CLASS: valueFromServer };
			Config.loadVarsFromServer(serverEnvVars);
			expect(Config.VUE_APP_AUTH_CLASS).toBe(valueFromServer);
		});
	});
});
