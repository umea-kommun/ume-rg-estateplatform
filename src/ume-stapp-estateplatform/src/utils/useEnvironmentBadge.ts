import Config from '@/Config';
import { computed, type CSSProperties } from 'vue';
import { useI18n } from 'vue-i18n';

export type EnvironmentKey = 'prod' | 'test' | 'dev' | 'local' | 'unknown';

export interface EnvironmentBadge {
	key: EnvironmentKey;
	labelKey: string;
	accentColor: string;
	backgroundColor: string;
	textColor: string;
}

interface UseEnvironmentBadgeOptions {
	environmentName?: string | null;
	hostname?: string | null;
	isLocalDevelopment?: boolean;
}

const LOCAL_HOSTS = new Set(['localhost', '127.0.0.1', '::1', '[::1]']);

const ENVIRONMENT_BADGE_CONFIG: Record<
	EnvironmentKey,
	Omit<EnvironmentBadge, 'key'>
> = {
	prod: {
		labelKey: 'component.appHeader.environment.prod',
		accentColor: '#006e1e',
		backgroundColor: 'rgba(0, 110, 30, 0.12)',
		textColor: '#005117',
	},
	test: {
		labelKey: 'component.appHeader.environment.test',
		accentColor: '#d97706',
		backgroundColor: 'rgba(217, 119, 6, 0.16)',
		textColor: '#8a4b00',
	},
	dev: {
		labelKey: 'component.appHeader.environment.dev',
		accentColor: '#2857d8',
		backgroundColor: 'rgba(40, 87, 216, 0.14)',
		textColor: '#16368f',
	},
	local: {
		labelKey: 'component.appHeader.environment.local',
		accentColor: '#8c2f7c',
		backgroundColor: 'rgba(140, 47, 124, 0.14)',
		textColor: '#69225d',
	},
	unknown: {
		labelKey: 'component.appHeader.environment.unknown',
		accentColor: '#616161',
		backgroundColor: 'rgba(97, 97, 97, 0.16)',
		textColor: '#424242',
	},
};

function getEnvironmentFromHost(
	hostname?: string | null
): EnvironmentKey | null {
	const normalizedHost = hostname?.trim().toLowerCase() ?? '';

	if (!normalizedHost) {
		return null;
	}

	if (LOCAL_HOSTS.has(normalizedHost)) {
		return 'local';
	}

	if (normalizedHost.includes('.test.')) {
		return 'test';
	}

	if (normalizedHost.includes('.dev.')) {
		return 'dev';
	}

	if (normalizedHost.endsWith('.umea.se')) {
		return 'prod';
	}

	return null;
}

/**
 * VUE_APP_ENV decides, hostname is the fallback for when the build was
 * deployed somewhere its env file did not anticipate.
 */
function resolveEnvironmentKey(
	environmentName?: string | null,
	hostname?: string | null,
	isLocalDevelopment = import.meta.env.DEV
): EnvironmentKey {
	if (LOCAL_HOSTS.has(hostname?.trim().toLowerCase() ?? '')) {
		return 'local';
	}

	switch (environmentName?.trim().toLowerCase() ?? '') {
		case 'production':
		case 'prod':
			return 'prod';
		case 'test':
		case 'testing':
		case 'stage':
		case 'staging':
			return 'test';
		case 'dev':
			return 'dev';
		case 'development':
		case 'local':
		case 'localhost':
			return 'local';
	}

	const hostEnvironment = getEnvironmentFromHost(hostname);
	if (hostEnvironment) {
		return hostEnvironment;
	}

	if (isLocalDevelopment) {
		return 'local';
	}

	return 'unknown';
}

function shouldShowEnvironmentBadge(environmentKey: EnvironmentKey): boolean {
	return environmentKey !== 'prod';
}

function getDefaultHostname(): string {
	if (typeof window === 'undefined') {
		return '';
	}

	return window.location.hostname;
}

/**
 * Colours the app header so a non-production environment is obvious at a
 * glance. Production shows nothing.
 */
export const useEnvironmentBadge = (
	options: UseEnvironmentBadgeOptions = {}
) => {
	const { t } = useI18n();

	const environmentBadge = computed<EnvironmentBadge>(() => {
		const key = resolveEnvironmentKey(
			options.environmentName ?? Config.VUE_APP_ENV,
			options.hostname ?? getDefaultHostname(),
			options.isLocalDevelopment ?? import.meta.env.DEV
		);

		return {
			key,
			...ENVIRONMENT_BADGE_CONFIG[key],
		};
	});

	const environmentBadgeLabel = computed(() =>
		t(environmentBadge.value.labelKey).toString()
	);

	const environmentBadgeTitle = computed(() =>
		t('component.appHeader.environment.current', {
			env: environmentBadgeLabel.value,
		}).toString()
	);

	const showEnvironmentBadge = computed(() =>
		shouldShowEnvironmentBadge(environmentBadge.value.key)
	);

	const environmentHeaderStyle = computed<CSSProperties>(() => {
		if (!showEnvironmentBadge.value) {
			return {};
		}

		return {
			'--environment-accent': environmentBadge.value.accentColor,
			'--environment-badge-background':
				environmentBadge.value.backgroundColor,
			'--environment-badge-color': environmentBadge.value.textColor,
		};
	});

	/** `"[TEST] "`, for prefixing the document title. Empty in production. */
	const environmentPrefix = computed(() =>
		showEnvironmentBadge.value ? `[${environmentBadgeLabel.value}] ` : ''
	);

	return {
		environmentBadge,
		environmentBadgeLabel,
		environmentBadgeTitle,
		showEnvironmentBadge,
		environmentHeaderStyle,
		environmentPrefix,
	};
};
