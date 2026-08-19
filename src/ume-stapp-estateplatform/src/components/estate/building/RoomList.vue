<template>
	<div class="room-list">
		<div v-if="!showingAllRooms" class="show-more-wrap">
			<v-btn @click="showMoreRooms" prepend-icon="add">
				{{
					$t('component.buildingDetails.room.showMoreRooms')
				}}
			</v-btn>
		</div>
		<v-list class="pa-0">
			<room-card
				v-for="room in rooms.slice(0, roomsShowing)"
				:key="room.id"
				:room="room"
				:focusedRoomId="focusedRoomId"
				:class="{
					'px-6': !noPadding,
				}"
				@click="emit('room-click', room.id)"
			/>
		</v-list>
	</div>
</template>

<script lang="ts" setup>
import { IBuildingRoom } from '@/models/Interfaces';
import RoomCard from './RoomCard.vue';
import { ref, computed, watch } from 'vue';

const props = defineProps<{
	rooms: IBuildingRoom[];
	focusedRoomId?: number | null;
	noPadding?: boolean;
}>();

const emit = defineEmits(['room-click']);

const INITIAL_ROOMS_SHOWING = 5;
const ROOMS_SHOWING_INCREMENT = 10;

const roomsShowing = ref(INITIAL_ROOMS_SHOWING);
const showingAllRooms = computed(
	() => roomsShowing.value >= (props.rooms?.length ?? 0)
);

const showMoreRooms = () => {
	roomsShowing.value = roomsShowing.value + ROOMS_SHOWING_INCREMENT;
};

watch(
	() => props.rooms,
	() => {
		roomsShowing.value = INITIAL_ROOMS_SHOWING;
	}
);
</script>

<style lang="scss" scoped>
.room-list {
	position: relative;

	.show-more-wrap {
		position: absolute;
		left: 0;
		right: 0;
		margin-left: -10px;
		margin-right: -10px;
		margin-bottom: -5px;
		bottom: 0;
		z-index: 100;
		height: 100px;
		background: linear-gradient(
			to bottom,
			rgba(#fff, 0) 0%,
			rgba(#fff, 1) 80%
		);

		display: flex;
		flex-direction: column;
		justify-content: center;
		align-items: center;

		> * {
			pointer-events: all;
		}
	}
}
</style>
