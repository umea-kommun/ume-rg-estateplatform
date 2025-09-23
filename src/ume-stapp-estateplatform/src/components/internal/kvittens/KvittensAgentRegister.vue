<template>
	<h3>{{ t('component.internal.kvittensAgent.registerTitle') }}</h3>
	<p id="respodants-label">
		{{ t('component.internal.kvittensAgent.registerRespondentsLabel') }}
	</p>
	<v-list aria-labelledby="respodants-label">
		<v-list-item
			v-for="linkedPerson in linkedPersons"
			:key="linkedPerson.socialSecurityNumber"
			class="pl-0"
		>
			<v-checkbox-btn
				v-model="selectedRespondantsSsno"
				:id="`respondant-${linkedPerson.socialSecurityNumber}`"
				:value="linkedPerson.socialSecurityNumber"
				color="primary"
				:disabled="linkedPerson.userHasAnswered"
			>
			</v-checkbox-btn>
			<label
				class="name ml-2"
				:for="`respondant-${linkedPerson.socialSecurityNumber}`"
				>{{ linkedPerson.name }}
			</label>
			<div class="status ml-2">
				<kvittens-user-answer
					:user-has-answered="linkedPerson.userHasAnswered"
				/>
			</div>
		</v-list-item>
	</v-list>

	<div v-if="!allRespondantsHasAnswered">
		<base-form-field
			id="label-guardian-photo"
			labelFor="guardian-photo"
			:label="t('component.internal.kvittensAgent.fileUploadLabel')"
			:is-required="true"
			class="mt-6"
		>
			<v-file-input
				accept="image/*"
				variant="outlined"
				prepend-icon="camera_alt"
				v-model="respondantImage"
				color="primary"
			>
				<template v-slot:append-inner>
					<v-btn flat variant="tonal">{{
						t(
							'component.internal.kvittensAgent.fileUploadSelectImage'
						)
					}}</v-btn>
				</template>
			</v-file-input>
			<base-help-and-error-text
				:helpText="
					t('component.internal.kvittensAgent.fileUploadHelpText')
				"
			/>
		</base-form-field>
		<div class="center-btn-wrap">
			<v-btn
				:text="t('component.internal.kvittensAgent.registerButton')"
				color="primary"
				class="regular-text"
				size="large"
				:disabled="
					!selectedRespondantsSsno.length ||
					!respondantImage ||
					isBusySendingAnswer
				"
				@click="registerKvittens"
				:loading="isBusySendingAnswer"
			></v-btn>
		</div>
	</div>

	<Transition name="done">
		<v-alert
			v-if="allRespondantsHasAnswered"
			class="mt-6 mb-6"
			type="success"
		>
			{{
				t('component.internal.kvittensAgent.everyRespondentHasAnswered')
			}}
		</v-alert>
	</Transition>
</template>

<script setup lang="ts">
import { IKvittensLinkedPerson } from '@/models/kvittens/Interfaces';
import BaseFormField from '@/components/base/BaseFormField.vue';
import BaseHelpAndErrorText from '@/components/base/BaseHelpAndErrorText.vue';
import { computed, ref } from 'vue';

import { useI18n } from 'vue-i18n';
import KvittensUserAnswer from '@/components/external/kvittens/KvittensUserAnswer.vue';
import { compressImageFile } from '@/utils/imageUtils';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';
import { DispatchType } from '@/models/Enums';

const props = defineProps<{
	subjectSsno: string;
	templateId: string;
	linkedPersons: IKvittensLinkedPerson[];
}>();

const emit = defineEmits(['update:kvittens']);

const { t } = useI18n();
const store = useStore<IRootState>();

const selectedRespondantsSsno = ref<string[]>([]);
const respondantImage = ref<File | null>(null);
const isBusySendingAnswer = ref(false);

const allRespondantsHasAnswered = computed(() =>
	props.linkedPersons.every((p) => p.userHasAnswered)
);

const registerSingleKvittens = async (
	templateId: string,
	compressedImage: File
) => {
	const updatedKvittens = await store.dispatch(
		DispatchType.AgentAnswerKvittens,
		{
			templateId: templateId,
			subjectSsno: props.subjectSsno,
			respondents: selectedRespondantsSsno.value,
			image: compressedImage,
		}
	);
	if (updatedKvittens) {
		emit('update:kvittens', updatedKvittens);
	}
};

const registerKvittens = async () => {
	if (
		!respondantImage.value ||
		isBusySendingAnswer.value ||
		!selectedRespondantsSsno.value.length
	) {
		return;
	}
	isBusySendingAnswer.value = true;

	try {
		const compressedImage = await compressImageFile(respondantImage.value);
		await registerSingleKvittens(props.templateId, compressedImage);
		selectedRespondantsSsno.value = [];
		respondantImage.value = null;
	} catch (err) {
		ErrorService.onError({ err });
	} finally {
		isBusySendingAnswer.value = false;
	}
};
</script>

<style scoped lang="scss">
.v-list-item {
	border-bottom: solid 1px $grey-lighten-4;
	&:last-child {
		border-bottom: none;
	}

	:deep(.v-list-item__content) {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		word-break: break-word;

		.v-selection-control {
			flex: none;
		}
		.name {
			flex: 1;
		}
	}
}
.center-btn-wrap {
	text-align: center;
}

.done-enter-active,
.done-leave-active {
	transition: all 0.3s ease;
}
.done-enter-from,
.done-leave-to {
	opacity: 0;
	transform: scale(0.8);
}
</style>
