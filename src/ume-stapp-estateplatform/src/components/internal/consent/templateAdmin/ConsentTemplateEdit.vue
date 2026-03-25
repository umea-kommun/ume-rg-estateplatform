<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="consent-template-edit"
		:pageTitle="pageTitle"
	>
		<base-back-button />
		<div v-if="!isBusyLoadingFromServer && template">
			<vee-form v-slot="{ errors }" :ref="(e) => (validator = e)">
				<v-row>
					<v-col class="pa-0">
						<h1>
							{{ pageTitle }}
						</h1>
					</v-col>
				</v-row>
				<v-row>
					<v-col>
						<base-validation-summary
							id="validation-summary"
							:validation-errors="errors"
						/>
					</v-col>
				</v-row>
				<v-row>
					<v-col>
						<base-text-box
							id="title"
							:label="
								$t(
									'component.internal.consentTemplateEdit.field.title.label'
								)
							"
							v-model="title"
							:error-message="
								showTitleRequiredError && !title
									? $t(
											'component.internal.consentTemplateEdit.field.title.requiredToSave'
									  )
									: ''
							"
							rules="required"
							:disabled="isPublished"
						/>
					</v-col>
				</v-row>
				<v-row>
					<v-col>
						<base-html-editor
							id="content"
							name="content_ifr"
							:label="
								$t(
									'component.internal.consentTemplateEdit.field.text.label'
								)
							"
							v-model="content"
							rules="required"
							:disabled="isPublished"
						/>
					</v-col>
				</v-row>
				<hr class="mb-4 mt-4" />
				<v-row>
					<v-col>
						<template-group-select
							id="group-select"
							:disabled="isPublished"
							v-model="selectedGroups"
							:label="
								$t(
									'component.internal.consentTemplateEdit.field.groups.label'
								)
							"
							:add-label="
								$t(
									'component.internal.templateGroupSelect.modal.title'
								)
							"
							rules="required"
							add-button-icon="add"
							add-button-variant="text"
						>
							<template v-slot:empty>
								<span class="text-grey-darken-2">
									{{
										$t(
											'component.internal.consentTemplateEdit.field.groups.empty'
										)
									}}
								</span>
							</template>
						</template-group-select>
					</v-col>
				</v-row>
				<hr class="mb-4 mt-4" />
				<v-row>
					<v-col>
						<base-date-picker
							id="start-date"
							:label="
								$t(
									'component.internal.consentTemplateEdit.field.startDate.label'
								)
							"
							v-model="publishedDate"
							:rules="
								'required' +
								(expireDate ? '|maxDate:' + expireDate : '')
							"
							max-date="9999-12-31"
							:disabled="isPublished"
						/>
					</v-col>
					<v-col>
						<base-date-picker
							id="end-date"
							:label="
								$t(
									'component.internal.consentTemplateEdit.field.endDate.label'
								)
							"
							v-model="expireDate"
							:rules="
								(publishedDate
									? '|minDate:' + publishedDate
									: '') +
								(!templateIsOngoing ? '|required' : '')
							"
							max-date="9999-12-31"
							:disabled="isPublished || templateIsOngoing"
						/>
						<v-checkbox
							:label="
								$t(
									'component.internal.consentTemplateEdit.field.isOngoing.label'
								)
							"
							color="primary"
							v-model="templateIsOngoing"
							:disabled="isPublished"
						/>
					</v-col>
				</v-row>
				<hr class="mb-4 mt-4" />
				<v-row v-if="!isPublished">
					<v-col v-if="!isBusySaving" class="save-buttons-wrap">
						<v-btn
							:disabled="!hasUnsavedChanges"
							class="ml-0"
							variant="outlined"
							size="large"
							@click="saveTemplate(false)"
							>{{
								$t(
									'component.internal.consentTemplateEdit.saveDraft'
								)
							}}</v-btn
						>
						<v-btn
							class="mr-0"
							variant="flat"
							color="primary"
							size="large"
							@click="saveTemplate(true)"
							>{{
								$t(
									'component.internal.consentTemplateEdit.publish'
								)
							}}</v-btn
						>
					</v-col>
					<v-col v-if="isBusySaving" class="save-buttons-wrap">
						<app-loading-spinner
							:isVisible="isBusySaving"
						></app-loading-spinner>
					</v-col>
				</v-row>
			</vee-form>
		</div>
	</app-content>
</template>
<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import AppContent from '@/components/app/AppContent.vue';
import { useRoute, useRouter } from 'vue-router';
import {
	AppContentSize,
	ConsentTemplateGuid,
	DispatchType,
	MutationType,
	ConsentTemplateStatus,
	TemplateConnectionType,
} from '@/models/Enums';
import { useStore } from 'vuex';
import {
	IRootState,
	IConsentTemplateGroup,
	ITemplateConnection,
} from '@/models/Interfaces';
import BaseTextBox from '@/components/base/BaseTextBox.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import BaseHtmlEditor from '@/components/base/BaseHtmlEditor.vue';
import BaseDatePicker from '@/components/base/BaseDatePicker.vue';
import BaseValidationSummary from '@/components/base/BaseValidationSummary.vue';
import AppLoadingSpinner from '@/components/app/AppLoadingSpinner.vue';
import { useI18n } from 'vue-i18n';
import { useTConfirmDialog } from '@turkos/components';
import TemplateGroupSelect from './TemplateGroupSelect.vue';
import { Form as VeeForm } from 'vee-validate';
import moment from 'moment';

const router = useRouter();
const route = useRoute();
const store = useStore<IRootState>();
const { t } = useI18n();

const contentSize = ref(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const props = defineProps({
	templateGuid: {
		type: String,
		required: true,
	},
});

const isBusyLoadingFromServer = ref<boolean>(true);
const isBusySaving = ref(false);
const originalTemplate = ref('');

const template = computed(() => {
	return store.state.consentTemplate;
});

const pageTitle = computed(() => {
	if (template.value?.guid) {
		if (template.value.status === ConsentTemplateStatus.Draft) {
			return t('component.internal.consentTemplateEdit.titleEdit');
		} else {
			return t('component.internal.consentTemplateEdit.titleView');
		}
	}
	return t('component.internal.consentTemplateEdit.titleCreate');
});

const updateTemplate = (prop: string, value: unknown): void => {
	store.commit(MutationType.UpdateConsentTemplate, { prop, value });
};

const title = computed({
	get: () => template.value?.title ?? '',
	set: (value: string) => {
		updateTemplate('title', value);
	},
});

const content = computed({
	get: () => template.value?.content ?? '',
	set: (value: string) => {
		updateTemplate('content', value);
	},
});

const selectedGroups = computed({
	get: () => {
		const groups = [] as IConsentTemplateGroup[];
		if (template.value?.templateConnections) {
			template.value?.templateConnections.forEach(
				(item: {
					refId: string;
					name: string;
					type: TemplateConnectionType;
				}) => {
					groups.push({
						refId: item.refId,
						title: item.name,
						type: item.type,
					});
				}
			);
			return groups;
		}
		return [];
	},
	set: (newGroups: IConsentTemplateGroup[]) => {
		const templateConnections: ITemplateConnection[] = [];
		newGroups.forEach((element) => {
			if (!templateConnections.find((f) => f.refId === element.refId)) {
				templateConnections.push({
					name: element.title,
					refId: element.refId,
					type: TemplateConnectionType[
						element.type as keyof typeof TemplateConnectionType
					],
				} as ITemplateConnection);
			}
		});
		updateTemplate('templateConnections', templateConnections);
	},
});

const publishedDate = computed({
	get: () => {
		if (template.value?.publishedDate) {
			return moment(template.value.publishedDate).format('YYYY-MM-DD');
		}
		return '';
	},
	set: (value: string) => {
		updateTemplate('publishedDate', value || null);
	},
});

const expireDate = computed({
	get: () => {
		if (template.value?.expireDate) {
			return moment(template.value.expireDate).format('YYYY-MM-DD');
		}
		return '';
	},
	set: (value: string) => {
		updateTemplate('expireDate', value || null);
	},
});

const templateIsOngoing = ref(false);
watch(
	() => templateIsOngoing.value,
	(newValue) => {
		if (newValue) {
			expireDate.value = '';
		}
	}
);

const isPublished = computed(
	() => template.value?.status === ConsentTemplateStatus.Published
);

const hasUnsavedChanges = computed(() => {
	return JSON.stringify(template.value) !== originalTemplate.value;
});

const showTitleRequiredError = ref(false);
const validator = ref();
async function isValidTemplate(): Promise<boolean> {
	const validatorResult = await validator.value.validate();
	const isValid = validatorResult.valid;
	return isValid;
}

const { tConfirmDialogAsync } = useTConfirmDialog();
const saveTemplate = async (publish: boolean): Promise<void> => {
	if (!isPublished.value) {
		showTitleRequiredError.value = false;
		if (publish) {
			const validTemplate = await isValidTemplate();
			if (!validTemplate) {
				document
					.getElementById('validation-summary')
					?.scrollIntoView({ behavior: 'smooth', block: 'center' });
				return;
			}

			const doPublish = await tConfirmDialogAsync(
				t(
					'component.internal.consentTemplateEdit.publishConfirm.title'
				),
				t('component.internal.consentTemplateEdit.publishConfirm.text'),
				{
					text: t(
						'component.internal.consentTemplateEdit.publishConfirm.yes'
					),
				}
			);
			if (!doPublish) {
				return;
			}
		} else {
			if (!template.value?.title) {
				showTitleRequiredError.value = true;
				const titleField = document.getElementById('title');
				titleField?.focus();
				return;
			}
		}

		isBusySaving.value = true;
		await store.dispatch(DispatchType.SaveConsentTemplate, {
			template: template.value,
			publish,
		});
		if (
			props.templateGuid === ConsentTemplateGuid.New &&
			store.state.consentTemplate?.guid
		) {
			router.replace({
				params: { templateGuid: store.state.consentTemplate?.guid },
			});
		}
		originalTemplate.value = JSON.stringify(template.value);
		isBusySaving.value = false;
	}
};

onMounted(async () => {
	isBusyLoadingFromServer.value = true;
	if (props.templateGuid === ConsentTemplateGuid.New) {
		// Creating new template
		store.commit(MutationType.NewConsentTemplate);
	} else {
		// Fetching an existing template
		await store.dispatch(DispatchType.GetConsentTemplate, {
			guid: props.templateGuid,
		});
	}
	if (store.state.consentTemplate) {
		originalTemplate.value = JSON.stringify(store.state.consentTemplate);
	}
	templateIsOngoing.value = !expireDate.value;
	isBusyLoadingFromServer.value = false;
});
</script>
<style scoped lang="scss">
.consent-template-edit {
	.v-row {
		gap: 24px;
		.v-col {
			padding: 0;
		}
	}
	hr {
		border: solid 1px $grey-lighten-3;
	}

	.v-checkbox {
		margin-left: -12px;
		:deep(.v-selection-control) {
			min-height: 24px;
		}
	}

	.v-btn:not(.back-btn) {
		text-transform: none;
		letter-spacing: normal;
	}

	.save-buttons-wrap {
		display: flex;
		justify-content: flex-end;

		:deep(.app-loading-spinner) {
			margin: 12px auto 12px;
		}
	}
	&.app-content {
		:deep(.v-container) {
			padding-top: calc($site-content-vertical-padding - 20px);
		}
	}
}
</style>
