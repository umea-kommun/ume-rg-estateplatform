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

			<turkos-release-notes class="release-list" />
		</div>
	</app-content>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { TurkosReleaseNotes } from '@turkos/controlcenter-client';
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

	.release-list {
		:deep(.turkos-release-notes__title) {
			font-size: size(28);
			line-height: 1.3;
		}

		:deep(.turkos-release-notes__heading) {
			font-size: size(24);
			margin-top: 16px;
		}

		:deep(.turkos-release-notes__intro) {
			color: $grey-darken-1;
			font-style: italic;
		}

		:deep(p),
		:deep(li) {
			font-size: size(18);
			line-height: 1.6;
		}

		:deep(.turkos-release-notes__items) {
			padding-left: 24px;
		}

		:deep(li + li) {
			margin-top: 6px;
		}

		:deep(li::marker) {
			color: $grey-darken-1;
		}
	}
}
</style>
