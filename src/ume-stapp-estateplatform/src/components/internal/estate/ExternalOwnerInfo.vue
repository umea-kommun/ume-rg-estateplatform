<template>
	<v-alert class="external-owner-info mx-6" rounded="lg">
		<div class="properties">
			<div class="prop">
				<div class="label">
					{{ $t('estateCommon.externalOwner.name') }}
				</div>
				<div class="value">
					{{ externalOwnerInfo?.name }}
				</div>
			</div>
			<div class="prop">
				<div class="label">
					{{ $t('estateCommon.externalOwner.note') }}
				</div>
				<div class="value">
					{{ externalOwnerInfo?.note }}
				</div>
			</div>
		</div>
	</v-alert>
</template>

<script setup lang="ts">
import { IExternalOwnerInfo } from '@/models/estate/Interfaces';

defineProps<{
	externalOwnerInfo: IExternalOwnerInfo;
}>();
</script>

<style scoped lang="scss">
.external-owner-info {
	.prop .label {
		color: $grey-darken-2;
	}
}
</style>
