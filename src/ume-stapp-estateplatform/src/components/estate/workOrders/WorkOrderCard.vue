<template>
	<v-card
		class="errand-card"
		role="button"
		tabindex="0"
		@click="emit('select')"
		@keydown.enter.prevent="emit('select')"
		@keydown.space.prevent="emit('select')"
	>
		<div class="errand-icon" :class="type">
			<v-icon :icon="typeIcon" :size="24" />
		</div>

		<div class="errand-body">
			<div class="errand-title">{{ order.description }}</div>
			<div class="errand-meta">
				{{
					[
						type &&
							t(`component.estatePortal.actions.${type}.title`),
						order.buildingPopularName ?? order.buildingName,
					]
						.filter(Boolean)
						.join(' · ')
				}}
			</div>
		</div>

		<div class="errand-status">
			<work-order-status :order="order" />
			<time
				class="errand-date"
				:datetime="order.lastChangedAt"
				:title="fullDate"
			>
				{{ date }}
			</time>
		</div>
	</v-card>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { formatFullDate, formatRecentDate } from '@/utils/formatRecentDate';
import type { WorkOrderListItem } from '@/utils/useWorkOrders';
import WorkOrderStatus from './WorkOrderStatus.vue';
import { workOrderTypeIcon, workOrderTypeKey } from './workOrderDisplay';

const props = defineProps<{
	order: WorkOrderListItem;
}>();

const emit = defineEmits<{ select: [] }>();

const { t, locale } = useI18n();

const type = computed(() => workOrderTypeKey(props.order));
const typeIcon = computed(() => workOrderTypeIcon(props.order));

// The date the list is sorted by; the modal shows when it was sent.
const lastChanged = computed(() => new Date(props.order.lastChangedAt));
const date = computed(() => formatRecentDate(lastChanged.value, locale.value));
const fullDate = computed(() =>
	formatFullDate(lastChanged.value, locale.value)
);
</script>

<style lang="scss" scoped>
.errand-card {
	display: flex;
	align-items: center;
	gap: 16px;
	padding: 16px 20px;

	.errand-icon {
		flex-shrink: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 44px;
		height: 44px;
		border-radius: 12px;
		background: $primary-bg;

		.v-icon {
			color: $primary;
		}

		&.faultReport {
			background: $warning-bg;

			.v-icon {
				color: $grey-darken-3;
			}
		}

		&.order {
			background: $accent-bg;

			.v-icon {
				color: $grey-darken-3;
			}
		}

		&.spaceRequirement {
			background: $info-bg;

			.v-icon {
				color: $grey-darken-3;
			}
		}
	}

	.errand-body {
		flex: 1;
		min-width: 0;

		.errand-title {
			font-size: size(18);
			font-weight: 600;
			color: $black;
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		.errand-meta {
			margin-top: 2px;
			font-size: size(16);
			color: $grey-darken-2;
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}
	}

	.errand-status {
		flex-shrink: 0;
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: 4px;

		.errand-date {
			font-size: size(14);
			color: $grey-darken-2;
		}
	}
}

@media only screen and (max-width: $estate-mobile-threshold) {
	.errand-card {
		align-items: flex-start;
		flex-wrap: wrap;

		.errand-status {
			flex-direction: row;
			align-items: center;
			gap: 8px;
			width: 100%;
			padding-left: 60px;
		}
	}
}
</style>
