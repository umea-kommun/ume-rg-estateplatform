<template>
	<footer
		class="app-footer"
		:style="{ backgroundImage: 'url(' + footerBackground + ')' }"
	>
		<v-container class="top-container">
			<v-layout justify-center class="content-layout">
				<v-col class="pa-0">
					<div class="footer-links">
						<h2>{{ $t('component.appFooter.linksTitle') }}</h2>
						<a href="/">
							{{ $t('component.appHeader.menu.appStart') }}
						</a>
						<a :href="umeaSeURL" target="blank">
							{{ $t('component.appHeader.menu.umeaSe') }}
						</a>
						<a :href="errorReportUrl" target="blank">
							{{ $t('component.appHeader.menu.errorReport') }}
						</a>
						<a
							v-if="availabilityStatementUrl"
							:href="availabilityStatementUrl"
							target="blank"
						>
							{{
								$t(
									'component.appHeader.menu.availabilityStatement'
								)
							}}
						</a>
						<a :href="aboutPageUrl" target="blank">
							{{ $t('component.appHeader.menu.about') }}
						</a>
					</div>
				</v-col>
			</v-layout>
		</v-container>
	</footer>
</template>

<script setup lang="ts">
import Config from '@/utils/Config';
import footerBackground from '@/assets/footer.svg';

// Navigation
const umeaSeURL = Config.VUE_APP_UMEA_SE_URL;
const errorReportUrl = Config.VUE_APP_ERROR_REPORT_URL;
const availabilityStatementUrl =
	Config.VUE_APP_AVAILABILITY_STATEMENT_URL ?? '';
const aboutPageUrl = Config.VUE_APP_ABOUT_URL;
</script>

<style scoped lang="scss">
.app-footer {
	z-index: 1;
	background: $primary;
	background-position: 50%;
	background-repeat: no-repeat;
	background-size: cover;

	.top-container {
		max-width: $site-max-width;
		padding: 50px $site-horizontal-padding;
		display: flex;
		min-height: 110px;
		align-items: center;
	}
	.content-layout {
		justify-content: center;

		.v-col {
			display: flex;
			justify-content: space-between;
			align-items: flex-end;
		}
	}

	.footer-links {
		max-width: 500px;
		h2 {
			color: #fff;
			font-size: size(24);
			margin-bottom: 20px;
		}

		a {
			display: inline-block;
			background-color: $success;
			font-size: size(18);
			border-radius: 31px;
			margin-right: 5px;
			margin-bottom: 11px;
			padding: 10px 14px;
			color: #fff !important;
			text-decoration: none;
			transition: opacity 0.2s ease;

			&:hover {
				opacity: 0.7;
				text-decoration: underline;
			}
		}
	}

	@media screen and (max-width: 600px) {
		.v-col {
			flex-wrap: wrap;
		}
		.logo-text {
			margin: 0;
			margin-top: 20px;
			text-align: left;
		}
		.footer-links {
			flex: 1;
			max-width: initial;
		}
	}
}
</style>
