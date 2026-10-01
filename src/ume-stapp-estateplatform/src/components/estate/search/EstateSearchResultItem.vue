<template>
	<v-card
		class="estate-search-result-item"
		:class="{ 'estate-search-result-item--compact': compact }"
		:to="navigation"
		:disabled="loading"
	>
		<!-- Compact: avatar + title + subtitle, like the selected-building card.
		Used where the user already knows the item, e.g. their own favourites. -->
		<div v-if="compact" class="compact-row">
			<div class="compact-avatar">
				<img
					v-if="entry.imageUrl"
					:src="`${entry.imageUrl}?w=88`"
					:alt="entry.popularName || entry.name"
					class="compact-avatar__img"
				/>
				<div v-else class="compact-avatar__icon" :class="entry.type">
					<v-icon :icon="typeIcon" :size="22" />
				</div>
			</div>
			<div class="compact-text">
				<div class="compact-title">
					{{ entry.popularName || entry.name }}
					<span
						v-if="
							entry.type === EstateType.Room && entry.popularName
						"
					>
						- {{ entry.name }}
					</span>
				</div>
				<div v-if="compactSubtitle" class="compact-subtitle">
					{{ compactSubtitle }}
				</div>
			</div>
			<favorite-button
				:id="entry.id"
				:type="entry.type"
				:isFavorite="entry.isFavorite"
				size="small"
			/>
		</div>

		<template v-else>
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
						<div
							class="metrics d-flex flex-wrap align-center ga-3 mt-4"
						>
							<estate-type-label :type="entry.type" />
							<ul class="pa-0 ma-0">
								<li
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
								</li>
								<li
									v-if="
										entry.type === EstateType.Building &&
										entry.hasRoomInformation !== false &&
										entry.metrics?.floorCount
									"
								>
									{{
										$t('estateCommon.floorCount', {
											count: entry.metrics?.floorCount,
										})
									}}
								</li>
								<li
									v-if="
										entry.type === EstateType.Building &&
										entry.hasRoomInformation !== false &&
										entry.metrics?.roomCount
									"
								>
									{{
										$t('estateCommon.roomCount', {
											count: entry.metrics?.roomCount,
										})
									}}
								</li>
								<li v-if="entry.metrics?.areaSqm">
									{{
										entry.metrics?.areaSqm?.toLocaleString()
									}}
									m²
								</li>
							</ul>
						</div>
					</v-card-text>
					<building-image
						v-if="entry.imageUrl"
						:src="entry.imageUrl"
						:image-width="300"
						class="building-image-desktop mr-4 mb-4"
					/>
				</div>
			</div>
		</template>

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
import EstateTypeLabel from '../EstateTypeLabel.vue';

const props = defineProps<{
	entry: IEstateSearchResultEntry;
	loading?: boolean;
	// Slim single-row rendering (icon + name + favourite), for lists where the
	// user already knows the item and doesn't need the full details.
	compact?: boolean;
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

// A short secondary line for the compact row: address for a building, the
// building it sits in for a room, the area for an estate.
const compactSubtitle = computed(() => {
	switch (props.entry?.type) {
		case EstateType.Building:
			return props.entry.address?.street?.trim() ?? '';
		case EstateType.Room:
			return (
				props.entry.ancestors?.find(
					(ancestor) => ancestor.type === EstateType.Building
				)?.popularName ??
				props.entry.ancestors?.find(
					(ancestor) => ancestor.type === EstateType.Building
				)?.name ??
				''
			);
		case EstateType.Estate:
			return props.entry.municipalityArea ?? '';
	}
	return '';
});

const typeIcon = computed(() => {
	switch (props.entry?.type) {
		case EstateType.Estate:
			return 'home_work';
		case EstateType.Building:
			return 'apartment';
		case EstateType.Room:
			return 'meeting_room';
	}
	return '';
});
</script>

<style lang="scss" scoped>
.estate-search-result-item {
	// Compact variant: avatar + title + subtitle row, like the selected-building
	// card, instead of the full detail card.
	&--compact {
		.compact-row {
			display: flex;
			align-items: center;
			gap: 12px;
			padding: 12px 16px;
		}
		.compact-avatar {
			flex: 0 0 auto;
			width: 44px;
			height: 44px;

			&__img {
				width: 44px;
				height: 44px;
				border-radius: 50%;
				object-fit: cover;
				display: block;
			}
			&__icon {
				width: 44px;
				height: 44px;
				border-radius: 50%;
				display: grid;
				place-items: center;

				// Same tints as EstateTypeLabel.
				&.estate {
					background: rgba($info, 0.16);
					color: $info;
				}
				&.building {
					background: rgba($primary, 0.16);
					color: $primary;
				}
				&.room {
					background: rgba($accent, 0.4);
					color: $grey-darken-4;
				}
			}
		}
		.compact-text {
			flex: 1 1 auto;
			min-width: 0;
		}
		.compact-title {
			font-weight: 500;
			font-size: size(16);
			color: $black;
			word-break: break-word;
		}
		.compact-subtitle {
			font-size: size(14);
			color: $grey-darken-2;
		}
	}

	// Full card: the content fills the card's height, so the type label and
	// metrics sit at the bottom even when the image (or a taller card next to
	// it in a grid) makes the card taller than its text.
	&:not(.estate-search-result-item--compact) {
		display: flex;
		flex-direction: column;
	}
	.content {
		flex: 1;
		display: flex;
		flex-direction: column;
	}
	.inner-content {
		flex: 1;
		display: flex;
		justify-content: space-between;

		.v-card-text {
			display: flex;
			flex-direction: column;
			justify-content: space-between;
		}
	}
	// Sized to fit beside the text (120×90 instead of the shared 140×105) and
	// flush with the title row, so a photo doesn't make the card taller.
	.building-image-desktop {
		align-self: flex-start;
		width: 120px;
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
				word-break: break-word;
			}
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
