<template>
	<app-loading-spinner v-if="isBusyFetching" :is-visible="true" />
	<v-alert
		v-else-if="failedToFetchRooms"
		class="mt-2 px-6"
		:class="{ 'mx-6': !noPadding }"
		rounded="lg"
		icon="warning"
	>
		{{ t('app.error.estate.unableToFetchRooms') }}
	</v-alert>
	<v-alert v-else-if="rooms?.length === 0" icon="info" class="mt-2 mx-6">
		{{ t('component.buildingDetails.noRooms') }}
	</v-alert>
	<div v-else-if="rooms">
		<div
			class="filter d-flex flex-wrap ga-4 mt-4"
			:class="{ 'px-6': !noPadding }"
		>
			<v-text-field
				:label="t('component.buildingDetails.room.search')"
				:placeholder="
					t('component.buildingDetails.room.searchPlaceholder')
				"
				prepend-inner-icon="search"
				v-model="searchTerm"
				class="filter-search"
				color="primary"
				rounded="lg"
				variant="outlined"
				density="comfortable"
				clearable
			/>
			<v-autocomplete
				v-if="roomTypes.length"
				:label="t('component.buildingDetails.room.type')"
				:items="roomTypes"
				v-model="selectedRoomType"
				class="filter-type"
				color="primary"
				rounded="lg"
				density="comfortable"
				variant="outlined"
				autocomplete="off"
				clearable
			/>
			<v-select
				:label="t('component.buildingDetails.room.floor')"
				v-model="selectedFloorOption"
				:items="floors ?? []"
				class="filter-floor"
				item-title="name"
				item-value="id"
				color="primary"
				rounded="lg"
				density="comfortable"
				variant="outlined"
				clearable
			/>
		</div>
		<slot name="append-search"></slot>
		<div class="result-wrap mt-4">
			<v-alert
				v-if="!filteredRooms?.length"
				icon="info"
				:class="{ 'mx-6': !noPadding }"
			>
				{{ t('component.buildingDetails.room.noMatches') }}
			</v-alert>
			<v-alert
				v-else-if="!filteredRoomsForFloor?.length"
				icon="info"
				:class="{ 'mx-6': !noPadding }"
				>{{
					t('component.buildingDetails.room.noMatchesSelectedFloor', {
						floor: floors?.find((f) => f.id === selectedFloorId)
							?.name,
					})
				}}
			</v-alert>

			<room-list
				:rooms="filteredRoomsForFloor"
				:focused-room-id="focusedRoomId"
				:no-padding="noPadding"
				@room-click="clickRoom"
			/>
			<div v-if="selectedFloorId && filteredRoomsForOtherFloors.length">
				<h3 class="mt-6 mb-2" :class="{ 'mx-6': !noPadding }">
					{{
						t('component.buildingDetails.room.matchesOnOtherFloors')
					}}
				</h3>
				<room-list
					:rooms="filteredRoomsForOtherFloors"
					:focused-room-id="focusedRoomId"
					:no-padding="noPadding"
					@room-click="clickRoom"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { IBuildingFloor, IBuildingRoom } from '@/models/Interfaces';
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { DispatchType } from '@/models/Enums';
import { IRootState } from '@/models/Interfaces';
import { sortByBoolean } from '@/utils/sortByBoolean';
import { useStore } from 'vuex';
import RoomList from './RoomList.vue';
import ErrorService from '@/utils/ErrorService';
import { pickDefaultFloor } from '../defaultFloor';

const props = defineProps<{
	buildingId: number;
	floor: number | null;
	returnObject?: boolean;
	noPadding?: boolean;
}>();

const emit = defineEmits(['room-selected', 'update:floor']);

const { t } = useI18n();
const store = useStore<IRootState>();

const floors = ref<IBuildingFloor[] | null>(null);
const rooms = ref<IBuildingRoom[] | null>(null);

// Filters
const searchTerm = ref('');
const selectedRoomType = ref<string | null>(null);
const selectedFloorId = computed<number | null>({
	get: () => props.floor,
	set: (value: number | null) => {
		emit('update:floor', value);
	},
});

// The id arrives before the floors it names, and the select would render the
// raw id until the list catches up
const selectedFloorOption = computed<number | null>({
	get: () =>
		floors.value?.some((floor) => floor.id === selectedFloorId.value)
			? selectedFloorId.value
			: null,
	set: (value: number | null) => {
		selectedFloorId.value = value;
	},
});

const normalizeRoomType = (roomType: string | null) => {
	if (roomType?.startsWith('Mötesrum')) {
		return 'Mötesrum';
	}
	return roomType?.trim() ?? '';
};

const roomTypes = computed(() => {
	// Room types are based on popular names
	const roomTypes = Array.from(
		new Set(
			(rooms.value ?? [])
				.map((room) => normalizeRoomType(room.popularName))
				.filter((name) => name.length > 0)
		)
	);

	return roomTypes.sort((a, b) => a.localeCompare(b));
});

const filteredRooms = computed(() => {
	let filtered = rooms.value ?? [];

	if (searchTerm.value) {
		const lowerSearchTerm = searchTerm.value.toLowerCase().trim();
		filtered = filtered.filter(
			(room) =>
				room.name.toLowerCase().includes(lowerSearchTerm) ||
				(room.popularName &&
					room.popularName.toLowerCase().includes(lowerSearchTerm))
		);
	}

	if (selectedRoomType.value) {
		filtered = filtered.filter(
			(room) =>
				normalizeRoomType(room.popularName) === selectedRoomType.value
		);
	}

	return filtered;
});

const filteredRoomsForFloor = computed(() => {
	if (selectedFloorId.value !== null) {
		return filteredRooms.value.filter(
			(room) => room.floorId === selectedFloorId.value
		);
	}

	return filteredRooms.value;
});

const filteredRoomsForOtherFloors = computed(() => {
	if (selectedFloorId.value !== null) {
		return filteredRooms.value.filter(
			(room) => room.floorId !== selectedFloorId.value
		);
	}

	return filteredRooms.value;
});

const isBusyFetchingFloors = ref(false);
const fetchFloors = async (buildingId: number) => {
	isBusyFetchingFloors.value = true;
	try {
		floors.value = await store.dispatch(DispatchType.GetBuildingFloors, {
			buildingId,
			includeRooms: false,
		});

		if (floors.value?.length && selectedFloorId.value === null) {
			selectedFloorId.value = pickDefaultFloor(floors.value)?.id ?? null;
		}
	} catch (err) {
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchFloors'),
		});
	} finally {
		isBusyFetchingFloors.value = false;
	}
};

const isBusyFetching = ref(false);
const failedToFetchRooms = ref(false);
const fetchRooms = async (buildingId: number) => {
	isBusyFetching.value = true;

	try {
		const roomsResponse = await store.dispatch(
			DispatchType.GetBuildingRooms,
			{
				buildingId,
			}
		);
		rooms.value = sortByBoolean(roomsResponse, (room) => room.isFavorite);
		failedToFetchRooms.value = false;
	} catch (err) {
		failedToFetchRooms.value = true;
		ErrorService.onError({
			err,
			message: t('app.error.estate.unableToFetchRooms'),
			hidden: true,
		});
	} finally {
		isBusyFetching.value = false;
	}
};

watch(
	() => props.buildingId,
	(newBuildingId) => {
		fetchRooms(newBuildingId);
		fetchFloors(newBuildingId);
	},
	{ immediate: true }
);

const focusedRoomId = ref<number | null>(null);
const focusRoom = async (roomId: number | null) => {
	if (roomId === null) {
		focusedRoomId.value = null;
		return;
	}

	focusedRoomId.value = roomId;
};

const clickRoom = async (roomId: number | null) => {
	focusedRoomId.value = roomId;

	if (props.returnObject) {
		const room = rooms.value?.find((r) => r.id === roomId) ?? null;
		emit('room-selected', room);
	} else {
		emit('room-selected', roomId);
	}
};

defineExpose({
	focusRoom,
});
</script>

<style lang="scss" scoped>
.v-alert {
	border-radius: $border-radius;
	:deep(.v-icon) {
		color: $grey-darken-3;
	}
}
h3 {
	font-size: size(18);
}
.filter {
	.v-input {
		min-width: 150px;
		flex: 2;

		&.filter-floor {
			flex: 1;
		}
	}

	@media only screen and (max-width: $estate-mobile-threshold) {
		.v-input {
			min-width: 0;

			&.filter-search {
				flex: 1 1 100%;
			}
			&.filter-type,
			&.filter-floor {
				flex: 1 1 calc(50% - 8px);
			}
		}
	}
}
.result-wrap {
	min-height: 20svh;
	overflow: hidden;
	padding: 3px;
}
</style>
