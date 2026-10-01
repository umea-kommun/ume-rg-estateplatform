<template>
	<!-- Collapsed: a compact card of the chosen order type, matching the building,
	location and room steps. -->
	<selection-card
		v-if="selectedCategory"
		class="mt-2"
		:icon="selectedCategory.icon"
		:title="selectedCategory.title"
		:description="selectedCategory.description"
	>
		<template #actions>
			<v-btn
				rounded="lg"
				variant="outlined"
				color="grey-darken-2"
				@click="emit('select', null)"
			>
				{{ $t('component.faultReport.changeAnswer') }}
			</v-btn>
		</template>
	</selection-card>

	<!-- Selecting: the shared tile grid, same as the fault indoor/outdoor selector. -->
	<option-card-grid
		v-else
		class="pt-4"
		:options="options"
		:selected="category"
		@select="emit('select', $event as EstateOrderCategory)"
	/>
</template>

<script setup lang="ts">
import { EstateOrderCategory } from '@/models/Enums';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import OptionCardGrid from './OptionCardGrid.vue';
import type { OptionCard } from './OptionCardGrid.vue';
import SelectionCard from './SelectionCard.vue';

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
		title: t('component.order.category.buildingServices.title'),
		description: t('component.order.category.buildingServices.description'),
	},
	{
		type: EstateOrderCategory.TownHallService,
		icon: 'account_balance',
		title: t('component.order.category.townHallServices.title'),
		description: t('component.order.category.townHallServices.description'),
	},
	{
		type: EstateOrderCategory.FacilityService,
		icon: 'engineering',
		title: t('component.order.category.facilitiesManager.title'),
		description: t(
			'component.order.category.facilitiesManager.description'
		),
	},
	// SpaceRequirement (Förändrade lokalbehov) is now its own top-level flow
	// (EstateSpaceRequirement.vue); it is intentionally no longer an order category.
];

// Only the categories the API marks available for this building, mapped to the
// shared tile shape.
const options = computed<OptionCard[]>(() =>
	allCategories
		.filter((category) => props.availableCategories.includes(category.type))
		.map((category) => ({
			value: category.type,
			icon: category.icon,
			title: category.title,
			description: category.description,
		}))
);

// The selected category's tile data, used to render the collapsed summary card.
const selectedCategory = computed(() =>
	allCategories.find((category) => category.type === props.category)
);
</script>
