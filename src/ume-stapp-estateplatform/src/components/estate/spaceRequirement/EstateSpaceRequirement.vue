<template>
	<app-content
		class="estate-default estate-space-requirement"
		:pageTitle="
			$t('component.appHeader.title.internalEstateSpaceRequirement')
		"
		:is-loading="isLoadingFromQuery"
	>
		<div class="content-wrap">
			<div class="pb-4 order-wrap">
				<nav-breadcrumbs
					class="mb-2"
					:breadcrumbs="breadcrumbs"
					full-width
				/>
				<estate-space-requirement-completed v-if="hasSubmitted" />
				<vee-form
					v-else
					ref="formValidator"
					class="mt-2"
					v-slot="{ errors }"
					@submit.prevent="submitReport"
				>
					<div class="pb-4">
						<h1 class="ma-0 mb-2">
							{{
								$t('component.internal.spaceRequirement.title')
							}}
						</h1>
						<p class="ma-0">
							{{
								$t(
									'component.internal.spaceRequirement.description'
								)
							}}
						</p>
					</div>

					<!-- CATEGORY (the primary choice - shown first, no building gate) -->
					<estate-order-step
						id="space-requirement-category"
						:title="
							$t(
								'component.internal.spaceRequirement.category.title'
							)
						"
					>
						<div
							v-if="isLoadingCategories"
							class="d-flex align-center ga-3 py-4 text-medium-emphasis"
						>
							<v-progress-circular
								indeterminate
								size="20"
								width="2"
							/>
							{{
								$t(
									'component.internal.spaceRequirement.category.loading'
								)
							}}
						</div>
						<option-card-grid
							v-else-if="categoryCards.length > 0"
							:options="categoryCards"
							:selected="selectedCategoryValue"
							dense
							@select="selectCategory"
						/>
						<v-alert
							v-else
							type="info"
							variant="tonal"
							rounded="lg"
						>
							{{
								$t(
									'component.internal.spaceRequirement.category.noneAvailable'
								)
							}}
						</v-alert>
						<v-alert
							v-if="categoryMissing"
							type="error"
							variant="tonal"
							rounded="lg"
							class="mt-4"
						>
							{{
								$t(
									'component.internal.spaceRequirement.category.required'
								)
							}}
						</v-alert>
					</estate-order-step>

					<!-- DESCRIPTION -->
					<estate-order-step
						:title="
							$t(
								'component.internal.spaceRequirement.general.descriptionTitle'
							)
						"
						class="mt-6"
					>
						<base-text-box
							id="problem-description"
							:label="
								$t(
									'component.internal.spaceRequirement.general.descriptionLabel'
								)
							"
							v-model="problemDescription"
							rules="required"
							variant="outlined"
							rounded="lg"
							aria-labelledby="problem-description-label"
							text-area
							auto-grow
							:error-message="descriptionServerError"
						/>
						<p class="text-medium-emphasis">
							{{
								$t(
									'component.internal.spaceRequirement.general.descriptionHelpText'
								)
							}}
						</p>

						<base-file-upload
							id="file-upload"
							:label="
								$t(
									'component.internal.spaceRequirement.general.fileUploadLabel'
								)
							"
							v-model="attachments"
							:accept="uploadAccept"
							:max-files="uploadMaxFiles"
							:max-size-mega-bytes="uploadMaxSizeMb"
							:server-errors="fileServerErrors"
							class="mt-6"
						/>
					</estate-order-step>

					<!-- LOCATION: collapsed to a single button until the user opts in -->
					<div
						v-if="!showBuildingSelector && !selectedBuilding"
						class="mt-6"
					>
						<v-btn
							variant="tonal"
							rounded="lg"
							color="grey-darken-2"
							prepend-icon="add"
							@click="showBuildingSelector = true"
						>
							{{
								$t(
									'component.internal.spaceRequirement.building.add'
								)
							}}
						</v-btn>
					</div>
					<estate-order-step
						v-else
						id="space-requirement-building"
						:title="
							selectedBuilding
								? $t(
										'component.internal.faultReport.building.selected'
								  )
								: $t(
										'component.internal.spaceRequirement.building.select'
								  )
						"
						:show-clear="!!selectedBuilding"
						:show-skip="!selectedBuilding"
						@clear="changeBuilding"
						@skip="showBuildingSelector = false"
						class="mt-6"
					>
						<template #header-btn v-if="!selectedBuilding">
							<building-map-selector @select="selectBuilding" />
						</template>
						<building-selector
							:selected-building="selectedBuilding"
							@select="selectBuilding"
							@select-room="selectBuildingAndRoom"
						/>
						<v-alert
							v-if="
								selectedBuilding &&
								!selectedBuildingSupportsType
							"
							type="warning"
							variant="tonal"
							rounded="lg"
							class="mt-4"
						>
							{{
								$t(
									'component.internal.spaceRequirement.building.notSupported'
								)
							}}
						</v-alert>
					</estate-order-step>

					<!-- ROOM (optional refinement of the chosen building) -->
					<div
						v-if="
							selectedBuilding &&
							!showRoomSelector &&
							!selectedRoom
						"
						class="mt-6"
					>
						<v-btn
							variant="tonal"
							rounded="lg"
							color="grey-darken-2"
							prepend-icon="add"
							@click="showRoomSelector = true"
						>
							{{
								$t(
									'component.internal.spaceRequirement.room.add'
								)
							}}
						</v-btn>
					</div>
					<estate-order-step
						v-else-if="selectedBuilding"
						:show-clear="!!selectedRoom"
						:show-skip="!selectedRoom"
						@clear="changeRoom"
						@skip="showRoomSelector = false"
						class="mt-6"
					>
						<template #title>
							<span v-if="selectedRoom">
								{{
									$t(
										'component.internal.faultReport.room.selected'
									)
								}}
							</span>
							<span v-else>
								{{
									$t(
										'component.internal.spaceRequirement.room.select'
									)
								}}
							</span>
						</template>
						<template
							#header-btn
							v-if="
								selectedBuilding.blueprintAvailable &&
								!selectedRoom
							"
						>
							<room-blueprint-selector
								:building="selectedBuilding"
								@room-selected="selectRoom"
							/>
						</template>
						<room-selector
							class="mt-2"
							:building="selectedBuilding"
							:skipped-room="false"
							:selected-room="selectedRoom"
							@select="selectRoom"
						/>
					</estate-order-step>

					<!-- CONTACT INFORMATION -->
					<estate-order-step
						:title="
							$t(
								'component.internal.spaceRequirement.general.contactLabel'
							)
						"
						class="mt-6"
					>
						<p class="text-medium-emphasis">
							{{
								$t(
									'component.internal.spaceRequirement.general.contactHelpText'
								)
							}}
						</p>
						<fault-contact-info
							v-model:contactName="contactName"
							v-model:contactEmail="contactEmail"
							v-model:contactPhone="contactPhone"
							:field-error="fieldError"
						/>
					</estate-order-step>

					<v-alert
						v-if="Object.keys(errors).length || manualErrors.length"
						type="error"
						variant="outlined"
						rounded="lg"
						class="mt-4"
					>
						<ul>
							<li v-for="err in manualErrors" :key="err.id">
								<a :href="`#${err.id}`">{{ err.message }}</a>
							</li>
							<li
								v-for="(error, fieldId) in errors"
								:key="error + fieldId"
							>
								<a :href="`#${fieldId}`">{{ error }}</a>
							</li>
						</ul>
					</v-alert>
					<v-alert
						v-if="serverErrors"
						type="error"
						variant="outlined"
						rounded="lg"
						class="mt-4"
					>
						<ul>
							<li
								v-for="(codes, field) in serverErrors"
								:key="field"
							>
								<span v-for="code in codes" :key="code">
									{{
										t(
											`app.error.estate.validation.${code}`,
											code
										)
									}}
								</span>
							</li>
						</ul>
					</v-alert>

					<div class="d-flex align-center justify-center pa-4 mt-4">
						<v-btn
							color="primary"
							size="large"
							rounded="lg"
							:disabled="isBusySubmitting"
							:loading="isBusySubmitting"
							@click="submitReport"
						>
							{{
								$t(
									'component.internal.spaceRequirement.submitButton'
								)
							}}
						</v-btn>
					</div>
				</vee-form>
			</div>
			<div class="info-wrap">
				<v-alert rounded="lg">
					<h2 class="ma-0">
						{{
							$t('component.internal.spaceRequirement.info.title')
						}}
					</h2>
					<p
						v-for="(paragraph, index) in tm(
							'component.internal.spaceRequirement.info.paragraphs'
						)"
						:key="index"
					>
						{{ paragraph }}
					</p>
				</v-alert>
			</div>
		</div>
	</app-content>
</template>

<script lang="ts" setup>
import '@/themes/estate.scss';
import AppContent from '@/components/app/AppContent.vue';
import {
	IBuildingDetails,
	IBuildingRoom,
	ISubmitEstateOrder,
	IWorkOrderCategoryOption,
} from '@/models/Interfaces';
import { computed, onMounted, ref, useTemplateRef, watch } from 'vue';
import { EstateRoutes, MyPagesRoutes } from '@/router/routes';
import NavBreadcrumbs from '../../shared/NavBreadcrumbs.vue';
import { useI18n } from 'vue-i18n';
import { EstateOrderCategory } from '@/models/Enums';
import { useRoute, useRouter } from 'vue-router';
import { DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import BaseFileUpload from '@/components/shared/BaseFileUpload.vue';
import { Form as VeeForm } from 'vee-validate';
import BaseTextBox from '@/components/shared/BaseTextBox.vue';
import ErrorService from '@/utils/ErrorService';
import { useServerValidation } from '@/utils/useServerValidation';
import { useWorkOrderConfig } from '@/utils/useWorkOrderConfig';
import BuildingSelector from '../faultReport/buildingSelector/BuildingSelector.vue';
import RoomSelector from '../faultReport/roomSelector/RoomSelector.vue';
import EstateSpaceRequirementCompleted from './EstateSpaceRequirementCompleted.vue';
import OptionCardGrid from '../order/OptionCardGrid.vue';
import type { OptionCard } from '../order/OptionCardGrid.vue';
import FaultContactInfo from '../faultReport/FaultContactInfo.vue';
import EstateOrderStep from '../order/EstateOrderStep.vue';
import RoomBlueprintSelector from '../faultReport/roomSelector/RoomBlueprintSelector.vue';
import BuildingMapSelector from '../faultReport/buildingSelector/BuildingMapSelector.vue';

const route = useRoute();
const router = useRouter();
const { t, te, tm } = useI18n();
const store = useStore<IRootState>();

const breadcrumbs = [
	{
		title: t('app.nav.home'),
		to: { name: MyPagesRoutes.InternalStart },
	},
	{
		title: t('component.internal.spaceRequirement.title'),
		to: { name: EstateRoutes.SpaceRequirement },
	},
];

const selectedBuilding = ref<IBuildingDetails | null>(null);
const selectedRoom = ref<IBuildingRoom | null>(null);
const selectedCategoryId = ref<number | null>(null);

// Building and room are optional and most space requirements need neither, so their
// selectors stay collapsed behind a button until the user actively chooses to add one.
const showBuildingSelector = ref(false);
const showRoomSelector = ref(false);

const categoryOptions = ref<IWorkOrderCategoryOption[]>([]);
const isLoadingCategories = ref(false);

const isLoadingFromQuery = ref(false);
const isBusySubmitting = ref(false);
const hasSubmitted = ref(false);
const submitAttempted = ref(false);

const user = computed(() => store.state.user);

const formValidator = useTemplateRef('formValidator');
const problemDescription = ref('');
const attachments = ref<File[]>([]);

const {
	maxFiles: uploadMaxFiles,
	maxSizeMb: uploadMaxSizeMb,
	accept: uploadAccept,
} = useWorkOrderConfig();

const {
	serverErrors,
	fileErrors: fileServerErrors,
	fieldError,
	setFromError,
	clear: clearServerErrors,
} = useServerValidation('app.error.estate.validation');
const descriptionServerError = fieldError('description');

const contactName = ref(user.value?.fullName ?? '');
const contactEmail = ref(user.value?.email ?? '');
const contactPhone = ref('');

watch(
	[problemDescription, attachments, contactName, contactEmail, contactPhone],
	clearServerErrors,
	{ deep: true }
);

// Material Icons (md set) per SpaceRequirement leaf category, keyed by the stable
// Pythagoras leaf-category id. New/unknown categories fall back to a neutral icon.
const CATEGORY_ICONS: Record<number, string> = {
	88: 'accessible', // Tillgänglighetsanpassningar
	89: 'manage_search', // Generella utredningar
	90: 'handyman', // Mindre anpassningar i befintliga lokaler
	91: 'architecture', // Större förändringar i lokalbehov
	97: 'apartment', // Lägenhetsbehov
};
const DEFAULT_CATEGORY_ICON = 'space_dashboard';

// Categories where a building is the usual case, so we expand the building selector up front
// instead of hiding it behind the "Välj byggnad" button. Keyed by Pythagoras leaf-category id:
// 88 = Tillgänglighetsanpassningar, 90 = Mindre anpassningar i befintliga lokaler.
const BUILDING_PROMPTED_CATEGORIES = new Set<number>([88, 90]);

// Short explanatory text per SpaceRequirement leaf category, keyed by the stable
// Pythagoras leaf-category id (see locales -> category.descriptions). Categories without
// a matching locale key simply show no description.
const categoryDescription = (id: number) => {
	const key = `component.internal.spaceRequirement.category.descriptions.${id}`;
	return te(key) ? t(key) : '';
};

const categoryCards = computed<OptionCard[]>(() =>
	categoryOptions.value.map((option) => ({
		value: option.id.toString(),
		icon: CATEGORY_ICONS[option.id] ?? DEFAULT_CATEGORY_ICON,
		title: option.name,
		description: categoryDescription(option.id),
	}))
);

const selectedCategoryValue = computed(() =>
	selectedCategoryId.value !== null
		? selectedCategoryId.value.toString()
		: null
);

// Building is optional. If one IS selected it must support this work order type
// (the backend rejects otherwise), so flag an unsupported pick before submit.
const selectedBuildingSupportsType = computed(
	() =>
		!selectedBuilding.value ||
		(selectedBuilding.value.workOrderTypes ?? []).includes(
			EstateOrderCategory.SpaceRequirement
		)
);

const categoryMissing = computed(
	() => submitAttempted.value && selectedCategoryId.value === null
);

// Aggregated, anchor-linked summary of the non-form (card-based) requirements,
// shown alongside the vee-validate field errors above the submit button. Building
// is optional, so only the category can be "missing" here.
const manualErrors = computed(() => {
	const list: { id: string; message: string }[] = [];
	if (categoryMissing.value) {
		list.push({
			id: 'space-requirement-category',
			message: t('component.internal.spaceRequirement.category.required'),
		});
	}
	return list;
});

const updateQueryParams = () => {
	const queryParams: Record<string, string | number | undefined> = {
		buildingId: selectedBuilding.value?.id,
		roomId: selectedRoom.value?.id,
		submitted: hasSubmitted.value ? 'true' : undefined,
	};
	if (route.name) {
		router.replace({ name: route.name, query: queryParams });
	}
};

// SpaceRequirement categories are type-global (the endpoint is keyed by work-order type,
// not by building), so we load them once up front - independent of any building selection.
const loadCategories = async () => {
	isLoadingCategories.value = true;
	try {
		categoryOptions.value = await store.dispatch(
			DispatchType.GetWorkOrderCategories,
			{ workOrderType: EstateOrderCategory.SpaceRequirement }
		);
	} catch (err) {
		categoryOptions.value = [];
		ErrorService.onError({
			err,
			hidden: true,
			message:
				'Failed to load space requirement categories, user cannot select a category',
		});
	} finally {
		isLoadingCategories.value = false;
	}
};

const selectBuilding = async (building: IBuildingDetails | null) => {
	// Room belongs to a building, so clear it (and re-collapse its selector) on change.
	selectedRoom.value = null;
	showRoomSelector.value = false;
	selectedBuilding.value = building;
	// Collapse back to the button when the building is cleared; keep it open otherwise.
	showBuildingSelector.value = building !== null;
	updateQueryParams();
};

const selectRoom = async (room: IBuildingRoom | null) => {
	selectedRoom.value = room;
	// Collapse back to the button when the room is cleared.
	showRoomSelector.value = room !== null;
	updateQueryParams();
};

// "Ändra" resets the current pick but keeps the selector expanded (and empty), so the user
// lands on the same state as the up-front prompt - ready to choose another building/room -
// rather than folding all the way back to the button. Folding away is "Hoppa över" (skip).
const changeBuilding = async () => {
	await selectBuilding(null);
	showBuildingSelector.value = true;
};

const changeRoom = async () => {
	await selectRoom(null);
	showRoomSelector.value = true;
};

const selectCategory = async (value: string) => {
	const id = parseInt(value);
	selectedCategoryId.value = id;
	// Some categories almost always concern a specific building, so expand the selector up
	// front. For the rest it stays collapsed behind the button - unless a building is already
	// picked, in which case the full selector is shown regardless.
	if (!selectedBuilding.value) {
		showBuildingSelector.value = BUILDING_PROMPTED_CATEGORIES.has(id);
	}
};

const selectBuildingAndRoom = async ({
	building,
	room,
}: {
	building: IBuildingDetails;
	room: IBuildingRoom;
}) => {
	await selectBuilding(building);
	selectRoom(room);
};

const loadFromQueryParams = async () => {
	const query = route.query;

	if (query.submitted === 'true') {
		hasSubmitted.value = true;
		return;
	}

	const buildingId = query.buildingId
		? parseInt(query.buildingId as string)
		: null;
	const roomId = query.roomId ? parseInt(query.roomId as string) : null;

	if (buildingId) {
		isLoadingFromQuery.value = true;
		try {
			const building = await store.dispatch(
				DispatchType.GetBuildingById,
				{
					buildingId,
				}
			);
			await selectBuilding(building ?? null);
			if (building && roomId) {
				const room = await store.dispatch(DispatchType.GetRoomById, {
					roomId,
				});
				selectRoom(room);
			}
		} catch (err) {
			ErrorService.onError({
				err,
				hidden: true,
				message:
					'Failed to load building from query params on space requirement page, user have to manually select',
			});
		}
	}

	isLoadingFromQuery.value = false;
};

const submitReport = async () => {
	submitAttempted.value = true;
	const validationResult = await formValidator.value?.validate();
	// Building and room are both optional. A category and a valid form are always required;
	// if a building IS selected it must support this work order type.
	if (
		selectedCategoryId.value === null ||
		!selectedBuildingSupportsType.value ||
		!validationResult?.valid
	) {
		return;
	}

	isBusySubmitting.value = true;

	const reportData: ISubmitEstateOrder = {
		buildingId: selectedBuilding.value?.id,
		category: EstateOrderCategory.SpaceRequirement,
		categoryId: selectedCategoryId.value,
		roomId: selectedRoom.value?.id,
		description: problemDescription.value,
		attachments: attachments.value,
		notifierName: contactName.value,
		notifierEmail: contactEmail.value,
		notifierPhone: contactPhone.value,
	};

	try {
		clearServerErrors();
		await store.dispatch(DispatchType.SubmitEstateOrder, reportData);

		hasSubmitted.value = true;
		updateQueryParams();
		window.scrollTo({ top: 0 });
	} catch (err) {
		if (!setFromError(err)) {
			ErrorService.onError({
				err,
				message: t('app.error.estate.unableToSubmitOrder'),
			});
		}
	} finally {
		isBusySubmitting.value = false;
	}
};

onMounted(() => {
	loadCategories();
	loadFromQueryParams();
});
</script>

<style scoped lang="scss">
.estate-space-requirement {
	:deep(.v-container) {
		padding-top: 1rem;
	}

	.content-wrap {
		display: flex;
		flex-wrap: wrap;
		gap: 2rem;
		justify-content: space-between;

		.order-wrap {
			flex: 1;
			max-width: 650px;
			min-width: 500px;

			a {
				color: inherit !important; /** Override global link color */
			}
			:deep(.help-and-error-wrap) {
				margin-bottom: 8px;
			}
		}
		.info-wrap {
			margin-top: 46px;
			width: 300px;
		}
		@media only screen and (max-width: 920px) {
			.order-wrap {
				min-width: auto;
				max-width: none;
			}
			.info-wrap {
				margin-top: 0;
				width: 100%;
			}
		}
	}
}
</style>
