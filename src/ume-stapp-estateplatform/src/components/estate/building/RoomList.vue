<template>
	<div class="room-list">
		<div class="rooms" ref="list">
			<div
				v-if="!showingAllRooms"
				class="show-more-wrap"
				@click="showMoreRooms"
			>
				<div class="buttons">
					<v-btn
						class="more-btn"
						@click.stop="showMoreRooms"
						prepend-icon="expand_more"
					>
						{{ $t('component.buildingDetails.room.showMoreRooms') }}
					</v-btn>
					<v-btn
						v-if="canShowFewer"
						class="fewer-btn"
						@click.stop="showFewerRooms"
						prepend-icon="expand_less"
					>
						{{
							$t('component.buildingDetails.room.showFewerRooms')
						}}
					</v-btn>
				</div>
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
		<div v-if="showingAllRooms && canShowFewer" class="show-fewer-wrap">
			<v-btn
				class="fewer-btn"
				@click="showFewerRooms"
				prepend-icon="expand_less"
			>
				{{ $t('component.buildingDetails.room.showFewerRooms') }}
			</v-btn>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { IBuildingRoom } from '@/models/Interfaces';
import RoomCard from './RoomCard.vue';
import {
	ref,
	computed,
	nextTick,
	onBeforeUnmount,
	useTemplateRef,
	watch,
} from 'vue';

const props = defineProps<{
	rooms: IBuildingRoom[];
	focusedRoomId?: number | null;
	noPadding?: boolean;
}>();

const emit = defineEmits(['room-click']);

const INITIAL_ROOMS_SHOWING = 5;
const ROOMS_SHOWING_INCREMENT = 10;

const roomsShowing = ref(INITIAL_ROOMS_SHOWING);
const settledRoomsShowing = ref(INITIAL_ROOMS_SHOWING);

const showingAllRooms = computed(
	() => settledRoomsShowing.value >= props.rooms.length
);

const listEl = useTemplateRef<HTMLElement>('list');

// Collapsing takes the button away before the list moves, unlike expanding,
// where it appears once the list has landed
const isCollapsing = ref(false);

const canShowFewer = computed(
	() =>
		settledRoomsShowing.value > INITIAL_ROOMS_SHOWING && !isCollapsing.value
);

const EXPAND_DURATION_MS = 250;

let animationToken = 0;
let animationTimer: ReturnType<typeof setTimeout> | null = null;

const stopAnimation = (el: HTMLElement | null) => {
	animationToken++;

	if (animationTimer !== null) {
		clearTimeout(animationTimer);
		animationTimer = null;
	}

	if (el) {
		el.ontransitionend = null;
		el.style.removeProperty('height');
		el.style.removeProperty('overflow');
		el.style.removeProperty('transition');
	}
};

const settle = (count: number) => {
	settledRoomsShowing.value = count;
	isCollapsing.value = false;
};

// Animates the list between the old and the new height so the buttons glide
// into place instead of jumping
const setRoomsShowing = async (count: number) => {
	const el = listEl.value;
	const from = el?.offsetHeight ?? 0;
	const scrollY = window.scrollY;
	const isShrinking = count < roomsShowing.value;

	stopAnimation(el);
	const token = animationToken;
	roomsShowing.value = count;

	if (!el) {
		settle(count);
		return;
	}

	await nextTick();

	if (token !== animationToken) {
		return;
	}

	const to = el.offsetHeight;

	if (to === from) {
		settle(count);
		return;
	}

	el.style.overflow = 'hidden';
	el.style.transition = 'none';
	el.style.height = `${from}px`;
	void el.offsetHeight; // force reflow
	el.style.transition = `height ${EXPAND_DURATION_MS}ms ease`;
	el.style.height = `${to}px`;

	// Measuring and pinning the height changes the page height for a moment,
	// which the browser can answer by moving the scroll position
	if (window.scrollY !== scrollY) {
		window.scrollTo({ top: scrollY });
	}

	const followListBottom = () => {
		if (token !== animationToken) {
			return;
		}
		window.scrollTo({
			top: Math.max(0, scrollY - (from - el.offsetHeight)),
		});
		requestAnimationFrame(followListBottom);
	};

	if (isShrinking) {
		requestAnimationFrame(followListBottom);
	}

	const finish = () => {
		if (token !== animationToken) {
			return;
		}
		stopAnimation(el);
		settle(count);
	};

	el.ontransitionend = (event) => {
		if (event.target === el && event.propertyName === 'height') {
			finish();
		}
	};
	// transitionend never fires if the transition is interrupted or disabled
	animationTimer = setTimeout(finish, EXPAND_DURATION_MS + 50);
};

const showMoreRooms = () =>
	setRoomsShowing(roomsShowing.value + ROOMS_SHOWING_INCREMENT);

const showFewerRooms = () => {
	isCollapsing.value = true;
	return setRoomsShowing(INITIAL_ROOMS_SHOWING);
};

watch(
	() => props.rooms,
	() => {
		stopAnimation(listEl.value);
		roomsShowing.value = INITIAL_ROOMS_SHOWING;
		settle(INITIAL_ROOMS_SHOWING);
	}
);

onBeforeUnmount(() => stopAnimation(listEl.value));
</script>

<style lang="scss" scoped>
.room-list {
	.rooms {
		position: relative;
		// The list grows and shrinks under the reader, so the browser must not
		// try to keep the content below it in place
		overflow-anchor: none;
	}

	.show-more-wrap {
		position: absolute;
		left: 0;
		right: 0;
		margin-left: -10px;
		margin-right: -10px;
		margin-bottom: -5px;
		bottom: 0;
		z-index: 1;
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

		cursor: pointer;

		.buttons {
			display: flex;
			flex-wrap: wrap;
			justify-content: center;
			gap: 8px;
		}

		// Only the "show more" button reacts, the band is its hit area - except
		// over the "show fewer" button, which is its own target
		&:hover:not(:has(.fewer-btn:hover)) .more-btn :deep(.v-btn__overlay) {
			opacity: calc(
				var(--v-hover-opacity) * var(--v-theme-overlay-multiplier)
			);
		}
		&:active:not(:has(.fewer-btn:active)) .more-btn :deep(.v-btn__overlay) {
			opacity: calc(
				var(--v-pressed-opacity) * var(--v-theme-overlay-multiplier)
			);
		}
	}

	.show-fewer-wrap {
		display: flex;
		justify-content: center;
		padding-top: 8px;
	}
}
</style>
