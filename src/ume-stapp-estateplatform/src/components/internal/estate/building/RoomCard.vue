<template>
	<div
		:key="room.id"
		class="room-card px-6 pt-4"
		:data-room-id="room.id"
		:class="{ focused: room.id === focusedRoomId }"
	>
		<div v-if="room.popularName" class="title">
			{{ room.popularName }} - {{ room.name }}
		</div>
		<div v-else class="title">{{ room.name }}</div>
		<ul>
			<li>
				{{ $t('component.internal.buildingDetails.floorLabel') }}
				{{ room.floorName }}
			</li>
			<li>{{ room.grossArea?.toLocaleString() }} m²</li>
		</ul>
		<hr class="mt-4" />
	</div>
</template>

<script lang="ts" setup>
import { IBuildingRoom } from '@/models/estate/Interfaces';

defineProps<{
	room: IBuildingRoom;
	focusedRoomId?: number | null;
}>();
</script>

<style scoped lang="scss">
.room-card {
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
