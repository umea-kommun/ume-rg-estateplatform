import { describe, expect, test, vi } from 'vitest';

vi.mock('vue-i18n', () => ({
	useI18n: () => ({
		t: (key: string, params?: Record<string, string>) =>
			params ? `${key}:${Object.values(params).join(',')}` : key,
	}),
}));

vi.mock('@/Config', () => ({
	default: {},
}));

import { useEnvironmentBadge } from '../useEnvironmentBadge';

describe('useEnvironmentBadge', () => {
	test('Hides the badge in production', () => {
		const { environmentBadge, showEnvironmentBadge, environmentPrefix } =
			useEnvironmentBadge({
				environmentName: 'prod',
				hostname: 'fastighetsportalen.umea.se',
			});

		expect(environmentBadge.value.key).toBe('prod');
		expect(showEnvironmentBadge.value).toBe(false);
		expect(environmentPrefix.value).toBe('');
	});

	test('Shows an accented header in test', () => {
		const {
			environmentBadge,
			showEnvironmentBadge,
			environmentHeaderStyle,
			environmentPrefix,
		} = useEnvironmentBadge({
			environmentName: 'test',
			hostname: 'fastighetsportalen.test.umea.se',
		});

		expect(environmentBadge.value.key).toBe('test');
		expect(showEnvironmentBadge.value).toBe(true);
		expect(environmentHeaderStyle.value['--environment-accent']).toBe(
			'#d97706'
		);
		expect(environmentPrefix.value).toBe(
			'[component.appHeader.environment.test] '
		);
	});

	test('Falls back to the hostname when no environment name is configured', () => {
		expect(
			useEnvironmentBadge({
				environmentName: null,
				hostname: 'fastighetsportalen.dev.umea.se',
			}).environmentBadge.value.key
		).toBe('dev');

		expect(
			useEnvironmentBadge({
				environmentName: '',
				hostname: 'fastighetsportalen.umea.se',
				isLocalDevelopment: false,
			}).environmentBadge.value.key
		).toBe('prod');
	});

	test('Localhost wins over a deployed environment name', () => {
		expect(
			useEnvironmentBadge({
				environmentName: 'prod',
				hostname: 'localhost',
			}).environmentBadge.value.key
		).toBe('local');
	});

	test('Falls back to unknown for an unrecognised deployment', () => {
		expect(
			useEnvironmentBadge({
				environmentName: '',
				hostname: 'fastighetsportalen.example.com',
				isLocalDevelopment: false,
			}).environmentBadge.value.key
		).toBe('unknown');
	});
});
