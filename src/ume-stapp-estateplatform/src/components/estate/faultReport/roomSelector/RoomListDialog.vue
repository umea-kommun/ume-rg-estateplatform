<template>
	<!-- The room list in a dialog with a pinned filter header and a bounded,
	virtualised scroll body instead of stretching the page. On mobile it goes
	fullscreen (the dialog fills the screen, so there's no nested-scroll fight
	with the page). -->
	<v-dialog
		v-model="open"
		class="room-list-dialog estate-default"
		:fullscreen="isMobile"
		:max-width="720"
		aria-labelledby="room-list-dialog-title"
	>
		<v-card
			class="dialog-card"
			:class="{ 'dialog-card--capped': !isMobile }"
		>
			<v-card-title id="room-list-dialog-title" class="dialog-title px-6">
				<span>{{ t('component.roomSelector.methodList') }}</span>
				<v-btn
					icon="close"
					variant="text"
					size="small"
					:aria-label="t('app.nav.close')"
					@click="open = false"
				/>
			</v-card-title>

			<div class="filters px-6 pb-4">
				<v-text-field
					v-model="searchTerm"
					:label="t('component.buildingDetails.room.search')"
					:placeholder="
						t('component.buildingDetails.room.searchPlaceholder')
					"
					prepend-inner-icon="search"
					color="primary"
					rounded="lg"
					variant="outlined"
					density="comfortable"
					hide-details
					clearable
					:autofocus="!isMobile"
					class="filter-search"
				/>
				<v-autocomplete
					v-if="roomTypes.length"
					v-model="selectedRoomType"
					:items="roomTypes"
					:label="t('component.buildingDetails.room.type')"
					color="primary"
					rounded="lg"
					variant="outlined"
					density="comfortable"
					autocomplete="off"
					hide-details
					clearable
					class="filter-type"
				/>
				<v-select
					v-model="selectedFloorId"
					:items="floors ?? []"
					item-title="name"
					item-value="id"
					:label="t('component.buildingDetails.room.floor')"
					color="primary"
					rounded="lg"
					variant="outlined"
					density="comfortable"
					hide-details
					clearable
					class="filter-floor"
				/>
			</div>

			<div class="list-body">
				<app-loading-spinner v-if="isBusyFetching" :is-visible="true" />
				<div
					v-else-if="failedToFetchRooms"
					class="empty-state empty-state--error"
				>
					<span class="empty-state__icon empty-state__icon--warning">
						<v-icon icon="warning" :size="28" />
					</span>
					<p class="empty-state__title">
						{{ t('component.roomSelector.fetchErrorTitle') }}
					</p>
					<p class="empty-state__text">
						{{ t('app.error.estate.unableToFetchRooms') }}
					</p>
					<p class="empty-state__text">
						{{ t('component.roomSelector.fetchErrorSkipHint') }}
					</p>
				</div>
				<div v-else-if="!filteredRooms.length" class="empty-state">
					<span class="empty-state__icon">
						<v-icon icon="search_off" :size="28" />
					</span>
					<p class="empty-state__title">
						{{ t('component.roomSelector.noMatchesTitle') }}
					</p>
					<p class="empty-state__text">
						{{ t('component.roomSelector.noMatchesSkipHint') }}
					</p>
					<v-btn
						v-if="hasActiveFilters"
						variant="outlined"
						color="grey-darken-2"
						rounded="lg"
						prepend-icon="close"
						@click="clearFilters"
					>
						{{ t('component.roomSelector.clearFilters') }}
					</v-btn>
				</div>
				<!-- Virtualised so opening a building with hundreds of rooms
				doesn't block on mounting every row. -->
				<v-virtual-scroll
					v-else
					:items="filteredRooms"
					:item-height="57"
					class="room-options"
					role="listbox"
				>
					<template #default="{ item: room }">
						<div
							class="room-option"
							role="option"
							tabindex="0"
							@click="onSelect(room)"
							@keydown.enter="onSelect(room)"
							@keydown.space.prevent="onSelect(room)"
						>
							<span class="room-option__text">
								<span class="room-option__name">
									<template v-if="room.popularName">
										{{ room.name }} -
										{{ room.popularName }}
									</template>
									<template v-else>
										{{ room.name }}
									</template>
								</span>
								<span class="room-option__meta">
									{{
										t('component.roomSelector.floorArea', {
											floor: room.floorName,
											area: room.grossArea?.toLocaleString(),
										})
									}}
								</span>
							</span>
							<v-icon
								class="room-option__chevron"
								icon="chevron_right"
								:size="20"
							/>
						</div>
					</template>
				</v-virtual-scroll>
			</div>

			<v-card-actions class="px-6 py-3 justify-end">
				<v-btn variant="text" @click="open = false">
					{{ t('app.nav.close') }}
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
/**
 * The "choose a room from the list" dialog for the fault report flow. Owns its
 * own room/floor data (fetched per building), the search + type + floor filters,
 * and a virtualised list. Rooms are fetched when the building changes - not on
 * open - so the dialog appears instantly. Emits `select` and closes on pick.
 */
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import {
	IBuildingDetails,
	IBuildingFloor,
	IBuildingRoom,
	IRootState,
} from '@/models/Interfaces';
import { DispatchType } from '@/models/Enums';
import ErrorService from '@/utils/ErrorService';
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useStore } from 'vuex';
import { useEstateIsMobile } from '../../useEstateIsMobile';

const props = defineProps<{
	modelValue: boolean;
	building: IBuildingDetails;
}>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	select: [room: IBuildingRoom];
}>();

const { t } = useI18n();
const store = useStore<IRootState>();
const isMobile = useEstateIsMobile();

const open = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

// Natural, numeric-aware ordering for floor names and room numbers.
const roomCollator = new Intl.Collator('sv', {
	numeric: true,
	sensitivity: 'base',
});

const floors = ref<IBuildingFloor[] | null>(null);
const rooms = ref<IBuildingRoom[] | null>(null);

const searchTerm = ref('');
const selectedRoomType = ref<string | null>(null);
const selectedFloorId = ref<number | null>(null);

const clearFilters = () => {
	searchTerm.value = '';
	selectedRoomType.value = null;
	selectedFloorId.value = null;
};

const hasActiveFilters = computed(
	() =>
		!!searchTerm.value?.trim() ||
		selectedRoomType.value !== null ||
		selectedFloorId.value !== null
);

// Room types come from popular names; the various "Mötesrum ..." variants are
// collapsed into a single "Mötesrum" bucket so the filter stays short.
const normalizeRoomType = (roomType: string | null) => {
	if (roomType?.startsWith('Mötesrum')) {
		return 'Mötesrum';
	}
	return roomType?.trim() ?? '';
};

const roomTypes = computed(() =>
	Array.from(
		new Set(
			(rooms.value ?? [])
				.map((room) => normalizeRoomType(room.popularName))
				.filter((name) => name.length > 0)
		)
	).sort((a, b) => a.localeCompare(b))
);

const filteredRooms = computed(() => {
	let filtered = rooms.value ?? [];

	if (selectedFloorId.value !== null) {
		filtered = filtered.filter(
			(room) => room.floorId === selectedFloorId.value
		);
	}

	if (selectedRoomType.value) {
		filtered = filtered.filter(
			(room) =>
				normalizeRoomType(room.popularName) === selectedRoomType.value
		);
	}

	const term = searchTerm.value?.toLowerCase().trim();
	if (term) {
		filtered = filtered.filter(
			(room) =>
				room.name.toLowerCase().includes(term) ||
				room.popularName?.toLowerCase().includes(term)
		);
	}

	return filtered;
});

const onSelect = (room: IBuildingRoom) => {
	open.value = false;
	emit('select', room);
};

const isBusyFetching = ref(false);
const failedToFetchRooms = ref(false);

const fetchRooms = async (buildingId: number) => {
	isBusyFetching.value = true;
	try {
		const roomsResponse: IBuildingRoom[] = await store.dispatch(
			DispatchType.GetBuildingRooms,
			{ buildingId }
		);
		// Ignore a response for a building the user has since switched away from,
		// so a slow request can't repopulate the dialog with stale rooms.
		if (buildingId !== props.building.id) {
			return;
		}
		// Order by floor ascending, then by room number ascending within a floor.
		// Numeric collation so e.g. floor 2 sorts before 10 and 9-1005 before
		// 9-1033.
		rooms.value = [...roomsResponse].sort((a, b) => {
			const byFloor = roomCollator.compare(a.floorName, b.floorName);
			return byFloor !== 0
				? byFloor
				: roomCollator.compare(a.name, b.name);
		});
		failedToFetchRooms.value = false;
	} catch (err) {
		if (buildingId !== props.building.id) {
			return;
		}
		failedToFetchRooms.value = true;
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchRooms'),
			hidden: true,
		});
	} finally {
		if (buildingId === props.building.id) {
			isBusyFetching.value = false;
		}
	}
};

const fetchFloors = async (buildingId: number) => {
	try {
		const floorsResponse: IBuildingFloor[] = await store.dispatch(
			DispatchType.GetBuildingFloors,
			{
				buildingId,
				includeRooms: false,
			}
		);
		// See fetchRooms: drop a stale response for a previous building.
		if (buildingId !== props.building.id) {
			return;
		}
		floors.value = floorsResponse;
	} catch (err) {
		if (buildingId !== props.building.id) {
			return;
		}
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchFloors'),
			hidden: true,
		});
	}
};

// Prefetch on building change so the dialog opens instantly; also drop any
// filters carried over from a previous building.
watch(
	() => props.building.id,
	(buildingId) => {
		clearFilters();
		rooms.value = null;
		floors.value = null;
		failedToFetchRooms.value = false;
		fetchRooms(buildingId);
		fetchFloors(buildingId);
	},
	{ immediate: true }
);

// Open the list fresh: clear any filters left from a previous visit.
watch(open, (isOpen) => {
	if (isOpen) {
		clearFilters();
	}
});
</script>

<style scoped lang="scss">
// Not nested under a page wrapper: the dialog content teleports to the overlay.
.room-list-dialog {
	// Flex column so the title + filters + close row stay put and only the list
	// body scrolls.
	.dialog-card {
		display: flex;
		flex-direction: column;
	}

	// Desktop: a fixed, capped height. `flex: none` stops the overlay's flex
	// column from stretching the card to fill 90vh; fixed (not max) height means
	// the dialog stays the same size - and the filters don't jump - as the result
	// count changes; capped because a full 90vh is needlessly tall on a big
	// monitor.
	.dialog-card--capped {
		flex: none;
		height: min(90vh, 640px);
	}

	// Mobile: the dialog is fullscreen (no --capped), so fill the screen height.
	&.v-overlay--fullscreen .dialog-card {
		height: 100%;
	}

	.dialog-title {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12px;
	}

	.v-alert {
		border-radius: $border-radius;
		:deep(.v-icon) {
			color: $grey-darken-3;
		}
	}

	// Search + room type + floor on one wrapping row, pinned above the scroll body.
	.filters {
		display: flex;
		flex-wrap: wrap;
		gap: 12px;

		.filter-search {
			flex: 2 1 200px;
			min-width: 0;
		}
		.filter-type {
			flex: 1 1 160px;
		}
		.filter-floor {
			flex: 1 1 130px;
		}
	}

	// Fills the remaining card height; the virtual list inside owns the scroll.
	// Full width - the rows carry their own gutter so they can highlight edge to
	// edge on hover.
	.list-body {
		flex: 1 1 auto;
		min-height: 0;
		padding-bottom: 4px;
		display: flex;
		flex-direction: column;
	}

	// Centered empty state shown when the filters match no rooms.
	.empty-state {
		flex: 1 1 auto;
		min-height: 0;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		gap: 8px;
		padding: 24px;

		&__icon {
			width: 56px;
			height: 56px;
			border-radius: 50%;
			display: grid;
			place-items: center;
			background: rgba(0, 0, 0, 0.05);
			color: $grey-darken-1;
			margin-bottom: 4px;

			&--warning {
				background: $warning-bg;
				color: $warning;
			}
		}
		&__title {
			margin: 0;
			font-size: size(16);
			font-weight: 600;
			color: $black;
		}
		&__text {
			margin: 0;
			max-width: 340px;
			font-size: size(14);
			color: $grey-darken-2;
		}
	}

	// Flat, dense, virtualised list. The v-virtual-scroll root is the scroll
	// container; it fills .list-body and only renders the visible rows. Full
	// width with a top rule separating it from the filters; rows are divided by
	// bottom borders (no wrapping box, no radius).
	.room-options {
		flex: 1 1 auto;
		min-height: 0;
		border-top: solid 1px $grey-lighten-3;

		.room-option {
			display: flex;
			align-items: center;
			gap: 12px;
			padding: 12px 24px;
			cursor: pointer;
			border-bottom: solid 1px $grey-lighten-3;
			transition: background-color 0.15s ease;

			&:hover,
			&:focus-visible {
				background-color: rgba($primary, 0.08);
				outline: none;

				.room-option__chevron,
				.room-option__name {
					color: $primary;
				}
			}

			&__text {
				flex: 1 1 auto;
				min-width: 0;
				display: flex;
				flex-wrap: wrap;
				align-items: baseline;
				gap: 2px 12px;
			}
			&__name {
				color: $black;
				font-size: size(16);
				font-weight: 500;
			}
			&__meta {
				color: $grey-darken-1;
				font-size: size(14);
			}
			&__chevron {
				flex: 0 0 auto;
				color: $grey-lighten-5;
			}
		}
	}
}
</style>
