<template>
	<app-content
		class="estate-default estate-order"
		:pageTitle="$t('component.appHeader.title.order')"
		:is-loading="isLoadingFromQuery"
	>
		<div class="content-wrap">
			<div class="pb-4 order-wrap">
				<nav-breadcrumbs
					class="mb-2"
					:breadcrumbs="breadcrumbs"
					full-width
				/>
				<estate-order-completed v-if="hasSubmitted" />
				<div v-else class="mt-2">
					<div class="pb-4">
						<h1 class="ma-0 mb-2">
							{{ $t('component.order.title') }}
						</h1>
						<p class="ma-0">
							{{ $t('component.order.description') }}
						</p>
					</div>

					<!-- BUILDING SELECTOR -->
					<estate-order-step
						:step="1"
						:step-count="stepCount"
						:title="
							selectedBuilding
								? $t('component.faultReport.building.selected')
								: $t('component.faultReport.building.select')
						"
						:show-clear="!!selectedBuilding"
						@clear="selectBuilding(null)"
					>
						<template #header-btn v-if="!selectedBuilding">
							<building-map-selector @select="selectBuilding" />
						</template>
						<building-selector
							:selected-building="selectedBuilding"
							@select="selectBuilding"
							@select-room="selectBuildingAndRoom"
						/>
					</estate-order-step>

					<!-- CATEGORY SELECTOR -->
					<estate-order-step
						v-if="
							selectedBuilding && availableCategories.length > 0
						"
						:step="2"
						:step-count="stepCount"
						:title="$t('component.order.category.title')"
						ref="categoryTitle"
						class="mt-6"
					>
						<order-category-selector
							:available-categories="availableCategories"
							:category="selectedCategory"
							@select="selectCategory"
						/>
					</estate-order-step>
					<v-alert
						v-else-if="selectedBuilding"
						type="info"
						variant="tonal"
						rounded="lg"
						class="mt-6"
					>
						{{ $t('component.order.category.noneAvailable') }}
					</v-alert>

					<!-- ROOM SELECTOR -->
					<estate-order-step
						v-if="selectedBuilding && selectedCategory"
						:step="3"
						:step-count="stepCount"
						:show-clear="!!selectedRoom || skippedRoom"
						@clear="selectRoom(null)"
						:show-skip="!selectedRoom && !skippedRoom"
						@skip="selectRoom(null, true)"
						ref="roomTitle"
						class="mt-6"
					>
						<template #title>
							<span v-if="selectedRoom && !skippedRoom">
								{{ $t('component.faultReport.room.selected') }}
							</span>
							<span v-else-if="!selectedRoom && skippedRoom">
								{{ $t('component.faultReport.room.none') }}
							</span>
							<span v-else>
								{{ $t('component.order.room.select') }}
							</span>
						</template>
						<template
							#header-btn
							v-if="
								selectedBuilding.blueprintAvailable &&
								!selectedRoom &&
								!skippedRoom
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
							:skipped-room="skippedRoom"
							:selected-room="selectedRoom"
							@select="selectRoom"
						/>
					</estate-order-step>

					<!-- PROBLEM DESCRIPTION -->
					<vee-form
						v-if="showLastSteps"
						ref="formValidator"
						v-slot="{ errors }"
						@submit.prevent="submitReport"
					>
						<estate-order-step
							:step="4"
							:step-count="stepCount"
							:title="
								$t('component.order.general.descriptionTitle')
							"
							ref="problemTitle"
							class="mt-6"
						>
							<base-text-box
								id="problem-description"
								:label="
									$t(
										'component.order.general.descriptionLabel'
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
										'component.order.general.descriptionHelpText'
									)
								}}
							</p>

							<base-file-upload
								id="file-upload"
								:label="
									$t(
										'component.order.general.fileUploadLabel'
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

						<!-- CONTACT INFORMATION -->
						<estate-order-step
							:title="$t('component.order.general.contactLabel')"
							:step="5"
							:step-count="stepCount"
							class="mt-6"
						>
							<p class="text-medium-emphasis">
								{{
									$t(
										'component.order.general.contactHelpText'
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
							v-if="Object.keys(errors).length"
							type="error"
							variant="outlined"
							rounded="lg"
							class="mt-4"
						>
							<ul
								v-for="(error, fieldId) in errors"
								:key="error + fieldId"
							>
								<li>
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

						<div
							class="d-flex align-center justify-center pa-4 mt-4"
						>
							<v-btn
								color="primary"
								size="large"
								rounded="lg"
								:disabled="isBusySubmitting"
								:loading="isBusySubmitting"
								@click="submitReport"
							>
								{{ $t('component.order.submitButton') }}
							</v-btn>
						</div>
					</vee-form>
				</div>
			</div>
			<div class="info-wrap">
				<v-alert rounded="lg">
					<h2 class="ma-0">
						{{ $t('component.order.info.title') }}
					</h2>
					<p>
						{{ $t('component.order.info.text1') }}
					</p>
					<p>
						{{ $t('component.order.info.text2') }}
					</p>
					<p>
						{{ $t('component.order.info.text3') }}
					</p>
					<p>
						{{ $t('component.order.info.text4') }}
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
} from '@/models/Interfaces';
import {
	computed,
	nextTick,
	onMounted,
	Ref,
	ref,
	useTemplateRef,
	watch,
} from 'vue';
import { EstateRoutes } from '@/router/routes';
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
import EstateOrderCompleted from './EstateOrderCompleted.vue';
import OrderCategorySelector from './OrderCategorySelector.vue';
import FaultContactInfo from '../faultReport/FaultContactInfo.vue';
import EstateOrderStep from './EstateOrderStep.vue';
import RoomBlueprintSelector from '../faultReport/roomSelector/RoomBlueprintSelector.vue';
import BuildingMapSelector from '../faultReport/buildingSelector/BuildingMapSelector.vue';

const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const store = useStore<IRootState>();

const breadcrumbs = [
	{
		title: t('component.order.title'),
		to: { name: EstateRoutes.Order },
	},
];

const categoryTitleRef = useTemplateRef('categoryTitle');
const roomTitleRef = useTemplateRef('roomTitle');
const problemTitleRef = useTemplateRef('problemTitle');

const selectedBuilding = ref<IBuildingDetails | null>(null);
const selectedRoom = ref<IBuildingRoom | null>(null);
const skippedRoom = ref(false);
const selectedCategory = ref<EstateOrderCategory | null>(null);

const isLoadingFromQuery = ref(false);
const isBusySubmitting = ref(false);
const hasSubmitted = ref(false);

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

watch(
	() => selectedBuilding.value,
	(_, oldVal) => {
		if (oldVal) {
			selectedCategory.value = null;
			selectedRoom.value = null;
			skippedRoom.value = false;
		}
	}
);

const orderCategoryValues = Object.values(EstateOrderCategory) as string[];
const availableCategories = computed(
	() =>
		(selectedBuilding.value?.workOrderTypes ?? []).filter((t) =>
			orderCategoryValues.includes(t)
		) as EstateOrderCategory[]
);

const showLastSteps = computed(() => {
	return (
		(selectedRoom.value || skippedRoom.value) &&
		selectedCategory.value &&
		selectedBuilding.value
	);
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

const scrollToAfterUiUpdate = async (
	elRef: Ref<InstanceType<typeof EstateOrderStep> | null>
) => {
	await nextTick();
	setTimeout(() => {
		elRef.value?.title?.scrollIntoView({
			behavior: 'smooth',
			block: 'center',
		});
	}, 50);
};

const selectBuilding = async (building: IBuildingDetails | null) => {
	selectedRoom.value = null;
	skippedRoom.value = false;
	selectedCategory.value = null;
	selectedBuilding.value = building;

	scrollToAfterUiUpdate(categoryTitleRef);
	updateQueryParams();
};

const selectRoom = async (room: IBuildingRoom | null, skipped = false) => {
	selectedRoom.value = room;
	skippedRoom.value = skipped;

	scrollToAfterUiUpdate(problemTitleRef);
	updateQueryParams();
};

const selectCategory = async (category: EstateOrderCategory | null) => {
	selectedCategory.value = category;

	scrollToAfterUiUpdate(roomTitleRef);
};

const selectBuildingAndRoom = async ({
	building,
	room,
}: {
	building: IBuildingDetails;
	room: IBuildingRoom;
}) => {
	selectedBuilding.value = building;
	selectRoom(room);
};

const stepCount = 5;

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
			selectBuilding(building ?? null);
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
					'Failed to load building from query params on order page, user have to manually select',
			});
		}
	}

	isLoadingFromQuery.value = false;
};

const submitReport = async () => {
	const validationResult = await formValidator.value?.validate();
	if (
		!selectedBuilding.value ||
		!selectedCategory.value ||
		(!selectedRoom.value && !skippedRoom.value) ||
		!validationResult?.valid
	) {
		return;
	}

	isBusySubmitting.value = true;

	const reportData: ISubmitEstateOrder = {
		buildingId: selectedBuilding.value?.id,
		category: selectedCategory.value,
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
	loadFromQueryParams();
});
</script>

<style scoped lang="scss">
.estate-order {
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
