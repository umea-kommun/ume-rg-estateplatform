<template>
	<div class="order-category-selector pt-4">
		<v-card
			v-for="cate in categories"
			:key="cate.type"
			class="category-card pa-4"
			:class="{
				'category-card--selected': props.category === cate.type,
			}"
			rounded="lg"
			@click="emit('select', cate.type)"
			@keydown.enter="emit('select', cate.type)"
			tabindex="0"
		>
			<div class="d-flex justify-space-between align-start mb-4">
				<div class="icon-wrap">
					<v-icon :icon="cate.icon" :size="28" />
				</div>

				<v-scale-transition>
					<v-icon
						v-if="props.category === cate.type"
						color="success"
						icon="check_circle"
						size="24"
					/>
				</v-scale-transition>
			</div>

			<div class="text-h6 font-weight-bold mb-2">
				{{ cate.title }}
			</div>

			<div class="text-body-2 text-medium-emphasis mb-4">
				{{ cate.description }}
			</div>

			<div
				class="d-flex align-center justify-space-between mt-auto select-wrap"
			>
				<v-chip
					size="small"
					variant="tonal"
					color="success"
					v-if="props.category === cate.type"
				>
					{{ $t('component.internal.order.category.selected') }}
				</v-chip>

				<span
					v-else
					class="text-body-2 text-primary font-weight-medium"
				>
					{{ $t('component.internal.order.category.select') }}
				</span>
			</div>
		</v-card>
	</div>
</template>

<script setup lang="ts">
import { EstateOrderCategory } from '@/models/Enums';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	category: EstateOrderCategory | null;
	availableCategories: EstateOrderCategory[];
}>();

const emit = defineEmits(['select']);

const { t } = useI18n();

const allCategories = [
	{
		type: EstateOrderCategory.BuildingService,
		icon: 'handyman',
		title: t('component.internal.order.category.buildingServices.title'),
		description: t(
			'component.internal.order.category.buildingServices.description'
		),
	},
	{
		type: EstateOrderCategory.TownHallService,
		icon: 'account_balance',
		title: t('component.internal.order.category.townHallServices.title'),
		description: t(
			'component.internal.order.category.townHallServices.description'
		),
	},
	{
		type: EstateOrderCategory.FacilityService,
		icon: 'engineering',
		title: t('component.internal.order.category.facilitiesManager.title'),
		description: t(
			'component.internal.order.category.facilitiesManager.description'
		),
	},
	// SpaceRequirement (Förändrade lokalbehov) is now its own top-level flow
	// (EstateSpaceRequirement.vue); it is intentionally no longer an order category.
];

const categories = computed(() => {
	return allCategories.filter((category) =>
		props.availableCategories.includes(category.type)
	);
});
</script>

<style lang="scss" scoped>
.order-category-selector {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
	gap: 1rem;

	.category-card {
		min-height: 220px;
		display: flex;
		flex-direction: column;
		border: 1px solid rgba(0, 0, 0, 0.08);

		&--selected {
			outline: 2px solid rgb(46, 125, 50);
			background: rgba(46, 125, 50, 0.05);

			background-color: rgba($primary, 0.1);
			color: $primary;
		}

		.icon-wrap {
			width: 52px;
			height: 52px;
			border-radius: 50%;
			display: grid;
			place-items: center;
			background: rgba(46, 125, 50, 0.08);
			color: rgb(46, 125, 50);
		}
		.select-wrap {
			height: 1rem;
		}
	}
}
</style>
