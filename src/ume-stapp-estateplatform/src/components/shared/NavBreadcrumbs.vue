<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/internal/shared/NavBreadcrumbs.vue -->
<template>
	<div class="nav-breadcrumbs" :class="{ 'full-width': fullWidth }">
		<v-breadcrumbs class="ma-0 pa-0" divider="/" :items="breadcrumbs">
			<template v-slot:prepend>
				<v-icon
					icon="home"
					:size="20"
					color="grey-darken-3"
					class="mr-1"
				></v-icon>
			</template>
		</v-breadcrumbs>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { RouteLocationAsRelativeTyped } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { EstateRoutes } from '@/router/routes';

const props = defineProps<{
	breadcrumbs: {
		title: string;
		to: RouteLocationAsRelativeTyped;
	}[];
	fullWidth?: boolean;
}>();

const { t } = useI18n();

// Every trail starts at the start page, e.g. [home] Hem / Felanmälan. The
// house icon in the prepend slot sits just before this first crumb.
const breadcrumbs = computed(() => [
	{
		title: t('app.nav.home'),
		to: { name: EstateRoutes.Home },
	},
	...props.breadcrumbs,
]);
</script>

<style scoped lang="scss">
.nav-breadcrumbs {
	.v-breadcrumbs {
		flex-wrap: wrap;
		:deep(.v-breadcrumbs-item a) {
			max-width: 130px;
			overflow: hidden;
			white-space: nowrap;
			text-overflow: ellipsis;
		}
		:first-child {
			padding-left: 0;
		}
	}

	&.full-width {
		.v-breadcrumbs {
			:deep(.v-breadcrumbs-item a) {
				max-width: none;
			}
		}
	}
}
</style>
