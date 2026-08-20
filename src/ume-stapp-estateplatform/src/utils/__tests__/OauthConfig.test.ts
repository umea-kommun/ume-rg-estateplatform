import OauthConfig from '../OauthConfig';
import { describe, expect, test, beforeEach } from 'vitest';

describe('OauthConfig', () => {
	describe('constructor', () => {
		let envTest: any;

		beforeEach(() => {
			envTest = {
				VUE_APP_OAUTH_ADMIN_CLIENT_ID: 'CLIENT_ID',
				VUE_APP_OAUTH_LOGIN_URL: 'LOGIN_URL',
				VUE_APP_OAUTH_REDIRECT_URL: 'REDIRECT_URL',
				VUE_APP_OAUTH_FETCH_TOKEN_API_URL: 'FETCH_TOKEN_API_URL',
				VUE_APP_OAUTH_LOGOUT_URL: 'LOGOUT_URL',
			};
		});

		test('All properties set correctly', () => {
			const config = new OauthConfig(envTest);
			expect(config.clientId).toBe(envTest.VUE_APP_OAUTH_ADMIN_CLIENT_ID);
			expect(config.loginUrl).toBe(envTest.VUE_APP_OAUTH_LOGIN_URL);
			expect(config.redirectUrl).toBe(envTest.VUE_APP_OAUTH_REDIRECT_URL);
			expect(config.fetchTokenApiUrl).toBe(
				envTest.VUE_APP_OAUTH_FETCH_TOKEN_API_URL
			);
			expect(config.logoutUrl).toBe(envTest.VUE_APP_OAUTH_LOGOUT_URL);
		});

		test('Throws exception when client id is missing', () => {
			envTest.VUE_APP_OAUTH_ADMIN_CLIENT_ID = null;
			expect(() => {
				new OauthConfig(envTest);
			}).toThrow('No oauth client id for admin is configured');
		});

		test('Throws exception when login url is missing', () => {
			envTest.VUE_APP_OAUTH_LOGIN_URL = null;
			expect(() => {
				new OauthConfig(envTest);
			}).toThrow('No oauth login url is configured');
		});

		test('Throws exception when redirect url is missing', () => {
			envTest.VUE_APP_OAUTH_REDIRECT_URL = null;
			expect(() => {
				new OauthConfig(envTest);
			}).toThrow('No oauth redirect url is configured');
		});

		test('Throws exception when fetch token api url is missing', () => {
			envTest.VUE_APP_OAUTH_FETCH_TOKEN_API_URL = null;
			expect(() => {
				new OauthConfig(envTest);
			}).toThrow('No api url used to fetch tokens is configured');
		});

		test('Throws exception when logout url is missing', () => {
			envTest.VUE_APP_OAUTH_LOGOUT_URL = null;
			expect(() => {
				new OauthConfig(envTest);
			}).toThrow('No oauth logout url is configured');
		});
	});
});
