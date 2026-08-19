<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/base/BaseBackButton.vue -->
<template>
	<v-btn
		prepend-icon="arrow_back"
		variant="plain"
		:ripple="false"
		class="back-btn mb-0 ml-0 pl-1"
		:to="to"
		@click="!to ? goBack() : undefined"
	>
		{{ $t('app.nav.goBack') }}
	</v-btn>
</template>

<script setup lang="ts">
import { PropType } from 'vue';
import { RouteLocationRaw, useRouter } from 'vue-router';

defineProps({
	to: {
		type: Object as PropType<RouteLocationRaw>,
	},
});

const router = useRouter();

const goBack = () => {
	router.back();
};
</script>
