<template>
	<v-btn
		class="ma-0"
		:class="{ small: size === 'small' }"
		:color="isFavorite ? 'secondary' : 'grey'"
		variant="text"
		:icon="isFavorite ? 'star' : 'star_border'"
		size="small"
		@click.stop.prevent="toggleFavorite"
		rounded="xl"
		:title="
			isFavorite
				? $t('component.internal.estateFavorite.removeFavorite')
				: $t('component.internal.estateFavorite.addFavorite')
		"
	/>
</template>

<script setup lang="ts">
import { DispatchType } from '@/models/Enums';
import { EstateType } from '@/models/estate/Enums';
import { IRootState } from '@/models/Interfaces';
import { ref } from 'vue';
import { useStore } from 'vuex';

const props = defineProps<{
	id: number;
	type: EstateType;
	isFavorite?: boolean;
	size?: 'small' | 'default';
}>();

const store = useStore<IRootState>();

const isFavorite = ref(props.isFavorite ?? false);

const isBusySettingFavorite = ref(false);
const toggleFavorite = async () => {
	if (isBusySettingFavorite.value) {
		return;
	}

	isBusySettingFavorite.value = true;
	try {
		await store.dispatch(
			isFavorite.value
				? DispatchType.UnsetFavorite
				: DispatchType.SetFavorite,
			{
				id: props.id,
				type: props.type,
			}
		);

		isFavorite.value = !isFavorite.value;
	} finally {
		isBusySettingFavorite.value = false;
	}
};
</script>

<style scoped lang="scss">
.v-btn {
	font-size: size(14);
	height: 34px;
	width: 34px;

	&.small {
		font-size: size(12);
		height: 28px;
		width: 28px;
	}
}
</style>
