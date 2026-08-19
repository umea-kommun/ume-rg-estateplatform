<template>
	<v-alert
		class="external-owner-info mx-6"
		:class="{ transparent: transparent }"
		rounded="lg"
	>
		<div class="properties">
			<div class="prop">
				<div class="label">
					{{ $t('estateCommon.externalOwner.name') }}
				</div>
				<div class="value">
					<span v-if="externalOwnerInfo?.name">
						{{ externalOwnerInfo?.name }}
					</span>
					<span class="font-italic font-weight-light" v-else>
						{{
							$t(
								'component.internal.estateDetails.informationMissing'
							)
						}}
					</span>
				</div>
			</div>
			<div class="prop">
				<div class="label">
					{{ $t('estateCommon.externalOwner.note') }}
				</div>
				<div class="value">
					<span v-if="externalOwnerInfo?.note">
						<BaseAutoLinkText :text="externalOwnerInfo.note" />
					</span>
					<span class="font-italic font-weight-light" v-else>
						{{
							$t(
								'component.internal.estateDetails.informationMissing'
							)
						}}
					</span>
				</div>
			</div>
		</div>
	</v-alert>
</template>

<script setup lang="ts">
import { IExternalOwnerInfo } from '@/models/Interfaces';
import BaseAutoLinkText from '@/components/base/BaseAutoLinkText.vue';

defineProps<{
	externalOwnerInfo: IExternalOwnerInfo;
	transparent?: boolean;
}>();
</script>

<style scoped lang="scss">
.external-owner-info {
	.prop .label {
		color: $grey-darken-2;
	}

	&.transparent {
		background-color: transparent;

		:deep(.v-alert__underlay) {
			background-color: transparent;
		}
	}
}
</style>
