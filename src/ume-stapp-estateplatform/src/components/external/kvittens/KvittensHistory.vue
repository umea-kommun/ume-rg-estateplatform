<template>
	<div
		v-for="historyPost in sortedHistory"
		:key="historyPost.date + historyPost.name"
		class="kvittens-history pt-4 pb-4"
	>
		<div>
			<div>
				{{
					$t(
						'component.external.kvittensDetails.history.answeredYes',
						{
							name: historyPost.name,
						}
					)
				}}
				<span v-if="historyPost.agentName">{{
					$t(
						'component.external.kvittensDetails.history.throughAgent',
						{
							agentName: historyPost.agentName,
						}
					)
				}}</span>
			</div>
			<v-btn
				v-if="historyPost.agentName"
				class="ma-0 mt-2 regular-text"
				prepend-icon="image"
				variant="outlined"
				color="grey-darken-4"
				@click="openHistoryImage(historyPost)"
				>{{
					$t('component.external.kvittensDetails.history.openImage')
				}}</v-btn
			>
		</div>
		<div>
			{{
				moment
					.utc(historyPost.date)
					.local()
					.format('Do MMMM YYYY, HH:mm')
			}}
		</div>
	</div>
	<base-image-modal />
</template>

<script setup lang="ts">
import { IKvittensHistory } from '@/models/kvittens/Interfaces';
import { PropType, computed } from 'vue';
import moment from 'moment';
import Config from '@/Config';
import BaseImageModal from '@/components/base/baseImageModal/BaseImageModal.vue';
import { useBaseImageModal } from '@/components/base/baseImageModal/baseImageModal';
import { useI18n } from 'vue-i18n';

const props = defineProps({
	history: {
		type: Array as PropType<IKvittensHistory[]>,
		required: true,
	},
});

const { t } = useI18n();
const { showImageInModal } = useBaseImageModal();

const sortedHistory = computed(() => {
	return [...props.history].sort((a, b) => {
		return moment(b.date).local().diff(moment(a.date).local());
	});
});

const openHistoryImage = (history: IKvittensHistory) => {
	if (history.imageIdToken) {
		const fullImageUrl =
			Config.VUE_APP_CONSENT_BRIDGE_SERVICE_HISTORY_IMAGE +
			`?imageIdToken=${history.imageIdToken}`;

		showImageInModal(
			fullImageUrl,
			t('component.external.consent.imageOfGuardianSignature')
		);
	}
};
</script>

<style scoped lang="scss">
.kvittens-history {
	display: flex;
	justify-content: space-between;
	flex-wrap: wrap;

	border-bottom: solid 2px $grey-lighten-3;
	&:first-of-type {
		border-top: solid 2px $grey-lighten-3;
	}
}
</style>
