<template>
	<v-card
		class="estate-search-result-item"
		:to="navigation"
		:disabled="loading"
	>
		<building-image
			v-if="entry.imageUrl"
			:src="entry.imageUrl"
			:image-width="300"
			class="building-image-mobile"
		/>
		<div class="content">
			<div class="d-flex align-center">
				<v-card-title class="justify-space-between">
					<div class="title">
						{{ entry.popularName || entry.name }}
						<span
							v-if="
								entry.type === EstateType.Room &&
								entry.popularName
							"
						>
							- {{ entry.name }}
						</span>
					</div>
					<favorite-button
						:id="entry.id"
						:type="entry.type"
						:isFavorite="entry.isFavorite"
						size="small"
					/>
				</v-card-title>
			</div>
			<div class="inner-content">
				<v-card-text class="pt-1">
					<div class="properties">
						<div
							class="prop"
							v-for="prop in properties"
							:key="prop.label"
							v-show="prop.value"
						>
							<div class="label">{{ prop.label }}</div>
							<div class="value">
								{{ prop.value }}
							</div>
						</div>
					</div>
					<div class="chip-properties">
						<v-chip
							class="estate-type"
							:class="entry.type"
							variant="flat"
							:color="typeColor"
							>{{ estateType }}</v-chip
						>
						<v-chip
							v-if="
								entry.type === EstateType.Estate &&
								entry.metrics?.buildingCount
							"
						>
							{{
								$t('estateCommon.buildingCount', {
									count: entry.metrics?.buildingCount,
								})
							}}
						</v-chip>
						<v-chip
							v-if="
								entry.type === EstateType.Building &&
								entry.metrics?.floorCount
							"
							>{{
								$t('estateCommon.floorCount', {
									count: entry.metrics?.floorCount,
								})
							}}
						</v-chip>
						<v-chip
							v-if="
								entry.type === EstateType.Building &&
								entry.metrics?.roomCount
							"
						>
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
				</v-card-text>
				<building-image
					v-if="entry.imageUrl"
					:src="entry.imageUrl"
					:image-width="300"
					class="building-image-desktop ma-4"
				/>
			</div>
		</div>
		<div class="loading-overlay loader-lazy" v-if="loading">
			<v-progress-circular
				color="primary"
				:size="32"
				:width="2"
				indeterminate
			/>
		</div>
	</v-card>
</template>

<script setup lang="ts">
import { EstateType } from '@/models/Enums';
import { IEstateSearchResultEntry } from '@/models/Interfaces';
import { EstateRoutes } from '@/router/routes';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import BuildingImage from '../building/BuildingImage.vue';
import FavoriteButton from '../favorite/FavoriteButton.vue';

const props = defineProps<{
	entry: IEstateSearchResultEntry;
	loading?: boolean;
}>();

const { t } = useI18n();

const navigation = computed(() => {
	switch (props.entry.type) {
		case EstateType.Estate:
			return {
				name: EstateRoutes.EstateDetails,
				params: { estateId: props.entry.id },
			};
		case EstateType.Building:
			return {
				name: EstateRoutes.BuildingDetails,
				params: { buildingId: props.entry.id },
			};
		case EstateType.Room: {
			const buildingId = props.entry.ancestors?.find(
				(ancestor) => ancestor.type === EstateType.Building
			)?.id;
			return {
				name: EstateRoutes.BuildingDetails,
				params: { buildingId: buildingId },
				query: { roomId: props.entry.id },
			};
		}
	}
	return undefined;
});

const properties = computed(() => {
	switch (props.entry.type) {
		case EstateType.Estate:
			return [
				{
					label: t('estateCommon.propertyDesignationLabel'),
					value: props.entry.name,
				},
				{
					label: t('estateCommon.municipalityAreaLabel'),
					value: props.entry.municipalityArea,
				},
				{
					label: t('estateCommon.operationalAreaLabel'),
					value: props.entry.operationalArea,
				},
			];
		case EstateType.Building:
			return [
				{
					label: t('estateCommon.propertyLabel'),
					value:
						props.entry.ancestors?.[0]?.popularName ||
						props.entry.ancestors?.[0]?.name,
				},
				{
					label: t('estateCommon.addressLabel'),
					value: props.entry.address?.street?.trim(),
				},
			];
		case EstateType.Room:
			return [
				{
					label: t('estateCommon.propertyLabel'),
					value:
						props.entry.ancestors?.[0]?.popularName ||
						props.entry.ancestors?.[0]?.name,
				},
				{
					label: t('estateCommon.buildingLabel'),
					value:
						props.entry.ancestors?.[1]?.popularName ||
						props.entry.ancestors?.[1]?.name,
				},
			];
	}
	return [];
});

const typeColor = computed(() => {
	switch (props.entry?.type) {
		case EstateType.Estate:
			return 'info';
		case EstateType.Building:
			return 'primary';
		case EstateType.Room:
			return 'accent';
	}
	return '';
});

const estateType = computed(() => {
	if (!props.entry?.type) return '';
	switch (props.entry.type) {
		case EstateType.Estate:
			return t('estateCommon.type.estate');
		case EstateType.Building:
			return t('estateCommon.type.building');
		case EstateType.Room:
			return t('estateCommon.type.room');
	}
	return props.entry.type;
});
</script>

<style lang="scss" scoped>
.estate-search-result-item {
	.inner-content {
		display: flex;
		align-items: start;
		justify-content: space-between;
	}
	.v-card-title {
		font-weight: bold;
		font-size: size(18);
		color: $black;
		flex: 1;
		display: flex;
		align-items: start;
		gap: 4px;

		.title {
			text-overflow: initial;
			white-space: normal;
			text-transform: capitalize;
			word-break: break-word;
		}
	}
	.properties {
		display: flex;
		flex-wrap: wrap;
		gap: 10px;
		.prop {
			margin-right: 20px;
			.label {
				font-size: size(13);
				color: $grey-darken-1;
				text-transform: uppercase;
			}
			.value {
				font-size: size(16);
				word-break: break-all;
			}
		}
	}
	.chip-properties {
		.v-chip {
			margin-top: 1rem;
			margin-right: 1rem;
		}
	}
	.building-image-mobile {
		display: none;
		margin: 0;
		max-height: 120px;
		width: 100%;
		border-radius: 0;
		object-fit: cover;
	}
	.loading-overlay {
		position: absolute;
		inset: 0;
		display: flex;
		align-items: center;
		justify-content: center;
	}
	@media only screen and (max-width: 620px) {
		.inner-content {
			flex-wrap: wrap-reverse;
		}
		.building-image-mobile {
			display: block;
		}
		.building-image-desktop {
			display: none;
		}
	}
}
</style>
