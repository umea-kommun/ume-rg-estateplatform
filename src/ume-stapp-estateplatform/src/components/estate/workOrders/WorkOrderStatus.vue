<template>
	<span class="errand-status-label" :class="statusKey">
		<span class="status-dot" aria-hidden="true"></span>
		{{ t(`component.estateOngoingErrands.status.${statusKey}`) }}
	</span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import type { WorkOrderListItem } from '@/utils/useWorkOrders';
import { workOrderStatusKey } from './workOrderDisplay';

const props = defineProps<{
	order: WorkOrderListItem;
}>();

const { t } = useI18n();

const statusKey = computed(() => workOrderStatusKey(props.order));
</script>

<style lang="scss" scoped>
.errand-status-label {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: size(14);
	color: $grey-darken-4;
	white-space: nowrap;

	.status-dot {
		width: 8px;
		height: 8px;
		border-radius: 50%;
	}

	&.sent,
	&.received {
		background: rgba($info, 0.16);

		.status-dot {
			background: $info;
		}
	}

	&.inProgress,
	&.materialOrdered,
	&.sentToContractor {
		background: rgba($warning, 0.24);

		.status-dot {
			background: $warning;
		}
	}

	&.onHold {
		background: $grey-lighten-3;

		.status-dot {
			background: $grey-darken-1;
		}
	}

	&.closed {
		background: $success-bg;

		.status-dot {
			background: $success;
		}
	}
}
</style>
