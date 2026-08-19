<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/app/AppHeader.vue -->
<template>
	<header class="app-header" :class="size">
		<div id="skip">
			<a href="#main-content" class="subheading text-center">{{
				$t('app.nav.skipToContent')
			}}</a>
		</div>
		<div class="locale-wrap">
			<div class="locale">
				<v-menu attach>
					<template v-slot:activator="{ props }">
						<v-btn
							prepend-icon="translate"
							variant="text"
							size="small"
							v-bind="props"
							>{{
								$t('component.appHeader.locale.change')
							}}</v-btn
						>
					</template>
					<v-list>
						<v-list-item
							v-for="(item, index) in languages"
							:key="index"
							:value="index"
							@click="selectedLocale = item.locale"
							href="#"
						>
							<v-list-item-title>{{
								item.title
							}}</v-list-item-title>
						</v-list-item>
					</v-list>
				</v-menu>
			</div>
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
			</div>

			<div
				class="menu-wrap"
				v-if="
					user.isAuthenticated ||
					(!user.isAuthenticated &&
						route.name !== AppRoutes.AuthLogin)
				"
			>
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
						<v-list-item :href="aboutPageUrl" target="_blank">
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
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import Config from '@/Config';
import { AppContentSize, AppHeaderTitle } from '@/models/Enums';
import logoGreen from '@/assets/logo_green.png';

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

const headerTitle = computed(() => {
	if (route.meta.title) {
		return t('component.appHeader.title.' + route.meta.title);
	}
	return t('component.appHeader.title.' + AppHeaderTitle.Default);
});

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
const aboutPageUrl = computed(() => {
	return Config.VUE_APP_ABOUT_URL;
});

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
		}

		.menu-wrap {
			align-self: flex-end;
			display: flex;
			align-items: center;
			min-height: 52px;
			margin-left: 10px;

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

	.locale-wrap {
		background-color: $grey-lighten-2;
		width: 100%;
		display: flex;
		justify-content: center;

		.locale {
			max-width: $site-max-width;
			width: 100%;
			padding: 0 calc($site-horizontal-padding - 6px);
			text-align: right;

			.v-btn {
				margin: 0px 0;
				padding: 6px 6px;
				height: auto;
				padding-left: 10px;
				text-transform: none;
				font-size: size(16);
				letter-spacing: normal;
				border-radius: 0;
			}
			.v-menu {
				text-align: left;
			}
		}
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

		.locale-wrap {
			display: none;
		}
		.locale {
			max-width: none;
			padding: 0 18px;
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
