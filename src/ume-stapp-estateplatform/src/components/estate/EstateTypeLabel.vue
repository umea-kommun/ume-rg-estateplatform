<template>
	<span class="estate-type-label" :class="type">
		<v-icon :icon="icon" size="16" />
		{{ t(`estateCommon.type.${type}`) }}
	</span>
</template>

<script setup lang="ts">
import { EstateType } from '@/models/Enums';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	type: EstateType;
}>();

const { t } = useI18n();

// Same icons as the hero search suggestions (EstateStartSearch).
const icon = computed(() => {
	switch (props.type) {
		case EstateType.Estate:
			return 'home_work';
		case EstateType.Building:
			return 'apartment';
		case EstateType.Room:
			return 'meeting_room';
	}
	return 'place';
});
</script>

<style lang="scss" scoped>
.estate-type-label {
	display: inline-flex;
	flex-shrink: 0;
	align-items: center;
	gap: 6px;
	padding: 4px 10px;
	border-radius: 999px;
	font-size: size(14);
	white-space: nowrap;

	&.estate {
		background: rgba($info, 0.16);
		color: $info;
	}

	&.building {
		background: rgba($primary, 0.16);
		color: $primary;
	}

	&.room {
		background: rgba($accent, 0.4);
		color: $grey-darken-4;
	}
}
</style>
