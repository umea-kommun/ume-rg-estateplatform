<template>
	<div>
		<div
			class="buildings-header d-flex align-center justify-space-between ga-2 px-6"
		>
			<h2>{{ t('component.estateDetails.buildings') }}</h2>
			<v-menu v-if="buildings && buildings.length > 1">
				<template v-slot:activator="{ props: activatorProps }">
					<v-btn
						v-bind="activatorProps"
						class="sort-button"
						icon="sort"
						variant="text"
						density="comfortable"
						:title="t('component.estateDetails.sort.label')"
						:aria-label="t('component.estateDetails.sort.label')"
					/>
				</template>
				<v-list :selected="[sortBy]" density="compact">
					<v-list-item
						v-for="option in sortOptions"
						:key="option.value"
						:value="option.value"
						:title="option.title"
						@click="sortBy = option.value"
					/>
				</v-list>
			</v-menu>
		</div>
		<app-loading-spinner v-if="loading" :is-visible="true" />
		<v-alert
			v-else-if="failed"
			class="mt-2 mx-6"
			rounded="lg"
			icon="warning"
		>
			{{ t('app.error.estate.unableToFetchEstateBuildings') }}
		</v-alert>
		<v-alert
			v-else-if="buildings?.length === 0"
			icon="info"
			class="mt-2 mx-6"
		>
			{{ t('component.estateDetails.noBuildings') }}
		</v-alert>
		<div v-else-if="buildings" class="mt-2">
			<v-card
				v-for="building in sortedBuildings"
				:key="building.id"
				class="building-item pt-2 px-6"
				:to="{
					name: EstateRoutes.BuildingDetails,
					params: { buildingId: building.id },
				}"
				@mouseenter="() => emit('building-mouseenter', building.id)"
				@mouseleave="() => emit('building-mouseleave')"
			>
				<v-card-title
					class="px-0 py-0 d-flex align-start ga-1 justify-space-between"
				>
					<div class="building-name">
						{{ building.popularName || building.name }}
					</div>
					<favorite-button
						:id="building.id"
						:type="EstateType.Building"
						:isFavorite="building.isFavorite"
					/>
				</v-card-title>
				<v-card-text class="pt-2 pb-3 px-0">
					<ul class="pa-0 ma-0">
						<li
							v-if="building.address?.street"
							class="text-capitalize"
						>
							{{ building.address?.street.toLocaleLowerCase() }}
						</li>
						<li
							v-if="
								building.hasRoomInformation !== false &&
								building.metrics?.floorCount
							"
						>
							{{
								$t('estateCommon.floorCount', {
									count: building.metrics?.floorCount,
								})
							}}
						</li>
						<li
							v-if="
								building.hasRoomInformation !== false &&
								building.metrics?.roomCount
							"
						>
							{{
								$t('estateCommon.roomCount', {
									count: building.metrics?.roomCount,
								})
							}}
						</li>
						<li v-if="building.grossArea">
							{{ building.grossArea?.toLocaleString() }} m²
						</li>
					</ul>
				</v-card-text>
				<hr class="mt-0" />
			</v-card>
		</div>
	</div>
</template>

<script setup lang="ts">
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { IEstateBuilding } from '@/models/Interfaces';
import { EstateRoutes } from '@/router/routes';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import FavoriteButton from '../favorite/FavoriteButton.vue';
import { BuildingSortOption, EstateType } from '@/models/Enums';
import { useStorage } from '@vueuse/core';

const props = defineProps<{
	loading: boolean;
	failed: boolean;
	buildings: IEstateBuilding[] | null;
}>();

const emit = defineEmits(['building-mouseenter', 'building-mouseleave']);

const { t } = useI18n();

const sortOptions = computed(() =>
	[
		BuildingSortOption.AreaDesc,
		BuildingSortOption.AreaAsc,
		BuildingSortOption.Name,
		BuildingSortOption.Address,
	].map((value) => ({
		value,
		title: t(`component.estateDetails.sort.${value}`),
	}))
);

const storedSortBy = useStorage<BuildingSortOption>(
	'estate-buildings-sort',
	BuildingSortOption.AreaDesc
);

const sortBy = computed({
	get: () =>
		sortOptions.value.some((option) => option.value === storedSortBy.value)
			? storedSortBy.value
			: BuildingSortOption.AreaDesc,
	set: (value: BuildingSortOption) => (storedSortBy.value = value),
});

const compareByName = (a: IEstateBuilding, b: IEstateBuilding) =>
	(a.popularName || a.name).localeCompare(b.popularName || b.name);

const compareByAddress = (a: IEstateBuilding, b: IEstateBuilding) => {
	const streetA = a.address?.street ?? '';
	const streetB = b.address?.street ?? '';
	if (!streetA || !streetB) return streetA ? -1 : streetB ? 1 : 0;
	return streetA.localeCompare(streetB) || compareByName(a, b);
};

const sortedBuildings = computed(() => {
	if (!props.buildings) return null;
	const buildings = [...props.buildings];

	switch (sortBy.value) {
		case BuildingSortOption.AreaAsc:
			return buildings.sort(
				(a, b) =>
					(a.grossArea ?? 0) - (b.grossArea ?? 0) ||
					compareByName(a, b)
			);
		case BuildingSortOption.Name:
			return buildings.sort(compareByName);
		case BuildingSortOption.Address:
			return buildings.sort(compareByAddress);
		default:
			return buildings.sort(
				(a, b) =>
					(b.grossArea ?? 0) - (a.grossArea ?? 0) ||
					compareByName(a, b)
			);
	}
});
</script>

<style lang="scss" scoped>
.v-alert {
	border-radius: $border-radius;
	:deep(.v-icon) {
		color: $grey-darken-3;
	}
}
.sort-button {
	color: $grey-darken-2;
}
.building-item {
	border-radius: 0;
	box-shadow: none;

	.v-card-title {
		color: $black;
		font-size: size(18);
		white-space: normal;

		.building-name {
			align-self: center;
		}
	}
	ul {
		li {
			list-style-type: none;
			display: inline;
			font-size: size(14);
			color: $grey-darken-2;
		}
		li:not(:first-child):before {
			content: '•';
			margin: 0 size(8);
		}
	}
}
</style>
