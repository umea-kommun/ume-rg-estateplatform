<template>
	<v-list-item class="building-selector-item" :to="undefined" rounded="lg">
		<div>
			<v-list-item-title>
				{{ entry.popularName || entry.name }}
			</v-list-item-title>

			<v-list-item-subtitle class="mt-1 mb-2">
				{{ address }}
			</v-list-item-subtitle>
			<div class="mt-2 mb-1 d-flex flex-wrap ga-2">
				<v-chip v-if="entry.metrics?.floorCount"
					>{{
						$t('estateCommon.floorCount', {
							count: entry.metrics?.floorCount,
						})
					}}
				</v-chip>
				<v-chip v-if="entry.metrics?.roomCount">
					{{
						$t('estateCommon.roomCount', {
							count: entry.metrics?.roomCount,
						})
					}}
				</v-chip>

				<v-chip v-if="entry.metrics?.areaSqm">
					{{ entry.metrics?.areaSqm?.toLocaleString() }}
					m²
				</v-chip>
			</div>
		</div>
		<building-image
			v-if="entry.imageUrl"
			:src="entry.imageUrl"
			:image-width="200"
		/>
	</v-list-item>
</template>

<script setup lang="ts">
import {
	IBuildingDetails,
	IEstateSearchResultEntry,
} from '@/models/estate/Interfaces';
import BuildingImage from '../../building/BuildingImage.vue';
import { computed } from 'vue';

const props = defineProps<{
	entry: IEstateSearchResultEntry | IBuildingDetails;
}>();

const address = computed(() => {
	const street = props.entry.address?.street?.trim() || '';
	const zipCode = props.entry.address?.zipCode;
	const city = props.entry.address?.city;

	if (street && zipCode && city) {
		return `${street}, ${zipCode} ${city}`.toLocaleLowerCase();
	} else if (street && city) {
		return `${street}, ${city}`.toLocaleLowerCase();
	} else if (street) {
		return street.toLocaleLowerCase();
	} else {
		return '';
	}
});
</script>

<style scoped lang="scss">
.building-selector-item {
	color: $grey-darken-4;

	.v-list-item-title {
		font-weight: bold;
		font-size: size(18);
		text-wrap: wrap;
	}
	.v-list-item-subtitle {
		opacity: 1;
		color: $grey-darken-3;
		text-transform: capitalize;
	}
	:deep(.v-list-item__content) {
		display: flex;
		justify-content: space-between;
		gap: 16px;
	}
	.building-image {
		height: fit-content;
		width: 120px;
	}

	@media only screen and (max-width: 620px) {
		.building-image {
			width: 100%;
			height: 100px;
		}

		:deep(.v-list-item__content) {
			flex-wrap: wrap-reverse;
		}
	}
}
</style>
