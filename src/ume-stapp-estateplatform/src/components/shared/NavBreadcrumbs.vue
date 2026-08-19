<!-- Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/components/internal/shared/NavBreadcrumbs.vue @ 84b4a5dc -->
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

const props = defineProps<{
	breadcrumbs: {
		title: string;
		to: RouteLocationAsRelativeTyped;
	}[];
	fullWidth?: boolean;
}>();

const breadcrumbs = computed(() => props.breadcrumbs);
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
