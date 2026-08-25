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
				? $t('component.estateFavorite.removeFavorite')
				: $t('component.estateFavorite.addFavorite')
		"
	/>
</template>

<script setup lang="ts">
import { EstateType } from '@/models/Enums';
import ErrorService from '@/utils/ErrorService';
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useFavorites } from './useFavorites';

const props = defineProps<{
	id: number;
	type: EstateType;
	isFavorite?: boolean;
	size?: 'small' | 'default';
}>();

const { t } = useI18n();
const favorites = useFavorites();

const isFavorite = computed(() =>
	favorites.isFavorite(props.type, props.id, props.isFavorite ?? false)
);

const isBusySettingFavorite = ref(false);
const toggleFavorite = async () => {
	if (isBusySettingFavorite.value) {
		return;
	}

	const shouldBeFavorite = !isFavorite.value;
	isBusySettingFavorite.value = true;
	try {
		await favorites.setFavorite(props.type, props.id, shouldBeFavorite);
	} catch (err) {
		ErrorService.onError({
			err,
			message: shouldBeFavorite
				? t('app.error.estate.unableToAddFavorite')
				: t('app.error.estate.unableToRemoveFavorite'),
		});
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
