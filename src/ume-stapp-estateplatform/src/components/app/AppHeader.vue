<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/app/AppHeader.vue -->
<template>
	<header
		class="app-header"
		:class="[size, { 'app-header--environment': showEnvironmentBadge }]"
		:style="environmentHeaderStyle"
	>
		<div id="skip">
			<a href="#main-content" class="subheading text-center">{{
				$t('app.nav.skipToContent')
			}}</a>
		</div>
		<div class="header-content">
			<div class="logo-wrap">
				<router-link
					:to="{ name: startPageRoute }"
					:title="$t('app.nav.startPage')"
				>
					<img
						class="logo"
						height="80"
						:src="logoGreen"
						:alt="$t('app.nav.logo')"
				/></router-link>

				<div class="title">
					<div class="separator"></div>
					{{ headerTitle }}
				</div>

				<span
					v-if="showEnvironmentBadge"
					class="environment-badge"
					:aria-label="environmentBadgeTitle"
					:title="environmentBadgeTitle"
				>
					{{ environmentBadgeLabel }}
				</span>
			</div>

			<!--
				Primary navigation, desktop only. Below
				$estate-mobile-threshold the same targets live in the menu.
			-->
			<nav
				v-if="user.isAuthenticated"
				class="primary-nav"
				:aria-label="$t('component.appHeader.nav.label')"
			>
				<router-link
					:to="{ name: EstateRoutes.Search }"
					@click="trackNav('search')"
				>
					<v-icon icon="search" :size="18" />
					{{ $t('component.appHeader.nav.search') }}
				</router-link>
				<template v-if="isErrorReportEnabled">
					<router-link
						:to="{ name: EstateRoutes.FaultReport }"
						@click="trackNav('faultReport')"
					>
						<v-icon icon="warning" :size="18" />
						{{ $t('app.nav.services.faultReport') }}
					</router-link>
					<router-link
						:to="{ name: EstateRoutes.Order }"
						@click="trackNav('order')"
					>
						<v-icon icon="handyman" :size="18" />
						{{ $t('app.nav.services.order') }}
					</router-link>
					<router-link
						v-if="isSpaceRequirementAllowed"
						:to="{ name: EstateRoutes.SpaceRequirement }"
						@click="trackNav('spaceRequirement')"
					>
						<v-icon icon="space_dashboard" :size="18" />
						{{ $t('app.nav.services.spaceRequirement') }}
					</router-link>
				</template>
			</nav>

			<div
				class="menu-wrap"
				v-if="
					user.isAuthenticated ||
					(!user.isAuthenticated &&
						route.name !== AppRoutes.AuthLogin)
				"
			>
				<!--
					Two-language toggle: shows the current language's flag,
					clicking switches to the other language.
				-->
				<v-btn
					class="locale-btn"
					variant="text"
					:title="$t('component.appHeader.locale.change')"
					:aria-label="$t('component.appHeader.locale.change')"
					@click="selectedLocale = nextLanguage.locale"
				>
					<svg
						v-if="selectedLocale === 'en'"
						class="flag"
						viewBox="0 0 16 10"
						aria-hidden="true"
					>
						<rect width="16" height="10" fill="#012169" />
						<path
							d="M0 0 L16 10 M16 0 L0 10"
							stroke="#fff"
							stroke-width="2"
						/>
						<path
							d="M0 0 L16 10 M16 0 L0 10"
							stroke="#c8102e"
							stroke-width="0.8"
						/>
						<path
							d="M8 0 V10 M0 5 H16"
							stroke="#fff"
							stroke-width="3.4"
						/>
						<path
							d="M8 0 V10 M0 5 H16"
							stroke="#c8102e"
							stroke-width="2"
						/>
					</svg>
					<svg
						v-else
						class="flag"
						viewBox="0 0 16 10"
						aria-hidden="true"
					>
						<rect width="16" height="10" fill="#005cbf" />
						<rect x="5" width="2" height="10" fill="#fecc00" />
						<rect y="4" width="16" height="2" fill="#fecc00" />
					</svg>
				</v-btn>
				<div v-if="user.isAuthenticated" class="user-wrap">
					<v-icon icon="account_circle" />
					<div class="name">{{ user.fullName }}</div>
				</div>
				<v-btn
					v-if="!user.isAuthenticated"
					prepend-icon="lock"
					flat
					color="primary"
					@click="navigateLogin"
					>{{ $t('component.appHeader.loginButton') }}</v-btn
				>

				<v-menu attach="" v-if="user.isAuthenticated">
					<template v-slot:activator="{ props }">
						<v-btn
							v-bind="props"
							prepend-icon="menu"
							color="primary"
							class="menu-btn"
							variant="outlined"
							>{{ $t('component.appHeader.menuButton') }}</v-btn
						>
					</template>
					<v-list>
						<v-list-item :to="{ name: startPageRoute }">
							<v-list-item-title>{{
								$t('component.appHeader.menu.appStart')
							}}</v-list-item-title>
						</v-list-item>
						<hr v-if="isErrorReportEnabled" />
						<v-list-item
							v-if="isErrorReportEnabled"
							:to="{ name: EstateRoutes.FaultReport }"
						>
							<v-list-item-title>{{
								$t('app.nav.services.faultReport')
							}}</v-list-item-title>
						</v-list-item>
						<v-list-item
							v-if="isErrorReportEnabled"
							:to="{ name: EstateRoutes.Order }"
						>
							<v-list-item-title>{{
								$t('app.nav.services.order')
							}}</v-list-item-title>
						</v-list-item>
						<v-list-item
							v-if="
								isErrorReportEnabled &&
								isSpaceRequirementAllowed
							"
							:to="{ name: EstateRoutes.SpaceRequirement }"
						>
							<v-list-item-title>{{
								$t('app.nav.services.spaceRequirement')
							}}</v-list-item-title>
						</v-list-item>
						<hr v-if="isErrorReportEnabled" />
						<v-list-item :to="{ name: EstateRoutes.AboutWebsite }">
							<v-list-item-title>{{
								$t('component.appHeader.menu.about')
							}}</v-list-item-title>
						</v-list-item>
						<v-list-group :value="selectedLocale">
							<template v-slot:activator="{ props }">
								<v-list-item v-bind="props" @click.prevent.stop>
									{{
										$t('component.appHeader.locale.change')
									}}
								</v-list-item>
							</template>
							<v-list-item
								v-for="(item, index) in languages"
								:key="index"
								:value="index"
								@click="selectedLocale = item.locale"
								:append-icon="
									selectedLocale === item.locale
										? 'check'
										: undefined
								"
								:active="selectedLocale === item.locale"
								:base-color="
									selectedLocale === item.locale
										? 'primary'
										: ''
								"
								>{{ item.title }}
							</v-list-item>
						</v-list-group>

						<hr v-if="user.isAuthenticated" />
						<v-list-item
							v-if="user.isAuthenticated"
							@click="logout"
						>
							<v-list-item-title>{{
								$t('component.appHeader.menu.logout')
							}}</v-list-item-title>
						</v-list-item>
					</v-list>
				</v-menu>
			</div>
		</div>
	</header>
</template>

<script setup lang="ts">
import { computed, inject, PropType } from 'vue';
import { useI18n } from 'vue-i18n';
import { setLocale } from '@/plugins/i18next';
import IAuthManager from '@/plugins/auth/IAuthManager';
import { RouterLink, useRoute, useRouter } from 'vue-router';
import { AppRoutes, EstateRoutes } from '@/router/routes';
import { useFeatureFlags } from '@/utils/useFeatureFlags';
import { useCurrentUser } from '@/utils/useCurrentUser';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import {
	AppContentSize,
	AppHeaderTitle,
	EstateOrderCategory,
} from '@/models/Enums';
import logoGreen from '@/assets/logo_green.png';
import { appInsights } from '@/plugins/appInsights';
import { useEnvironmentBadge } from '@/utils/useEnvironmentBadge';

defineProps({
	size: {
		type: String as PropType<AppContentSize>,
		default: AppContentSize.Default,
	},
});

const route = useRoute();
const router = useRouter();
const store = useStore<IRootState>();
const $auth = inject('$auth') as IAuthManager;
const { t, locale } = useI18n();

const user = computed(() => store.state.user);

const {
	environmentBadgeLabel,
	environmentBadgeTitle,
	showEnvironmentBadge,
	environmentHeaderStyle,
} = useEnvironmentBadge();

const { isEnabled } = useFeatureFlags();
const isErrorReportEnabled = computed(() => isEnabled('ErrorReport'));

// "Förändrade lokalbehov" can be restricted to an AAD group (WorkOrder:RequiredGroupByType).
// Non-members get the type stripped server-side, so don't advertise it to them.
const { canCreateWorkOrderType } = useCurrentUser();
const isSpaceRequirementAllowed = computed(() =>
	canCreateWorkOrderType(EstateOrderCategory.SpaceRequirement)
);

const headerTitle = computed(() => {
	if (route.meta.title) {
		return t('component.appHeader.title.' + route.meta.title);
	}
	return t('component.appHeader.title.' + AppHeaderTitle.Default);
});

const trackNav = (target: string) => {
	appInsights?.trackEvent({
		name: 'EstateHeaderNavClicked',
		properties: {
			target,
			url: window.location.href,
		},
	});
};

function navigateLogin(): void {
	router.push({
		name: AppRoutes.AuthLogin,
	});
}
function logout(): void {
	$auth.logoutRedirectingToStartPage(
		user.value.authClientName,
		'logoutReason=manual'
	);
}

const startPageRoute = EstateRoutes.Search;
/** Handle translations */
const languages = [
	{ title: 'Svenska', locale: 'sv' },
	{ title: 'English', locale: 'en' },
];
const selectedLocale = computed({
	get: () => {
		return locale.value;
	},
	set: (newLocale: string) => {
		setLocale(newLocale);
		locale.value = newLocale;
	},
});
const nextLanguage = computed(
	() =>
		languages.find((item) => item.locale !== selectedLocale.value) ??
		languages[0]
);
</script>

<style scoped lang="scss">
.app-header {
	background-color: $white;
	box-shadow: 0px 3px 5px -2px rgba(0, 0, 0, 0.1);
	flex-direction: column;
	align-items: center;
	display: flex;

	.header-content {
		padding: 14px $site-horizontal-padding;
		display: flex;
		width: 100%;
		max-width: $site-max-width;
		justify-content: space-between;

		.logo-wrap {
			display: flex;
			justify-content: center;
			align-items: center;
			.logo {
				height: 46px;
			}

			.separator {
				height: 50%;
				width: 1px;
				margin: 0 14px;
				background-color: $grey-lighten-5;
			}
			.title {
				display: flex;
				height: 100%;
				align-items: center;
				color: $grey-darken-2;
				font-size: size(20);
				font-weight: bold;
			}

			.environment-badge {
				display: inline-flex;
				align-items: center;
				justify-content: center;
				margin-left: 12px;
				padding: 5px 10px;
				border-radius: 999px;
				background: var(--environment-badge-background);
				color: var(--environment-badge-color);
				font-size: size(12);
				font-weight: 700;
				letter-spacing: 0.08em;
				line-height: 1;
				text-transform: uppercase;
				white-space: nowrap;
			}
		}

		.primary-nav {
			display: flex;
			align-items: center;
			gap: 4px;
			margin-left: 24px;
			margin-right: auto;

			a {
				position: relative;
				display: inline-flex;
				align-items: center;
				gap: 6px;
				padding: 6px 10px;
				color: $grey-darken-3;
				text-decoration: none;
				font-size: size(16);
				border-radius: $border-radius;
				white-space: nowrap;

				.v-icon {
					color: $grey-darken-2;
				}

				&:hover {
					background-color: rgba($primary, 0.06);
				}

				&.router-link-exact-active {
					color: $primary;
					font-weight: bold;

					.v-icon {
						color: $primary;
					}

					&::after {
						content: '';
						position: absolute;
						left: 10px;
						right: 10px;
						bottom: 0;
						height: 2px;
						background-color: $primary;
					}
				}
			}

			@media only screen and (max-width: $estate-mobile-threshold) {
				display: none;
			}
		}

		.menu-wrap {
			align-self: flex-end;
			display: flex;
			align-items: center;
			min-height: 52px;
			margin-left: 10px;

			.locale-btn {
				min-width: 0;
				padding: 0 8px;
				margin-right: 8px;

				.flag {
					width: 24px;
					height: auto;
					border-radius: 2px;
					box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.15);
				}
			}

			.user-wrap {
				display: flex;
				align-items: center;
				margin-right: 16px;
				font-weight: bold;
				color: $grey-darken-3;
				.v-icon {
					color: $grey-darken-2;
					margin-right: 8px;
				}
			}

			.v-list {
				min-width: 160px;
				hr {
					border: solid 1px $grey-lighten-3;
				}
			}

			button:last-of-type {
				margin-right: 0;
			}
		}

		.v-btn {
			--v-btn-height: 40px;
			margin-top: 0;
			margin-bottom: 0;
			text-transform: none;
			font-size: size(16);
			letter-spacing: normal;
			&:first-child {
				margin-left: 0;
			}
		}

		@media only screen and (max-width: 700px) {
			.logo-wrap .logo {
				height: 38px;
			}
			.menu-wrap .user-wrap {
				display: none;
			}
		}

		@media only screen and (max-width: 450px) {
			.logo-wrap .title {
				display: none;
			}
		}
	}

	// Coloured line across the top, so a non-production environment is
	// impossible to miss. The custom properties come from useEnvironmentBadge.
	&--environment {
		border-top: 6px solid var(--environment-accent);
	}

	&.Size-FullWidth {
		position: sticky;
		top: 0;
		z-index: 10;
		height: $site-header-height;

		.header-content {
			max-width: none;
			padding: 14px 24px;
		}
	}

	// Skip to content visible when tabbing, used for screen readers [WCAG 2.4, navigation]
	#skip {
		position: absolute;
		top: 0;
		left: -9999px;
		z-index: 100;
		width: 100%;
		margin: 0;

		a:focus,
		a:active {
			display: block;
			position: absolute;
			top: 0;
			left: 9999px;
			width: 100%;
			padding: 8px 0;
			background: $accent;
			color: $black;
		}
	}
}
</style>
