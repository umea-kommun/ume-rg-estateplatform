<template>
	<app-content
		:isLoading="isBusyLoadingFromServer"
		class="kvittens-template-edit"
		:pageTitle="$t('component.internal.kvittensTemplateEdit.title')"
	>
		<base-back-button />
		<vee-form
			v-if="!isBusyLoadingFromServer && template"
			ref="form"
			v-slot="{ meta, errors }"
		>
			<h1 class="my-3">
				{{ $t('component.internal.kvittensTemplateEdit.title') }}
			</h1>
			<v-alert
				v-if="!isCreatingNewTemplate"
				type="info"
				icon="lock"
				variant="tonal"
				class="mb-4"
			>
				{{
					$t(
						'component.internal.kvittensTemplateEdit.publishedAndLocked'
					)
				}}
			</v-alert>
			<base-validation-summary
				id="validation-summary"
				class="mb-4"
				:validation-errors="errors"
			/>
			<base-text-box
				id="title"
				:label="
					$t(
						'component.internal.kvittensTemplateEdit.field.titleLabel'
					)
				"
				:helpText="
					$t(
						'component.internal.kvittensTemplateEdit.field.titleHelpText'
					)
				"
				v-model="template.title"
				rules="required"
				:disabled="isDisabled"
			/>

			<base-text-box
				id="shortTitle"
				:label="
					$t(
						'component.internal.kvittensTemplateEdit.field.shortTitleLabel'
					)
				"
				:helpText="
					$t(
						'component.internal.kvittensTemplateEdit.field.shortTitleHelpText'
					)
				"
				v-model="template.shortTitle"
				rules="required|max:24"
				:disabled="isDisabled"
			/>

			<v-divider class="my-4" />

			<base-text-box
				id="confirmText"
				:label="
					$t(
						'component.internal.kvittensTemplateEdit.field.confirmTextLabel'
					)
				"
				:helpText="
					$t(
						'component.internal.kvittensTemplateEdit.field.confirmTextHelpText'
					)
				"
				v-model="template.confirmText"
				rules="required"
				:disabled="isDisabled"
			/>

			<v-divider class="my-4" />
			<kvittens-template-target-select
				:label="
					$t(
						'component.internal.kvittensTemplateEdit.field.schoolFormAndYearLabel'
					)
				"
				:template="template"
				v-model="template.targets"
				required
				:disabled="isDisabled"
			/>

			<v-divider class="my-4" />
			<base-html-editor
				id="content"
				name="content_ifr"
				:label="
					$t(
						'component.internal.kvittensTemplateEdit.field.textLabel'
					)
				"
				v-model="template.text"
				rules="required"
				:disabled="isDisabled"
			/>

			<base-html-editor
				id="gdprText"
				name="gdprText_ifr"
				:label="
					$t(
						'component.internal.kvittensTemplateEdit.field.gdprTextLabel'
					)
				"
				v-model="template.gdprText"
				rules="required"
				:disabled="isDisabled"
			/>
			<div class="d-flex justify-center">
				<v-btn
					class="mt-4"
					color="primary"
					size="large"
					prepend-icon="save"
					:disabled="isCreatingTemplate || !meta.valid || isDisabled"
					:loading="isCreatingTemplate"
					@click="createTemplate"
				>
					{{
						$t('component.internal.kvittensTemplateEdit.saveButton')
					}}
				</v-btn>
			</div>
		</vee-form>
	</app-content>
</template>
<script setup lang="ts">
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import BaseHtmlEditor from '@/components/base/BaseHtmlEditor.vue';
import BaseTextBox from '@/components/base/BaseTextBox.vue';
import { IRootState } from '@/models/Interfaces';
import {
	ICreateKvittensTemplate,
	IKvittensTemplate,
} from '@/models/kvittens/Interfaces';
import { computed, onMounted, ref, useTemplateRef } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useStore } from 'vuex';
import { Form as VeeForm } from 'vee-validate';
import KvittensTemplateTargetSelect from './targetSelect/KvittensTemplateTargetSelect.vue';
import BaseValidationSummary from '@/components/base/BaseValidationSummary.vue';
import { MyPagesRoutes } from '@/router/routes.js';
import { useTConfirmDialog } from '@turkos/components';
import { useI18n } from 'vue-i18n';

const store = useStore<IRootState>();
const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const { tConfirmDialogAsync } = useTConfirmDialog();

const isBusyLoadingFromServer = ref(false);
const template = ref<IKvittensTemplate | ICreateKvittensTemplate | null>(null);
const form = useTemplateRef('form');

const isCreatingNewTemplate = computed(() => route.params.id === 'new');
const isDisabled = computed(
	() => isBusyLoadingFromServer.value || !isCreatingNewTemplate.value
);

const isCreatingTemplate = ref(false);
const createTemplate = async () => {
	const isValid = (await form.value?.validate())?.valid ?? false;
	if (!isValid) {
		return;
	}

	const doPublish = await tConfirmDialogAsync(
		t('component.internal.kvittensTemplateEdit.publishConfirm.title'),
		t('component.internal.kvittensTemplateEdit.publishConfirm.text'),
		{
			text: t(
				'component.internal.kvittensTemplateEdit.publishConfirm.yes'
			),
		}
	);
	if (!doPublish) {
		return;
	}

	isCreatingTemplate.value = true;
	try {
		const createdTemplate = await store.dispatch(
			'createKvittensTemplate',
			template.value
		);
		template.value = createdTemplate;
		router.replace({
			name: MyPagesRoutes.InternalKvittensTemplateEdit,
			params: { id: createdTemplate.id },
		});
	} finally {
		isCreatingTemplate.value = false;
	}
};

const loadTemplate = async () => {
	isBusyLoadingFromServer.value = true;
	try {
		template.value = await store.dispatch('getKvittensTemplate', {
			templateId: route.params.id as string,
		});
	} finally {
		isBusyLoadingFromServer.value = false;
	}
};

const initNewTemplate = () => {
	template.value = {
		title: '',
		shortTitle: '',
		text: '',
		confirmText: '',
		gdprText: '',
		targets: [],
	};
};

onMounted(() => {
	isCreatingNewTemplate.value ? initNewTemplate() : loadTemplate();
});
</script>
<style lang="scss" scoped>
.kvittens-template-edit {
	&.app-content {
		:deep(.v-container) {
			padding-top: calc($site-content-vertical-padding - 20px);
		}
	}
}
</style>
