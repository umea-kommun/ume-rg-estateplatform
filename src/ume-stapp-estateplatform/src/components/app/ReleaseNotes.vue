<template>
	<app-content
		class="release-notes estate-default"
		:size="AppContentSize.Narrow"
		:pageTitle="t('component.releaseNotes.pageTitle')"
	>
		<template #breadcrumbs>
			<nav-breadcrumbs class="mt-4 mb-2" :breadcrumbs="breadcrumbs" />
		</template>

		<div class="content py-4">
			<h1>{{ t('component.releaseNotes.title') }}</h1>
			<p class="lead mt-4">
				{{ t('component.releaseNotes.intro') }}
			</p>

			<v-divider class="my-8" />

			<section class="release">
				<h2>Release 2026-10-01</h2>
				<p class="subtitle">
					I denna release introducerar vi en ny startsida, mina
					ärenden, nya funktioner och mindre förbättringar.
				</p>

				<h3>Vad är nytt</h3>
				<ul>
					<li>
						Ny startsida med snabblänkar till bl.a. Felanmälan samt
						snabbsök.
					</li>
					<li>
						Ny funktion Mina ärenden, där du ser dina inskickade
						ärenden och deras aktuella status.
					</li>
					<li>
						Nytt sökfilter för fastigheter och byggnader, där
						byggnader visas som standard.
					</li>
					<li>
						Ny design av korten för byggnader/fastigheter. Rum,
						kontaktpersoner och handlingar visas nu under egna
						flikar.
					</li>
					<li>
						Ny våningsväljare. Våning 1 visas först om byggnaden har
						en sådan.
					</li>
					<li>
						Byggnader som saknar rumsinformation visar inte längre
						en tom rumslista.
					</li>
					<li>
						Förbättring av felanmälan/beställning-flödet. Alla steg
						visas på en gång så att det ska bli enklare och
						tydligare.
					</li>
					<li>
						Förbättrad tillgänglighetsanpassning för användare av
						text-till-tal.
					</li>
					<li>Mindre buggfixar och förbättringar av stabiliteten.</li>
				</ul>
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
			title: t('component.appHeader.menu.releaseNotes'),
			to: { name: EstateRoutes.ReleaseNotes },
		},
	];
});
</script>

<style scoped lang="scss">
.release-notes {
	.content {
		max-width: 760px;
		margin: 0 auto;
	}

	h1 {
		font-size: size(36);
		line-height: 1.2;
	}

	p,
	li {
		font-size: size(18);
		line-height: 1.6;
	}

	.lead {
		font-size: size(20);
	}

	.release {
		h2 {
			font-size: size(28);
			line-height: 1.3;
		}

		h3 {
			font-size: size(24);
		}

		.subtitle {
			color: $grey-darken-1;
			font-size: size(18);
			font-style: italic;
			margin-bottom: 40px;
		}

		ul {
			padding-left: 24px;
		}

		li + li {
			margin-top: 6px;
		}

		li::marker {
			color: $grey-darken-1;
		}
	}
}
</style>
