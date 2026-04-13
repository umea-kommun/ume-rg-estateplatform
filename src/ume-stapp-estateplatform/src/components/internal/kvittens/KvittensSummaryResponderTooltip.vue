<template>
	<div>
		<div v-if="responders === null" class="my-1">
			{{ $t('component.internal.kvittensSummary.tooltip.noAnswers') }}
		</div>
		<div v-else>
			<div
				v-for="responder in responders"
				:key="responder?.name + responder?.dateOfBirth"
				class="d-flex justify-space-between align-center ga-2 my-1"
			>
				<div v-if="responder.hasAnswered">
					<v-icon icon="check" class="mr-2" />
					{{
						$t(
							'component.internal.kvittensSummary.tooltip.nameHasAnswered',
							{
								name: responder.name,
							}
						)
					}}
				</div>
				<div v-else>
					<v-icon icon="close" class="mr-2" />
					{{
						$t(
							'component.internal.kvittensSummary.tooltip.nameHasNotAnswered',
							{
								name: responder.name,
							}
						)
					}}
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { IKvittensSummaryResponder } from '@/models/kvittens/Interfaces';

defineProps<{
	title: string;
	responders: IKvittensSummaryResponder[] | null;
}>();
</script>
