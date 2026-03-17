<template>
	<div class="favorite-list">
		<v-skeleton-loader
			v-if="isFetchingFavorites"
			class="mb-4 pa-0 loader-lazy"
			type="subtitle,article"
		/>
		<div v-else>
			<slot name="header" :count="filteredFavorites?.length ?? 0">
				<h2 class="mb-4">
					{{
						$t('component.internal.estateFavorite.title', {
							count: filteredFavorites?.length ?? 0,
						})
					}}
				</h2>
			</slot>
			<p
				v-if="!isFetchingFavorites && !filteredFavorites?.length"
				class="text-medium-emphasis"
			>
				{{ $t('component.internal.estateFavorite.noFavorites') }}
			</p>

			<div v-if="selectable">
				<estate-search-result-item
					v-for="entry in filteredFavorites ?? []"
					:key="entry.type + entry.id"
					:entry="entry"
					:to="undefined"
					:loading="
						(isBusyFetchingBuildingId === entry.id &&
							entry.type === EstateType.Building) ||
						(isBusyFetchingRoomId === entry.id &&
							entry.type === EstateType.Room)
					"
					@click="select(entry)"
					class="mb-4 pl-0"
				/>
			</div>
			<div v-else>
				<estate-search-result-item
					v-for="entry in filteredFavorites ?? []"
					:key="entry.type + entry.id"
					:entry="entry"
					class="mb-4 pl-0"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import { IRootState } from '@/models/Interfaces';
import { computed, onMounted, ref } from 'vue';
import { useStore } from 'vuex';
import EstateSearchResultItem from '../search/EstateSearchResultItem.vue';
import {
	IBuildingDetails,
	IEstateSearchResultEntry,
} from '@/models/estate/Interfaces';
import { EstateType } from '@/models/estate/Enums';

const props = defineProps<{
	selectable?: boolean;
	types?: EstateType[];
}>();

const emit = defineEmits<{
	(e: 'select-building', building: IBuildingDetails): void;
	(
		e: 'select-room',
		payload: { room: IBuildingDetails; building: IBuildingDetails }
	): void;
}>();

const store = useStore<IRootState>();

const isBusyFetchingBuildingId = ref<number | null>(null);
const selectBuilding = async (buildingId: number) => {
	isBusyFetchingBuildingId.value = buildingId;
	try {
		const building = await store.dispatch(DispatchType.GetBuildingById, {
			buildingId: buildingId,
		});
		emit('select-building', building);
	} finally {
		isBusyFetchingBuildingId.value = null;
	}
};
const isBusyFetchingRoomId = ref<number | null>(null);
const selectRoom = async (roomId: number) => {
	isBusyFetchingRoomId.value = roomId;
	try {
		const room = await store.dispatch(DispatchType.GetRoomById, {
			roomId: roomId,
		});
		const building = await store.dispatch(DispatchType.GetBuildingById, {
			buildingId: room.buildingId,
		});
		emit('select-room', { room, building });
	} finally {
		isBusyFetchingRoomId.value = null;
	}
};

const select = (entry: IEstateSearchResultEntry) => {
	switch (entry.type) {
		case EstateType.Building:
			selectBuilding(entry.id);
			break;
		case EstateType.Room:
			selectRoom(entry.id);
			break;
	}
};

const favorites = ref<IEstateSearchResultEntry[] | null>(null);

const filteredFavorites = computed(() => {
	if (props.types) {
		return favorites.value?.filter(
			(fav) => props.types?.includes(fav.type)
		);
	}
	return favorites.value;
});

const isFetchingFavorites = ref(false);
const fetchFavorites = async () => {
	if (isFetchingFavorites.value) {
		return;
	}

	isFetchingFavorites.value = true;
	try {
		favorites.value = await store.dispatch(DispatchType.GetFavorites);
	} finally {
		isFetchingFavorites.value = false;
	}
};

onMounted(() => {
	fetchFavorites();
});
</script>
