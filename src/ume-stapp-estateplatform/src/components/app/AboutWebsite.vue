<template>
	<app-content
		class="about-website estate-default"
		:size="AppContentSize.Narrow"
		:pageTitle="t('component.aboutWebsite.pageTitle')"
	>
		<template #breadcrumbs>
			<nav-breadcrumbs class="mt-4 mb-2" :breadcrumbs="breadcrumbs" />
		</template>

		<div class="content py-4">
			<h1>{{ t('component.aboutWebsite.title') }}</h1>
			<p class="lead mt-4">
				{{ t('component.aboutWebsite.intro') }}
			</p>

			<section class="mt-8">
				<h2>{{ t('component.aboutWebsite.usedForTitle') }}</h2>
				<p>{{ t('component.aboutWebsite.usedForText') }}</p>
			</section>

			<section class="mt-8">
				<h2>{{ t('component.aboutWebsite.supplierTitle') }}</h2>
				<p>{{ t('component.aboutWebsite.supplierText') }}</p>
			</section>
			
			<section class="mt-8">
				<a href="https://www.intranet.umea.se/sidor/serviceitochlokaler/itsystemitstod/systemao/felanmalningarbestallningarochfastighetsinformationfastighetsportalen.4.463658d2177d4c931e02e51.html"
				   target="_blank"
				   rel="noopener noreferrer">
					{{ t('component.aboutWebsite.intranetLink') }}
				</a>
			</section>
		</div>
	</app-content>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import AppContent from '@/components/app/AppContent.vue';
import NavBreadcrumbs from '@/components/shared/NavBreadcrumbs.vue';
import { AppContentSize } from '@/models/Enums';
import { EstateRoutes } from '@/router/routes';

const { t, locale } = useI18n();

const breadcrumbs = computed(() => {
	if (!locale.value) return [];
	return [
		{
			title: t('component.appHeader.menu.about'),
			to: { name: EstateRoutes.AboutWebsite },
		},
	];
});
</script>

<style scoped lang="scss">
.about-website {
	.content {
		max-width: 760px;
		margin: 0 auto;
	}

	h1 {
		font-size: size(36);
		line-height: 1.2;
	}

	h2 {
		font-size: size(24);
		margin-bottom: 0.5rem;
	}

	p {
		font-size: size(18);
		line-height: 1.6;
	}

	.lead {
		font-size: size(20);
	}
	
	a {
		font-size: size(18);
	}
}
</style>
