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
		<div class="d-flex align-end justify-space-between w-100 ga-4">
			<v-btn
				v-if="baseLayer === MapBaseLayer.Ortofoto"
				@click="baseLayer = MapBaseLayer.Lovisa"
				class="layer-btn"
				:style="{ backgroundImage: `url(${LovisaImg})` }"
			>
				<span class="layer-btn__label">
					{{ t('component.map.layer.regular') }}
				</span>
			</v-btn>
			<v-btn
				v-else
				@click="baseLayer = MapBaseLayer.Ortofoto"
				class="layer-btn"
				:style="{ backgroundImage: `url(${OrtofotoImg})` }"
			>
				<span class="layer-btn__label">
					{{ t('component.map.layer.photo') }}
				</span>
			</v-btn>

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
import LovisaImg from '@/assets/map/layer_lovisa.webp';
import OrtofotoImg from '@/assets/map/layer_ortofoto.webp';
import { MapBaseLayer } from '@/models/Enums';
import { appInsights } from '@/plugins/appInsights';

const props = defineProps<{
	fullScreen: boolean;
	zoomInDisabled?: boolean;
	zoomOutDisabled?: boolean;
	baseLayer?: MapBaseLayer;
}>();

const { t } = useI18n();

const emit = defineEmits([
	'update:fullScreen',
	'update:baseLayer',
	'zoom-in',
	'zoom-out',
]);

const fullScreen = computed({
	get: () => props.fullScreen,
	set: (value: boolean) => {
		emit('update:fullScreen', value);
	},
});

const baseLayer = computed({
	get: () => props.baseLayer,
	set: (value: MapBaseLayer) => {
		emit('update:baseLayer', value);
		appInsights?.trackEvent({
			name: 'EstateMapBaseLayerChanged',
			properties: {
				baseLayer: value,
			},
		});
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
	.layer-btn {
		text-transform: none;
		letter-spacing: normal;
		color: #fff;
		width: 75px;
		height: 75px;
		align-items: end;

		border: solid 2px #fff;

		background-position: center;
		background-size: cover;
		overflow: hidden;

		&::before {
			content: '';
			position: absolute;
			inset: 0;
			background: linear-gradient(
				to top,
				rgb(0 0 0 / 0.65) 0%,
				rgb(0 0 0 / 0.45) 30%,
				rgb(0 0 0 / 0.1) 60%,
				rgb(0 0 0 / 0) 100%
			);
			z-index: 0;
		}

		&__label {
			padding: 8px;
			z-index: 1;
		}
	}
}
</style>
