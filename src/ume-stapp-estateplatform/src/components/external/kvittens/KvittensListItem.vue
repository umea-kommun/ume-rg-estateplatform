<template>
	<v-card class="kvittens-list-item" :elevation="2">
		<div class="content flex-fill">
			<v-card-title>
				{{ kvittens.title }}
			</v-card-title>
			<v-card-text>
				<div v-if="userHasAnswered !== undefined">
					<kvittens-user-answer
						:user-has-answered="userHasAnswered"
						:status-of-self="true"
					/>
				</div>
				<div
					v-if="
						!kvittensIsAboutLoggedInUser ||
						kvittensForMultiplePersons
					"
					:title="
						$t('component.external.kvittensListItem.kvittensFor', {
							name: kvittens.personName,
						})
					"
				>
					<v-icon icon="person" />{{ kvittens.personName }}
				</div>
				<div v-if="kvittensNeedsMultipleAnswers">
					<v-tooltip
						location="bottom"
						theme="light"
						:open-on-click="true"
						:open-delay="200"
					>
						<template v-slot:activator="{ props }">
							<div v-bind="props">
								<v-icon icon="info_outline" />
								{{
									kvittensSentInAnswersCount ===
									kvittens.linkedPersons.length
										? $t(
												'component.external.kvittensListItem.allHaveAnswered'
										  )
										: $t(
												'component.external.kvittensListItem.XofYHasAnswered',
												{
													answered:
														kvittensSentInAnswersCount,
													total: kvittens
														.linkedPersons.length,
												}
										  )
								}}
							</div>
						</template>
						<table class="kvittens-list-item-linked-person-tooltip">
							<tr
								v-for="linkedPerson of kvittens.linkedPersons"
								:key="linkedPerson.socialSecurityNumber"
							>
								<td>{{ linkedPerson.name }}</td>
								<td>
									<kvittens-user-answer
										:user-has-answered="
											linkedPerson.userHasAnswered
										"
									/>
								</td>
							</tr>
						</table>
					</v-tooltip>
				</div>
			</v-card-text>
		</div>
		<div class="d-flex justify-end align-center pa-4 flex-1-1">
			<v-btn
				flat
				color="primary"
				:variant="userHasAnswered ? 'outlined' : undefined"
				:to="{
					name: MyPagesRoutes.KvittensDetails,
					params: {
						localId: kvittens.localId,
					},
				}"
				>{{
					userHasAnswered
						? $t('component.external.kvittensListItem.actionOpen')
						: $t('component.external.kvittensListItem.actionAnswer')
				}}</v-btn
			>
		</div>
	</v-card>
</template>

<script setup lang="ts">
import { IRootState } from '@/models/Interfaces';
import KvittensUserAnswer from './KvittensUserAnswer.vue';
import { IKvittens } from '@/models/kvittens/Interfaces';
import { PropType, computed } from 'vue';
import { useStore } from 'vuex';
import { MyPagesRoutes } from '@/router/routes';

const props = defineProps({
	kvittens: {
		required: true,
		type: Object as PropType<IKvittens>,
	},
	kvittensForMultiplePersons: {
		type: Boolean,
		default: true,
	},
});

const store = useStore<IRootState>();

const userHasAnswered = computed(
	() =>
		props.kvittens.linkedPersons.find(
			(linkedPerson) =>
				linkedPerson.socialSecurityNumber ===
				store.state.user.socialSecurityNumber
		)?.userHasAnswered
);
const kvittensIsAboutLoggedInUser = computed(
	() => store.state.user.socialSecurityNumber === props.kvittens.personSSNo
);
const kvittensNeedsMultipleAnswers = computed(
	() => props.kvittens.linkedPersons.length > 1
);
const kvittensSentInAnswersCount = computed(
	() =>
		props.kvittens.linkedPersons.filter(
			(linkedPerson) => linkedPerson.userHasAnswered
		).length
);
</script>

<style scoped lang="scss">
.kvittens-list-item {
	color: $black;
	display: flex;
	flex-wrap: wrap;

	.v-card-title {
		font-size: size(18);
		font-weight: bold;
		white-space: normal;
	}
	.content {
		max-width: 100%;
	}
	.v-card-text {
		display: flex;
		font-size: size(16);
		flex-wrap: wrap;
		gap: 16px;
		& > div {
			margin-right: 6px;
		}
		.v-icon {
			margin-right: 6px;
		}
	}
	.v-btn {
		font-size: size(16);
		height: auto;
		padding: 8px 26px;
		&.bg-primary {
			color: $white !important;
		}
	}
}
.kvittens-list-item-linked-person-tooltip {
	td {
		padding: 6px;
	}

	:deep(.kvittens-user-answer) {
		color: $white !important;
		background-color: transparent !important;
		border: none;
	}
}
</style>
