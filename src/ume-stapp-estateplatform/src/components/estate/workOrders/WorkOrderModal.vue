<template>
	<v-dialog
		v-model="showModal"
		class="work-order-modal estate-default"
		:max-width="820"
		aria-labelledby="work-order-modal-title"
	>
		<v-card v-if="order">
			<div class="content">
				<div v-if="imageUrl" class="image-wrap">
					<building-image
						:src="imageUrl"
						:image-width="820"
						:alt="buildingLabel ?? undefined"
						class="image"
					/>
				</div>

				<div class="px-6 pt-5">
					<div class="header">
						<div class="type">
							<div class="type-icon" :class="type">
								<v-icon :icon="typeIcon" :size="24" />
							</div>
							<h2 id="work-order-modal-title">
								{{
									type
										? t(
												`component.estatePortal.actions.${type}.title`
										  )
										: t('component.workOrderModal.title')
								}}
							</h2>
						</div>
						<work-order-status :order="order" />
					</div>

					<section v-if="buildingLabel" class="where">
						<h3>{{ t('component.workOrderModal.where') }}</h3>
						<div class="building">
							<span>{{ buildingLabel }}</span>
							<router-link
								v-if="order.buildingId"
								:to="{
									name: EstateRoutes.BuildingDetails,
									params: { buildingId: order.buildingId },
								}"
								class="show-building"
							>
								{{ t('component.workOrderModal.showBuilding') }}
							</router-link>
						</div>
						<div v-if="order.roomName" class="room">
							{{
								t('component.workOrderModal.room', {
									room: order.roomName,
								})
							}}
						</div>
						<div v-if="locationLabel" class="location">
							{{ locationLabel }}
						</div>
					</section>

					<section class="description">
						<h3>{{ t('component.workOrderModal.description') }}</h3>
						<p>{{ order.description }}</p>
					</section>

					<section
						v-if="order.performedDescription"
						class="performed"
					>
						<h3>{{ t('component.workOrderModal.performed') }}</h3>
						<p>{{ order.performedDescription }}</p>
					</section>

					<p class="meta">{{ meta }}</p>
				</div>
			</div>
			<v-card-actions>
				<hr class="mb-4 mt-4" />
				<v-btn @click="showModal = false">{{
					t('app.nav.close')
				}}</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import BuildingImage from '@/components/estate/building/BuildingImage.vue';
import { EstateRoutes } from '@/router/routes';
import { getBuildingImageUrl } from '@/store/mapper';
import { formatDateTime } from '@/utils/formatRecentDate';
import type { WorkOrderListItem } from '@/utils/useWorkOrders';
import WorkOrderStatus from './WorkOrderStatus.vue';
import { workOrderTypeIcon, workOrderTypeKey } from './workOrderDisplay';

const props = defineProps<{
	modelValue: boolean;
	order: WorkOrderListItem | null;
}>();

const emit = defineEmits(['update:modelValue']);

const { t, locale } = useI18n();

const showModal = computed({
	get: () => props.modelValue,
	set: (show) => emit('update:modelValue', show),
});

const type = computed(() =>
	props.order ? workOrderTypeKey(props.order) : null
);
const typeIcon = computed(() =>
	props.order ? workOrderTypeIcon(props.order) : ''
);

const buildingLabel = computed(
	() => props.order?.buildingPopularName ?? props.order?.buildingName ?? null
);

// The API only flags that images exist; the URL is built like everywhere else.
const imageUrl = computed(() =>
	props.order?.buildingId && props.order.buildingImageUrl
		? getBuildingImageUrl(props.order.buildingId)
		: null
);

const locationLabel = computed(() => {
	switch (props.order?.location) {
		case 'Indoor':
			return t('component.workOrderModal.indoor');
		case 'Outdoor':
			return t('component.workOrderModal.outdoor');
		default:
			return null;
	}
});

const meta = computed(() => {
	if (!props.order) return '';
	const sentAt = new Date(props.order.submittedAt ?? props.order.createdAt);
	const changedAt = new Date(props.order.lastChangedAt);
	const sent = formatDateTime(sentAt, locale.value);
	const updated = formatDateTime(changedAt, locale.value);
	return [
		props.order.workOrderNumber &&
			t('component.workOrderModal.workOrderNumber', {
				number: props.order.workOrderNumber,
			}),
		t('component.workOrderModal.sentOn', { date: sent }),
		// An unchanged order dates from its creation, just before it was sent,
		// so only a later minute counts as an update.
		changedAt > sentAt &&
			updated !== sent &&
			t('component.workOrderModal.updatedOn', { date: updated }),
	]
		.filter(Boolean)
		.join(' · ');
});
</script>

<style scoped lang="scss">
.content {
	overflow-y: auto;
	max-height: calc(80vh - 100px);

	.image-wrap {
		aspect-ratio: 16 / 7;
		background: $grey-lighten-3;
		overflow: hidden;

		.image {
			display: block;
			width: 100%;
			height: 100%;
			object-fit: cover;
			border-radius: 0;
		}
	}

	.header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 12px;
		margin-bottom: 20px;

		.type {
			display: flex;
			align-items: center;
			gap: 12px;

			h2 {
				margin: 0;
				font-size: size(22);
			}
		}

		.type-icon {
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

			&.faultReport,
			&.order,
			&.spaceRequirement {
				.v-icon {
					color: $grey-darken-3;
				}
			}

			&.faultReport {
				background: $warning-bg;
			}

			&.order {
				background: $accent-bg;
			}

			&.spaceRequirement {
				background: $info-bg;
			}
		}
	}

	section {
		margin-bottom: 20px;

		h3 {
			margin-bottom: 4px;
			font-size: size(14);
			font-weight: 600;
			text-transform: uppercase;
			letter-spacing: 0.02em;
			opacity: 0.75;
		}

		.building {
			display: flex;
			flex-wrap: wrap;
			align-items: baseline;
			gap: 4px 12px;
			font-size: size(16);
		}

		.room,
		.location {
			font-size: size(16);
		}
	}

	.description p,
	.performed p {
		margin: 0;
		font-size: size(16);
		white-space: pre-line;
		overflow-wrap: anywhere;
	}

	.meta {
		font-size: size(14);
		color: $grey-darken-2;
	}
}
</style>
