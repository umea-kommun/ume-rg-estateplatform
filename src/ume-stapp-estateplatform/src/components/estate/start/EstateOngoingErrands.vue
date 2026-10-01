<template>
	<section class="ongoing-errands">
		<div class="errands-header">
			<h2 class="mb-4">
				{{
					loading || loadFailed
						? $t('component.estateOngoingErrands.titleWithoutCount')
						: $t('component.estateOngoingErrands.title', {
								count: listed.length,
						  })
				}}
			</h2>
			<v-btn
				v-if="closed.length"
				class="toggle-closed"
				variant="text"
				@click="toggleClosed"
			>
				{{
					showClosed
						? $t('component.estateOngoingErrands.hideClosed')
						: $t('component.estateOngoingErrands.showClosed')
				}}
			</v-btn>
		</div>

		<p v-if="loadFailed" class="text-medium-emphasis mt-0">
			{{ $t('component.estateOngoingErrands.loadFailed') }}
		</p>
		<div v-else-if="loading" class="errand-list">
			<v-skeleton-loader
				v-for="n in SkeletonCount"
				:key="n"
				class="errand-skeleton"
				type="list-item-avatar-two-line"
				elevation="1"
			/>
		</div>
		<p v-else-if="!listed.length" class="text-medium-emphasis mt-0">
			{{ $t('component.estateOngoingErrands.noErrands') }}
		</p>
		<template v-else>
			<div class="errands">
				<div class="errand-list">
					<work-order-card
						v-for="order in shown"
						:key="order.id"
						:order="order"
						@select="openOrder(order)"
					/>
				</div>
				<!-- Same pattern as RoomList: the whole band is the button's hit area. -->
				<div
					v-if="!expanded && hasMore"
					class="show-more-wrap"
					@click="expanded = true"
				>
					<v-btn
						class="toggle-expanded more-btn"
						prepend-icon="expand_more"
						@click.stop="expanded = true"
					>
						{{ $t('component.estateOngoingErrands.showAll') }}
					</v-btn>
				</div>
			</div>
			<div v-if="expanded && hasMore" class="show-fewer-wrap">
				<v-btn
					class="toggle-expanded"
					prepend-icon="expand_less"
					@click="expanded = false"
				>
					{{ $t('component.estateOngoingErrands.showFewer') }}
				</v-btn>
			</div>
		</template>

		<work-order-modal v-model="modalOpen" :order="selectedOrder" />
	</section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import WorkOrderCard from '@/components/estate/workOrders/WorkOrderCard.vue';
import WorkOrderModal from '@/components/estate/workOrders/WorkOrderModal.vue';
import {
	isClosedWorkOrder,
	workOrderStatusKey,
} from '@/components/estate/workOrders/workOrderDisplay';
import { appInsights } from '@/plugins/appInsights';
import { useWorkOrders, type WorkOrderListItem } from '@/utils/useWorkOrders';

const MaxShown = 5;
const SkeletonCount = 2;

const { orders, loading, loadFailed } = useWorkOrders();
const expanded = ref(false);
const showClosed = ref(false);
const modalOpen = ref(false);
const selectedId = ref<string | null>(null);

// Held by id: refresh() replaces the objects in `orders`.
const selectedOrder = computed(
	() => orders.value.find((order) => order.id === selectedId.value) ?? null
);
watch(selectedOrder, (order) => {
	if (!order) modalOpen.value = false;
});
watch(modalOpen, (open) => {
	if (!open) selectedId.value = null;
});

// Latest change first, as returned by the API. Dismissed orders count as closed.
const ongoing = computed(() =>
	orders.value.filter((order) => !isClosedWorkOrder(order))
);
const closed = computed(() => orders.value.filter(isClosedWorkOrder));
// Closed errands are mixed in by the same order, so a just-closed one comes first.
const listed = computed(() =>
	showClosed.value ? orders.value : ongoing.value
);
const collapsed = computed(() => listed.value.slice(0, MaxShown));
const shown = computed(() => (expanded.value ? listed.value : collapsed.value));
const hasMore = computed(() => listed.value.length > collapsed.value.length);

function toggleClosed() {
	showClosed.value = !showClosed.value;
	appInsights?.trackEvent({
		name: 'WorkOrdersShowClosedToggled',
		properties: { value: showClosed.value },
	});
}

function openOrder(order: WorkOrderListItem) {
	selectedId.value = order.id;
	modalOpen.value = true;
	appInsights?.trackEvent({
		name: 'WorkOrderModalOpened',
		properties: {
			type: order.workOrderType,
			status: workOrderStatusKey(order),
		},
	});
}
</script>

<style lang="scss" scoped>
.ongoing-errands {
	margin-top: 56px;

	.errands-header {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 16px;
	}

	.errands {
		position: relative;
	}

	.errand-list {
		display: flex;
		flex-direction: column;
		gap: 12px;
	}

	// Mirrors RoomList's band, so the two "show more" buttons look alike.
	.show-more-wrap {
		position: absolute;
		left: 0;
		right: 0;
		margin-left: -10px;
		margin-right: -10px;
		margin-bottom: -5px;
		bottom: 0;
		z-index: 1;
		height: 100px;
		background: linear-gradient(
			to bottom,
			rgba(#fff, 0) 0%,
			rgba(#fff, 1) 80%
		);
		display: flex;
		justify-content: center;
		align-items: center;
		cursor: pointer;

		&:hover .more-btn :deep(.v-btn__overlay) {
			opacity: calc(
				var(--v-hover-opacity) * var(--v-theme-overlay-multiplier)
			);
		}
		&:active .more-btn :deep(.v-btn__overlay) {
			opacity: calc(
				var(--v-pressed-opacity) * var(--v-theme-overlay-multiplier)
			);
		}
	}

	.show-fewer-wrap {
		display: flex;
		justify-content: center;
		padding-top: 8px;
	}

	// Sized like WorkOrderCard so the list does not jump when the errands arrive.
	.errand-skeleton {
		min-height: 85px;
		border-radius: $border-radius;
		overflow: hidden;

		:deep(.v-skeleton-loader__avatar) {
			width: 44px;
			min-width: 44px;
			height: 44px;
			min-height: 44px;
			border-radius: 12px;
		}
	}
}
</style>
