<template>
	<v-alert
		v-if="childHasNonFolkbokfordGuardian"
		class="mt-4"
		icon="info"
		rounded="lg"
		:title="t('component.external.common.nonFolkbokfordAlert.title')"
		:text="t('component.external.common.nonFolkbokfordAlert.text')"
	/>
</template>

<script lang="ts" setup>
import { IChild } from '@/models/Interfaces';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	children: IChild[];
}>();

const { t } = useI18n();

const childHasNonFolkbokfordGuardian = computed(() =>
	props.children.some((child) => child.guardianIsNotFolkbokford)
);
</script>
