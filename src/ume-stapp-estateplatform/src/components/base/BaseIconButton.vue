<template>
	<v-btn class="base-icon-button regular-text ma-0" :class="{ active }" flat>
		<div class="icon-wrap d-flex justify-center align-center">
			<v-icon :icon="icon" />
		</div>
		<div class="text">{{ label }}</div>
	</v-btn>
</template>

<script setup lang="ts">
defineProps<{ label: string; icon: string; active?: boolean }>();
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
		width: 48px;
		height: 48px;
		background-color: #e6e6e6;
		border-radius: 50px;

		.v-icon {
			font-size: size(24);
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
