<template>
	<div
		class="option-card-grid pt-4"
		:class="{ 'option-card-grid--dense': dense }"
	>
		<v-card
			v-for="option in options"
			:key="option.value"
			class="option-card pa-4"
			:class="{ 'option-card--selected': selected === option.value }"
			rounded="lg"
			@click="emit('select', option.value)"
			@keydown.enter="emit('select', option.value)"
			tabindex="0"
		>
			<div class="d-flex justify-space-between align-start mb-4">
				<div class="icon-wrap">
					<v-icon :icon="option.icon" :size="28" />
				</div>

				<v-scale-transition>
					<v-icon
						v-if="selected === option.value"
						color="success"
						icon="check_circle"
						size="24"
					/>
				</v-scale-transition>
			</div>

			<div class="text-h6 font-weight-bold mb-2">
				{{ option.title }}
			</div>

			<div
				v-if="option.description"
				class="text-body-2 text-medium-emphasis mb-4"
			>
				{{ option.description }}
			</div>

			<div
				class="d-flex align-center justify-space-between mt-auto select-wrap"
			>
				<v-chip
					size="small"
					variant="tonal"
					color="success"
					v-if="selected === option.value"
				>
					{{ $t('component.internal.order.category.selected') }}
				</v-chip>

				<span
					v-else
					class="text-body-2 text-primary font-weight-medium"
				>
					{{ $t('component.internal.order.category.select') }}
				</span>
			</div>
		</v-card>
	</div>
</template>

<script setup lang="ts">
/**
 * Generic selectable tile grid shared by the estate work order flows
 * (order categories, fault indoor/outdoor, space requirement sub-categories).
 * Replaces the former OrderCategorySelector + FaultLocationSelector duplicates.
 */
export interface OptionCard {
	value: string;
	icon: string;
	title: string;
	description: string;
}

defineProps<{
	options: OptionCard[];
	selected: string | null;
	/**
	 * Compact variant: shorter content-height cards and a tighter grid so more
	 * fit per screen (used where tiles carry only an icon + title, e.g. the space
	 * requirement categories). Leaves the default order/fault layout unchanged.
	 */
	dense?: boolean;
}>();

const emit = defineEmits<{
	select: [value: string];
}>();
</script>

<style lang="scss" scoped>
.option-card-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
	gap: 1rem;

	.option-card {
		min-height: 220px;
		display: flex;
		flex-direction: column;
		border: 1px solid rgba(0, 0, 0, 0.08);

		&--selected {
			outline: 2px solid rgb(46, 125, 50);
			background: rgba(46, 125, 50, 0.05);

			background-color: rgba($primary, 0.1);
			color: $primary;
		}

		.icon-wrap {
			width: 52px;
			height: 52px;
			border-radius: 50%;
			display: grid;
			place-items: center;
			background: rgba(46, 125, 50, 0.08);
			color: rgb(46, 125, 50);
		}
		.select-wrap {
			height: 1rem;
		}
	}

	// Compact variant: on mobile only, lay each tile out as a short horizontal row
	// (icon left, title right) so many fit without scrolling. Selection is shown by
	// the card outline, so the check/select hint are hidden to keep rows tight.
	// Normal screens keep the default stacked layout (icon on top, title below).
	&.option-card-grid--dense {
		@media only screen and (max-width: 600px) {
			grid-template-columns: 1fr;
			gap: 0.5rem;

			.option-card {
				flex-direction: row;
				align-items: center;
				gap: 12px;
				min-height: 0;
				padding: 12px !important;

				// Icon row -> just the icon on the left; hide the selected check.
				.d-flex.mb-4 {
					margin-bottom: 0 !important;
					flex: 0 0 auto;

					> .v-icon {
						display: none;
					}
				}
				.icon-wrap {
					width: 40px;
					height: 40px;
				}
				// Title to the right of the icon.
				.text-h6 {
					flex: 1 1 auto;
					margin-bottom: 0 !important;
					font-size: 1rem !important;
					line-height: 1.3;
				}
				// Drop the description and the bottom select hint in the compact row.
				.text-body-2,
				.select-wrap {
					display: none;
				}
			}
		}
	}
}
</style>
