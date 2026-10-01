<template>
	<v-btn-toggle
		v-if="variant === 'overlay'"
		class="building-map-toggle overlay"
		:model-value="activeMap"
		mandatory
		base-color="white"
		color="primary"
		variant="flat"
		rounded="lg"
		elevation="2"
		@update:model-value="select"
	>
		<v-tooltip
			v-for="option in options"
			:key="option.type"
			:text="option.tooltip"
			location="bottom"
			:disabled="!option.tooltip"
			:open-delay="200"
		>
			<template v-slot:activator="{ props }">
				<div v-bind="props" class="option">
					<v-btn
						:value="option.type"
						rounded="0"
						:prepend-icon="option.icon"
						:disabled="option.disabled"
					>
						{{ option.label }}
					</v-btn>
				</div>
			</template>
		</v-tooltip>
	</v-btn-toggle>

	<div v-else class="building-map-toggle inline">
		<base-icon-button
			v-for="option in options"
			:key="option.type"
			:icon="option.icon"
			:label="option.label"
			:disabled="option.disabled"
			:tooltip="option.tooltip"
			@click="emit('select', option.type)"
		/>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ActiveMapType } from '@/models/Enums';
import BaseIconButton from '@/components/shared/BaseIconButton.vue';

const props = defineProps<{
	activeMap: ActiveMapType;
	blueprintAvailable: boolean;
	// The inline variant is a cell row in the content column and only shows on
	// mobile, where the map pane is hidden and both views open fullscreen.
	variant: 'overlay' | 'inline';
}>();

const emit = defineEmits<{
	(e: 'select', value: ActiveMapType): void;
}>();

const { t } = useI18n();

const options = computed(() => [
	{
		type: ActiveMapType.Map,
		icon: 'map',
		label: t('component.buildingDetails.mapButton'),
		disabled: false,
		tooltip: '',
	},
	{
		type: ActiveMapType.Blueprint,
		icon: 'straighten',
		label: t('component.buildingDetails.blueprintButton'),
		disabled: !props.blueprintAvailable,
		tooltip: props.blueprintAvailable
			? ''
			: t('component.buildingDetails.blueprintMissingTooltip'),
	},
]);

const select = (value: ActiveMapType | null) => {
	if (value) emit('select', value);
};
</script>

<style scoped lang="scss">
.building-map-toggle {
	.option {
		display: flex;
	}

	&.overlay {
		position: absolute;
		top: 14px;
		left: 14px;
		max-width: calc(100% - 28px);
		z-index: 101;
		overflow: hidden;

		.v-btn {
			font-size: size(18);

			&:not(.v-btn--active) {
				color: $grey-darken-3;
			}

			&.v-btn--active :deep(.v-btn__overlay) {
				display: none;
			}
		}
	}

	&.inline {
		// The options are cells of the action row the parent lays out
		display: contents;
	}
}
</style>
