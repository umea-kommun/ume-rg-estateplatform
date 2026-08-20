<!-- Duplicated from ume-rg-myplatform @ 84b4a5dc
     src/ume-stapp-minasidor/src/components/base/BaseIconButton.vue -->
<template>
	<v-tooltip
		class="base-icon-button-wrap"
		:text="tooltip"
		location="bottom"
		:disabled="!tooltip"
		:open-delay="200"
	>
		<template v-slot:activator="{ props }">
			<div v-bind="props">
				<v-btn
					v-bind="$attrs"
					class="base-icon-button"
					:class="{ active }"
					:disabled="disabled"
					flat
				>
					<div class="icon-wrap d-flex justify-center align-center">
						<v-icon :icon="icon" :color="iconColor" />
						<div
							v-if="count !== undefined"
							class="count"
							:class="{
								zero: !count,
								'three-digit': count > 99,
							}"
						>
							{{ count }}
						</div>
						<div v-else-if="disabled" class="count zero">
							<v-icon icon="close" size="14" />
						</div>
					</div>
					<div class="text">{{ label }}</div>
				</v-btn>
			</div>
		</template>
	</v-tooltip>
</template>

<script setup lang="ts">
defineProps<{
	active?: boolean;
	count?: number;
	disabled?: boolean;
	icon: string;
	iconColor?: string;
	label: string;
	tooltip?: string;
}>();
</script>

<style scoped lang="scss">
.base-icon-button {
	height: auto;
	padding: 8px;
	color: $grey-darken-3;

	:deep(.v-btn__content) {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 8px;
		font-size: size(16);
	}
	.icon-wrap {
		position: relative;
		margin: 0 12px;
		width: 48px;
		height: 48px;
		background-color: #e6e6e6;
		border-radius: 50px;

		.v-icon {
			font-size: size(24);
		}
	}
	.count {
		position: absolute;
		display: flex;
		align-items: center;
		justify-content: center;
		margin-top: -90%;
		margin-right: -80%;

		font-size: size(12);

		border-radius: 50%;
		background-color: $grey-darken-2;
		background-color: $primary;
		font-weight: bold;
		color: #fff;
		width: size(22);
		height: size(22);

		&.zero {
			background-color: $primary;
			background-color: $grey-darken-2;
		}
		&.three-digit {
			font-size: size(10);
		}
	}
	&.v-btn--disabled {
		.count {
			background-color: $grey-lighten-6;
		}
	}

	/* States */
	&.active {
		color: #000;
		.icon-wrap {
			background-color: $primary;

			.v-icon {
				color: #fff;
			}
		}
	}
	&.v-btn--disabled {
		color: $grey-lighten-6;
		:deep(.v-btn__overlay) {
			display: none;
		}
		.icon-wrap {
			background-color: $grey-lighten-3;
		}
	}
}
</style>
