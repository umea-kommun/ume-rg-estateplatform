<template>
	<v-list-item
		:key="room.id"
		class="room-card pt-4"
		:data-room-id="room.id"
		:class="{ focused: room.id === focusedRoomId }"
	>
		<div class="title d-flex align-start justify-space-between">
			<span v-if="room.popularName">
				{{ room.popularName }} - {{ room.name }}
			</span>
			<span v-else>
				{{ room.name }}
			</span>
			<favorite-button
				:id="room.id"
				:type="EstateType.Room"
				:isFavorite="room.isFavorite"
				size="small"
			/>
		</div>
		<ul class="pa-0 ma-0">
			<li>
				{{ $t('component.buildingDetails.floorLabel') }}
				{{ room.floorName }}
			</li>
			<li>{{ room.grossArea?.toLocaleString() }} m²</li>
		</ul>
		<hr class="mt-4" />
	</v-list-item>
</template>

<script lang="ts" setup>
import { IBuildingRoom } from '@/models/Interfaces';
import FavoriteButton from '../favorite/FavoriteButton.vue';
import { EstateType } from '@/models/Enums';

defineProps<{
	room: IBuildingRoom;
	focusedRoomId?: number | null;
}>();
</script>

<style scoped lang="scss">
.room-card {
	transition: background-color 0.2s ease-in-out;

	hr {
		border: none;
		border-bottom: solid 1px $grey-lighten-2;
	}

	.title {
		color: $black;
		font-size: size(18);
		margin-bottom: 4px;
	}
	ul {
		font-size: size(14);
		color: $grey-darken-2;
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
	&.focused {
		background-color: rgb(73, 73, 255, 0.1);
		&:hover {
			background-color: rgb(73, 73, 255, 0.2);
		}
	}
}
</style>
