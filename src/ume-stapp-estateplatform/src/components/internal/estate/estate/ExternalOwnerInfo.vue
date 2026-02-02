<template>
	<v-alert class="external-owner-info mx-6" rounded="lg">
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
					<span v-if="noteTokens.length">
						<template v-for="(t, i) in noteTokens" :key="i">
							<span v-if="t.type === 'text'">{{ t.value }}</span>
							<a
								v-else
								:href="t.href"
								target="_blank"
								rel="noopener noreferrer"
							>
								{{ t.value }}
							</a>
						</template>
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
import { IExternalOwnerInfo } from '@/models/estate/Interfaces';
import { computed } from 'vue';
import { linkify } from './linkifyText';

const props = defineProps<{
	externalOwnerInfo: IExternalOwnerInfo;
}>();

const noteTokens = computed(() => {
	const note = props.externalOwnerInfo?.note ?? '';
	return note ? linkify(note) : [];
});
</script>

<style scoped lang="scss">
.external-owner-info {
	.prop .label {
		color: $grey-darken-2;
	}
}
</style>
