<template>
	<app-loading-spinner v-if="isBusyFetching" :is-visible="true" />
	<v-alert v-else-if="rooms?.length === 0" icon="info" class="mt-2 mx-6">
		{{ t('component.internal.buildingDetails.noRooms') }}
	</v-alert>
	<div v-else-if="rooms">
		<div class="filter d-flex flex-wrap ga-4 mt-4 px-6">
			<v-text-field
				:label="t('component.internal.buildingDetails.room.search')"
				:placeholder="
					t(
						'component.internal.buildingDetails.room.searchPlaceholder'
					)
				"
				v-model="searchTerm"
				color="primary"
				rounded="lg"
				variant="outlined"
				density="comfortable"
				clearable
			/>
			<v-select
				:label="t('component.internal.buildingDetails.room.floor')"
				v-model="selectedFloorId"
				:items="floors ?? []"
				item-title="name"
				item-value="id"
				color="primary"
				rounded="lg"
				density="comfortable"
				variant="outlined"
				clearable
			/>
		</div>
		<div class="mt-2">
			<v-alert
				v-if="!filteredRooms?.length"
				icon="info"
				class="mt-4 mx-6"
			>
				{{ t('component.internal.buildingDetails.room.noMatches') }}
			</v-alert>
			<div
				v-for="room in filteredRooms"
				:key="room.id"
				class="room-item px-6 pt-4"
				:data-room-id="room.id"
				:class="{ focused: room.id === focusedRoomId }"
				@click="clickRoom(room.id)"
			>
				<div v-if="room.popularName" class="title">
					{{ room.popularName }} - {{ room.name }}
				</div>
				<div v-else class="title">{{ room.name }}</div>
				<div class="properties d-flex ga-2">
					<div>
						{{ t('component.internal.buildingDetails.floorLabel') }}
						{{ room.floorName }}
					</div>
					&bull;
					<div>{{ room.grossArea?.toLocaleString() }} m²</div>
				</div>
				<hr class="mt-4" />
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { IBuildingFloor, IBuildingRoom } from '@/models/estate/Interfaces';
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { DispatchType } from '@/models/Enums';
import { IRootState } from '@/models/Interfaces';
import { useStore } from 'vuex';

const props = defineProps<{
	buildingId: number;
	floor: number | null;
}>();

const emit = defineEmits(['room-selected', 'update:floor']);

const { t } = useI18n();
const store = useStore<IRootState>();

const floors = ref<IBuildingFloor[] | null>(null);
const rooms = ref<IBuildingRoom[] | null>(null);

// Filters
const searchTerm = ref('');
const selectedFloorId = computed<number | null>({
	get: () => props.floor,
	set: (value: number | null) => {
		emit('update:floor', value);
	},
});

const filteredRooms = computed(() => {
	let filtered = rooms.value ?? [];

	if (searchTerm.value) {
		const lowerSearchTerm = searchTerm.value.toLowerCase();
		filtered = filtered.filter(
			(room) =>
				room.name.toLowerCase().includes(lowerSearchTerm) ||
				(room.popularName &&
					room.popularName.toLowerCase().includes(lowerSearchTerm))
		);
	}

	if (selectedFloorId.value !== null) {
		filtered = filtered.filter(
			(room) => room.floorId === selectedFloorId.value
		);
	}

	return filtered;
});

const isBusyFetchingFloors = ref(false);
const fetchFloors = async (buildingId: number) => {
	isBusyFetchingFloors.value = true;
	floors.value = await store.dispatch(DispatchType.GetBuildingFloors, {
		buildingId,
		includeRooms: false,
	});
	isBusyFetchingFloors.value = false;
};

const isBusyFetching = ref(false);
const fetchRooms = async (buildingId: number) => {
	isBusyFetching.value = true;
	rooms.value = await store.dispatch(DispatchType.GetBuildingRooms, {
		buildingId,
	});
	isBusyFetching.value = false;
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
	emit('room-selected', roomId);
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
.filter {
	.v-input {
		min-width: 150px;
		flex: 2;

		&.v-select {
			flex: 1;
		}
	}
}
.room-item {
	cursor: pointer;
	transition: background-color 0.2s ease-in-out;

	&:hover {
		background-color: $grey-lighten-2;
	}

	hr {
		border: none;
		border-bottom: solid 1px $grey-lighten-2;
	}

	.title {
		color: $black;
		font-size: size(18);
		margin-bottom: 4px;
	}
	.properties {
		font-size: size(14);
		color: $grey-darken-2;
	}
	&.focused {
		background-color: $grey-lighten-3;
	}
}
</style>
