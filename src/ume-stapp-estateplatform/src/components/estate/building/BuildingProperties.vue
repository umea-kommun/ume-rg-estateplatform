<template>
	<div>
		<div class="properties-wrap d-flex align-start ga-4">
			<div>
				<div class="properties">
					<div
						class="prop"
						v-for="prop in properties"
						:key="prop.label"
						v-show="prop.value"
					>
						<div class="label">{{ prop.label }}</div>
						<div class="value" :class="prop.class">
							{{ prop.value }}
						</div>
					</div>
				</div>

				<div class="metrics d-flex flex-wrap align-center ga-3 mt-4">
					<estate-type-label :type="EstateType.Building" />
					<ul class="pa-0 ma-0">
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
						<li v-if="building.metrics?.areaSqm">
							{{
								$t('estateCommon.usableArea', {
									area: building.metrics.areaSqm.toLocaleString(),
								})
							}}
						</li>
					</ul>
				</div>
			</div>
			<building-image
				v-if="building.imageUrl"
				:src="building.imageUrl"
				:image-width="300"
				:alt="$t('component.buildingDetails.buildingImageAlt')"
				class="cursor-pointer building-image"
				@click="showImageInModal(building.imageUrl)"
			/>
		</div>
		<base-image-modal />
	</div>
</template>

<script setup lang="ts">
import { useBaseImageModal } from '@/components/shared/baseImageModal';
import BaseImageModal from '@/components/shared/BaseImageModal.vue';
import { IBuildingDetails } from '@/models/Interfaces';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import BuildingImage from './BuildingImage.vue';
import EstateTypeLabel from '../EstateTypeLabel.vue';
import { EstateType } from '@/models/Enums';

const props = defineProps<{
	building: IBuildingDetails;
}>();

const { t } = useI18n();
const { showImageInModal } = useBaseImageModal();

const lowercaseAddress = computed(() => {
	if (!props.building?.address) return '';
	const adr = props.building.address;
	const address = `${adr.street.trim()}, ${adr.zipCode.trim()} ${adr.city.trim()}`;
	return address.toLocaleLowerCase();
});

const properties = computed(() => {
	return [
		{
			label: t('estateCommon.addressLabel'),
			value: lowercaseAddress.value,
			class: 'text-capitalize',
		},
		{
			label: t('estateCommon.yearOfConstructionLabel'),
			value: props.building?.metrics?.yearOfConstruction,
		},
		{
			label: t('estateCommon.operationalAreaLabel'),
			value: props.building?.region.name?.toLocaleLowerCase(),
			class: 'text-capitalize',
		},
		{
			label: t('estateCommon.externalOwner.status'),
			value: props.building?.externalOwnerInfo?.status,
		},
	];
});
</script>

<style scoped lang="scss">
.properties-wrap {
	justify-content: space-between;

	@media only screen and (max-width: 620px) {
		flex-wrap: wrap-reverse;

		.building-image {
			width: 100%;
			height: 120px;
			aspect-ratio: initial;
		}
	}
}
</style>
