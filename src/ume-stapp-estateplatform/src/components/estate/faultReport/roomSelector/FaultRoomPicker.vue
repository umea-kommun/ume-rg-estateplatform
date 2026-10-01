<template>
	<div class="fault-room-picker">
		<div class="method-choice">
			<p class="method-choice__lead text-medium-emphasis">
				{{ t('component.roomSelector.methodLead') }}
			</p>

			<button
				v-if="building.blueprintAvailable"
				type="button"
				class="method-option"
				@click="openBlueprint"
			>
				<span class="method-option__icon">
					<v-icon icon="map" :size="22" />
				</span>
				<span class="method-option__text">
					<span class="method-option__title">
						{{ t('component.roomSelector.methodBlueprint') }}
					</span>
					<span class="method-option__desc">
						{{ t('component.roomSelector.methodBlueprintDesc') }}
					</span>
				</span>
				<v-icon
					class="method-option__chevron"
					icon="chevron_right"
					:size="22"
				/>
			</button>

			<button type="button" class="method-option" @click="openList">
				<span class="method-option__icon">
					<v-icon icon="search" :size="22" />
				</span>
				<span class="method-option__text">
					<span class="method-option__title">
						{{ t('component.roomSelector.methodList') }}
					</span>
					<span class="method-option__desc">
						{{ t('component.roomSelector.methodListDesc') }}
					</span>
				</span>
				<v-icon
					class="method-option__chevron"
					icon="chevron_right"
					:size="22"
				/>
			</button>

			<!-- Skip is opt-out: flows where the room is genuinely optional (space
			requirement) hide it and offer their own way out. -->
			<template v-if="skippable">
				<div class="method-choice__divider" />

				<button
					type="button"
					class="method-option method-option--skip"
					@click="emit('skip')"
				>
					<span class="method-option__icon method-option__icon--grey">
						<v-icon icon="arrow_forward" :size="22" />
					</span>
					<span class="method-option__text">
						<span class="method-option__title">
							{{ t('component.faultReport.room.skipButton') }}
						</span>
						<span class="method-option__desc">
							{{ t('component.faultReport.room.skipHelp') }}
						</span>
					</span>
					<v-icon
						class="method-option__chevron"
						icon="chevron_right"
						:size="22"
					/>
				</button>
			</template>
		</div>

		<!-- The list path opens in its own dialog; it owns the room data, filters
		and virtualised list. -->
		<room-list-dialog
			v-model="showListDialog"
			:building="building"
			@select="onSelect"
		/>

		<!-- Blueprint machinery: always mounted (no visible trigger) so the choice
		row can open it via the exposed open(). -->
		<room-blueprint-selector
			ref="blueprintRef"
			hide-trigger
			:building="building"
			@room-selected="onSelect"
		/>
	</div>
</template>

<script setup lang="ts">
/**
 * Purpose-built room picker for the fault report flow. The user first chooses how
 * to find the room - from the list, on the blueprint, or skip. Each path is a
 * separate, self-contained piece; this component is just the choice screen and
 * the wiring that opens them and forwards the selected room.
 */
import { IBuildingDetails, IBuildingRoom } from '@/models/Interfaces';
import RoomBlueprintSelector from './RoomBlueprintSelector.vue';
import RoomListDialog from './RoomListDialog.vue';
import { ref, useTemplateRef } from 'vue';
import { useI18n } from 'vue-i18n';

withDefaults(
	defineProps<{
		building: IBuildingDetails;
		skippable?: boolean;
	}>(),
	{ skippable: true }
);

const emit = defineEmits<{
	select: [room: IBuildingRoom];
	skip: [];
}>();

const { t } = useI18n();

const showListDialog = ref(false);
const openList = () => (showListDialog.value = true);

const blueprintRef = useTemplateRef('blueprintRef');
const openBlueprint = () => blueprintRef.value?.open();

const onSelect = (room: IBuildingRoom) => {
	showListDialog.value = false;
	emit('select', room);
};
</script>

<style scoped lang="scss">
.fault-room-picker {
	// --- Choose-method screen ---------------------------------------------------
	.method-choice {
		&__lead {
			font-size: size(15);
			margin: 0 0 12px;
		}
		&__divider {
			border-top: solid 1px $grey-lighten-3;
			margin: 14px 0;
		}
	}

	.method-option {
		display: flex;
		align-items: center;
		gap: 14px;
		width: 100%;
		text-align: left;
		padding: 14px 16px;
		margin-bottom: 10px;
		background: transparent;
		border: solid 1px $grey-lighten-4;
		border-radius: $border-radius;
		cursor: pointer;
		transition:
			border-color 0.15s ease,
			background-color 0.15s ease;

		&:last-child {
			margin-bottom: 0;
		}

		&:hover,
		&:focus-visible {
			border-color: $primary;
			background: rgba($primary, 0.04);
			outline: none;
		}

		&__icon {
			flex: 0 0 auto;
			width: 40px;
			height: 40px;
			border-radius: 50%;
			display: grid;
			place-items: center;
			background: rgba($primary, 0.1);
			color: $primary;

			&--grey {
				background: rgba(0, 0, 0, 0.05);
				color: $grey-darken-1;
			}
		}
		&__text {
			flex: 1 1 auto;
			min-width: 0;
			display: flex;
			flex-direction: column;
			gap: 2px;
		}
		&__title {
			font-size: size(16);
			font-weight: 500;
			color: $black;
		}
		&__desc {
			font-size: size(14);
			color: $grey-darken-2;
		}
		&__chevron {
			flex: 0 0 auto;
			color: $grey-lighten-5;
		}
	}
}
</style>
