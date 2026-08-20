<template>
	<div
		class="base-expandable-content"
		:class="{ expanded: contentExpanded || contentFits }"
		:style="{
			maxHeight: contentExpanded ? 'none' : props.contentThreshold + 'px',
		}"
	>
		<div class="content" ref="content">
			<slot></slot>
		</div>
	</div>
	<div class="expand-button-wrapper" v-if="!contentFits">
		<v-btn
			:append-icon="
				contentExpanded ? 'keyboard_arrow_up' : 'keyboard_arrow_down'
			"
			variant="flat"
			@click="contentExpanded = !contentExpanded"
		>
			{{
				contentExpanded
					? t('component.baseExpandableContent.showMoreButtonClose')
					: t('component.baseExpandableContent.showMoreButtonOpen')
			}}
		</v-btn>
	</div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { computed, onMounted, ref } from 'vue';

interface Props {
	contentThreshold?: number;
}
const props = withDefaults(defineProps<Props>(), {
	contentThreshold: 100,
});

const { t } = useI18n();

const content = ref();
const height = ref<number>(0);

const contentExpanded = ref<boolean>(false);
const contentFits = computed(() => {
	return height.value <= props.contentThreshold;
});

onMounted(() => {
	height.value = content.value.getBoundingClientRect().height;
});
</script>

<style scoped lang="scss">
.base-expandable-content {
	overflow: hidden;
	position: relative;

	&::before {
		content: ' ';
		position: absolute;
		left: 0;
		right: 0;
		bottom: 0;
		max-height: 100px;
		height: 90%;
		background: linear-gradient(
			180deg,
			rgba(255, 255, 255, 0) 0%,
			rgba(255, 255, 255, 0.47) 40%,
			rgba(255, 255, 255, 1) 100%
		);
		pointer-events: none;
	}

	&.expanded {
		overflow: auto;

		&::before {
			display: none;
		}
	}
}
.expand-button-wrapper {
	text-align: center;
}
</style>
