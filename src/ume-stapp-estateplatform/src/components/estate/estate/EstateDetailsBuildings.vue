<template>
	<app-loading-spinner v-if="loading" :is-visible="true" />
	<v-alert v-else-if="buildings?.length === 0" icon="info" class="mt-2 mx-6">
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
					<li v-if="building.address?.street" class="text-capitalize">
						{{ building.address?.street.toLocaleLowerCase() }}
					</li>
					<li v-if="building.metrics?.floorCount">
						{{
							$t('estateCommon.floorCount', {
								count: building.metrics?.floorCount,
							})
						}}
					</li>
					<li v-if="building.metrics?.roomCount">
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
</template>

<script setup lang="ts">
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { IEstateBuilding } from '@/models/Interfaces';
import { EstateRoutes } from '@/router/routes';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import FavoriteButton from '../favorite/FavoriteButton.vue';
import { EstateType } from '@/models/Enums';

const props = defineProps<{
	loading: boolean;
	buildings: IEstateBuilding[] | null;
}>();

const emit = defineEmits(['building-mouseenter', 'building-mouseleave']);

const { t } = useI18n();

const sortedBuildings = computed(() => {
	return props.buildings
		? [...props.buildings].sort((a, b) => {
				const nameA = a.popularName || a.name;
				const nameB = b.popularName || b.name;
				return nameA.localeCompare(nameB);
		  })
		: null;
});
</script>

<style lang="scss" scoped>
.v-alert {
	border-radius: $border-radius;
	:deep(.v-icon) {
		color: $grey-darken-3;
	}
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
