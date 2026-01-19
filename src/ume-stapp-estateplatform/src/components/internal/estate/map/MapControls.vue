<template>
	<div class="map-controls w-100">
		<v-btn
			:icon="fullScreen ? 'close' : 'open_in_full'"
			@click="fullScreen = !fullScreen"
			:title="
				fullScreen
					? t('component.map.fullScreenClose')
					: t('component.map.fullScreenOpen')
			"
			size="small"
		/>
		<v-spacer></v-spacer>
		<div class="d-flex align-end justify-end w-100 ga-4">
			<v-btn-group direction="vertical" :elevation="2">
				<v-btn
					rounded="0"
					icon="add"
					:title="t('component.map.zoomIn')"
					@click="emit('zoom-in')"
					size="small"
					:disabled="zoomInDisabled"
				/>
				<v-btn
					rounded="0"
					icon="remove"
					:title="t('component.map.zoomOut')"
					@click="emit('zoom-out')"
					size="small"
					:disabled="zoomOutDisabled"
				/>
			</v-btn-group>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	fullScreen: boolean;
	zoomInDisabled?: boolean;
	zoomOutDisabled?: boolean;
}>();

const { t } = useI18n();

const emit = defineEmits(['update:fullScreen', 'zoom-in', 'zoom-out']);

const fullScreen = computed({
	get: () => props.fullScreen,
	set: (value: boolean) => {
		emit('update:fullScreen', value);
	},
});
</script>

<style lang="scss" scoped>
.map-controls {
	touch-action: manipulation;
	pointer-events: none;
	display: flex;
	flex-direction: column;
	align-items: end;
	position: absolute;
	right: 0;
	top: 0;
	bottom: 0;
	padding: 14px;
	gap: 10px;
	.v-btn {
		pointer-events: all;
		margin: 0;

		:deep(.v-icon) {
			color: $grey-darken-4;
		}
	}

	.v-btn-group {
		border-radius: $border-radius;
		.v-btn {
			border-bottom: solid 1px $grey-lighten-3;
			border-radius: 0;

			&:last-child {
				border-bottom: none;
			}
		}
	}
}
</style>
