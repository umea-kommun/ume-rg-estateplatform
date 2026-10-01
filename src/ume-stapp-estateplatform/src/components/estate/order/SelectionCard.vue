<template>
	<div class="selection-card" :class="{ 'selection-card--muted': muted }">
		<div class="main">
			<div v-if="imageUrl || icon" class="media">
				<img
					v-if="imageUrl"
					:src="imageUrl"
					:alt="title"
					class="media-img"
				/>
				<div v-else class="icon-wrap">
					<v-icon :icon="icon" :size="24" />
				</div>
			</div>
			<div class="content">
				<div class="font-weight-medium">{{ title }}</div>
				<div
					v-if="description"
					class="text-body-2 text-medium-emphasis"
				>
					{{ description }}
				</div>
			</div>
		</div>
		<div v-if="$slots.actions" class="actions">
			<slot name="actions"></slot>
		</div>
	</div>
</template>

<script setup lang="ts">
/**
 * Compact summary card for a made choice in the work order flows - the collapsed
 * state of the building, indoor/outdoor and room steps. Shares one look so a
 * selected building, location, a skipped room and an outdoor "no room" all read
 * the same: media on the left, title + description, and right-aligned actions
 * (e.g. "Ändra", "Visa på karta").
 *
 * `imageUrl` shows a thumbnail in the media slot, falling back to `icon`. Both
 * are optional - omit them to render without a media slot at all, for a
 * selected value plain enough not to need one (e.g. a chosen room).
 * `muted` renders a grey icon for a step passed without a real choice (skipped
 * room, outdoor "no room"), matching its grey step marker.
 */
defineProps<{
	icon?: string;
	title: string;
	description?: string;
	muted?: boolean;
	imageUrl?: string | null;
}>();
</script>

<style lang="scss" scoped>
.selection-card {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 18px;
	border: solid 1px rgba(0, 0, 0, 0.2);
	border-radius: $border-radius;

	// Media + text always stay on one row together; only the actions may wrap
	// below on narrow screens, so building and room cards line up the same way.
	.main {
		display: flex;
		align-items: center;
		gap: 12px;
		flex: 1 1 auto;
		min-width: 0;
	}
	.media {
		flex: 0 0 auto;
	}
	.icon-wrap {
		width: 44px;
		height: 44px;
		border-radius: 50%;
		display: grid;
		place-items: center;
		background: rgba($primary, 0.08);
		color: $primary;
	}
	.media-img {
		width: 44px;
		height: 44px;
		border-radius: 50%;
		object-fit: cover;
		display: block;
	}
	.content {
		flex: 1 1 auto;
		min-width: 0;
	}
	.actions {
		flex: 0 0 auto;
		display: flex;
		align-items: center;
		gap: 8px;
	}

	&--muted .icon-wrap {
		background: rgba(0, 0, 0, 0.04);
		color: $grey-darken-1;
	}

	// Keep the action buttons usable on narrow screens by letting them drop
	// below the media + text instead of squeezing the title.
	@media only screen and (max-width: 480px) {
		flex-wrap: wrap;

		.actions {
			width: 100%;
			justify-content: flex-end;
		}
	}
}
</style>
