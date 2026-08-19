<template>
	<app-content
		class="estate-default estate-fault-report"
		:pageTitle="$t('component.appHeader.title.faultReport')"
		:is-loading="isLoadingFromQuery"
	>
		<div class="content-wrap">
			<div class="pb-4 report-wrap">
				<nav-breadcrumbs
					class="mb-2"
					:breadcrumbs="breadcrumbs"
					full-width
				/>
				<estate-fault-report-completed v-if="hasSubmitted" />
				<div v-else class="mt-2">
					<div class="pb-4">
						<h1 class="ma-0 mb-2">
							{{ $t('component.faultReport.title') }}
						</h1>
						<p class="ma-0">
							{{ $t('component.faultReport.description') }}
						</p>
					</div>

					<!-- BUILDING SELECTOR -->
					<estate-order-step
						:title="
							selectedBuilding
								? $t('component.faultReport.building.selected')
								: $t('component.faultReport.building.select')
						"
						:step="1"
						:step-count="stepCount"
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
						<v-alert
							v-if="
								selectedBuildingIsRented &&
								selectedBuilding?.externalOwnerInfo
							"
							type="info"
							variant="tonal"
							rounded="lg"
							class="mt-4"
						>
							{{
								$t('component.faultReport.rentedBuildingNotice')
							}}

							<external-owner-info
								:externalOwnerInfo="
									selectedBuilding.externalOwnerInfo
								"
								class="ml-0 mt-2 pa-0 d-flex"
								transparent
							/>
							<div class="d-flex justify-end">
								<v-btn
									v-if="!hasConfirmedRentedBuildingNotice"
									flat
									@click="
										hasConfirmedRentedBuildingNotice = true
									"
									class="mt-4"
								>
									{{
										$t(
											'component.faultReport.rentedBuildingNoticeConfirmButton'
										)
									}}
								</v-btn>
							</div>
						</v-alert>
					</estate-order-step>

					<!-- OUTDOOR / INDOOR SELECTOR -->
					<estate-order-step
						v-if="
							selectedBuilding &&
							(!selectedBuildingIsRented ||
								hasConfirmedRentedBuildingNotice)
						"
						:show-clear="!!problemLocation"
						@clear="selectLocation(null)"
						:step="2"
						:step-count="stepCount"
						class="mt-6"
						ref="locationTitle"
					>
						<template #title>
							<span
								v-if="
									problemLocation ===
									EstateFaultLocation.Indoor
								"
							>
								{{
									$t(
										'component.faultReport.location.indoor.title'
									)
								}}
							</span>
							<span
								v-else-if="
									problemLocation ===
									EstateFaultLocation.Outdoor
								"
							>
								{{
									$t(
										'component.faultReport.location.outdoor.title'
									)
								}}
							</span>
							<span v-else>
								{{
									$t('component.faultReport.location.select')
								}}
							</span>
						</template>
						<fault-location-selector
							class="mt-2"
							:problem-location="problemLocation"
							@select="selectLocation"
						/>
					</estate-order-step>

					<!-- ROOM SELECTOR -->
					<estate-order-step
						v-if="
							problemLocation === EstateFaultLocation.Indoor &&
							selectedBuilding
						"
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
								{{ $t('component.faultReport.room.select') }}
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
							:title="
								$t('component.faultReport.general.problemTitle')
							"
							:step="stepCount - 1"
							:step-count="stepCount"
							class="mt-6"
							ref="problemTitle"
						>
							<base-text-box
								id="problem-description"
								:label="
									$t(
										'component.faultReport.general.problemLabel'
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
										'component.faultReport.general.problemHelpText'
									)
								}}
							</p>

							<base-file-upload
								id="file-upload"
								:label="
									$t(
										'component.faultReport.general.fileUploadLabel'
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
							:title="
								$t('component.faultReport.general.contactLabel')
							"
							:step="stepCount"
							:step-count="stepCount"
							class="mt-6"
						>
							<p class="text-medium-emphasis">
								{{
									$t(
										'component.faultReport.general.contactHelpText'
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
								{{ $t('component.faultReport.submitButton') }}
							</v-btn>
						</div>
					</vee-form>
				</div>
			</div>
			<div class="info-wrap">
				<v-alert rounded="lg">
					<h2 class="ma-0">
						{{ $t('component.faultReport.info.title') }}
					</h2>
					<p>
						{{ $t('component.faultReport.info.text1') }}
					</p>
					<p>
						{{ $t('component.faultReport.info.text2') }}
					</p>
					<p v-html="$t('component.faultReport.info.text3')"></p>
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
	ISubmitEstateFaultReport,
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
import { EstateFaultLocation, ExternalOwnerStatus } from '@/models/Enums';
import BuildingSelector from './buildingSelector/BuildingSelector.vue';
import RoomSelector from './roomSelector/RoomSelector.vue';
import { useRoute, useRouter } from 'vue-router';
import { DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import BaseFileUpload from '@/components/shared/BaseFileUpload.vue';
import FaultLocationSelector from './FaultLocationSelector.vue';
import EstateFaultReportCompleted from './EstateFaultReportCompleted.vue';
import { Form as VeeForm } from 'vee-validate';
import BaseTextBox from '@/components/shared/BaseTextBox.vue';
import ErrorService from '@/utils/ErrorService';
import { useServerValidation } from '@/utils/useServerValidation';
import { useWorkOrderConfig } from '@/utils/useWorkOrderConfig';
import FaultContactInfo from './FaultContactInfo.vue';
import EstateOrderStep from '../order/EstateOrderStep.vue';
import RoomBlueprintSelector from './roomSelector/RoomBlueprintSelector.vue';
import BuildingMapSelector from './buildingSelector/BuildingMapSelector.vue';
import ExternalOwnerInfo from '../estate/ExternalOwnerInfo.vue';

const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const store = useStore<IRootState>();

const breadcrumbs = [
	{
		title: t('component.faultReport.title'),
		to: { name: EstateRoutes.FaultReport },
	},
];

const locationTitleRef = useTemplateRef('locationTitle');
const roomTitleRef = useTemplateRef('roomTitle');
const problemTitleRef = useTemplateRef('problemTitle');

const selectedBuilding = ref<IBuildingDetails | null>(null);
const selectedRoom = ref<IBuildingRoom | null>(null);
const skippedRoom = ref(false);
const problemLocation = ref<EstateFaultLocation | null>(null);

const isLoadingFromQuery = ref(false);
const isBusySubmitting = ref(false);
const hasSubmitted = ref(false);
const hasConfirmedRentedBuildingNotice = ref(false);

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

const selectedBuildingIsRented = computed(() => {
	return (
		selectedBuilding.value?.externalOwnerInfo?.status ===
		ExternalOwnerStatus.Inhyrd
	);
});

watch(
	[problemDescription, attachments, contactName, contactEmail, contactPhone],
	clearServerErrors,
	{ deep: true }
);

watch(
	() => selectedBuilding.value,
	(newVal, oldVal) => {
		if (oldVal) {
			problemLocation.value = null;
		}
		if (newVal) {
			hasConfirmedRentedBuildingNotice.value = false;
		}
	}
);
watch(
	() => problemLocation.value,
	(_, oldVal) => {
		if (oldVal) {
			selectedRoom.value = null;
			skippedRoom.value = false;
		}
	}
);

const showLastSteps = computed(() => {
	return (
		(selectedRoom.value &&
			problemLocation.value === EstateFaultLocation.Indoor) ||
		skippedRoom.value ||
		problemLocation.value === EstateFaultLocation.Outdoor
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
	problemLocation.value = null;
	selectedBuilding.value = building;

	scrollToAfterUiUpdate(locationTitleRef);
	updateQueryParams();
};

const selectLocation = async (location: EstateFaultLocation | null) => {
	selectedRoom.value = null;
	skippedRoom.value = false;
	problemLocation.value = location;

	scrollToAfterUiUpdate(
		location === EstateFaultLocation.Indoor ? roomTitleRef : problemTitleRef
	);
};

const selectRoom = async (room: IBuildingRoom | null, skipped = false) => {
	selectedRoom.value = room;
	skippedRoom.value = skipped;

	scrollToAfterUiUpdate(problemTitleRef);
	updateQueryParams();
};

const selectBuildingAndRoom = async ({
	building,
	room,
}: {
	building: IBuildingDetails;
	room: IBuildingRoom;
}) => {
	selectedBuilding.value = building;
	problemLocation.value = EstateFaultLocation.Indoor;
	selectRoom(room);
};

const stepCount = computed(() =>
	problemLocation.value === EstateFaultLocation.Outdoor ? 4 : 5
);

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
				problemLocation.value = EstateFaultLocation.Indoor;
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
					'Failed to load building/room from query params on fault report page, user have to manually select',
			});
		}
	}

	isLoadingFromQuery.value = false;
};

const submitReport = async () => {
	const validationResult = await formValidator.value?.validate();
	if (
		!selectedBuilding.value ||
		!problemLocation.value ||
		!validationResult?.valid
	) {
		return;
	}

	isBusySubmitting.value = true;

	const reportData: ISubmitEstateFaultReport = {
		buildingId: selectedBuilding.value?.id,
		location: problemLocation.value,
		roomId: selectedRoom.value?.id,
		description: problemDescription.value,
		attachments: attachments.value,
		notifierName: contactName.value,
		notifierEmail: contactEmail.value,
		notifierPhone: contactPhone.value,
	};

	try {
		clearServerErrors();
		await store.dispatch(DispatchType.SubmitFaultReport, reportData);

		hasSubmitted.value = true;
		updateQueryParams();
		window.scrollTo({ top: 0 });
	} catch (err) {
		if (!setFromError(err)) {
			ErrorService.onError({
				err,
				message: t('app.error.estate.unableToSubmitFaultReport'),
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
.estate-fault-report {
	:deep(.v-container) {
		padding-top: 1rem;
	}

	// Layout comes from the shared .content-wrap skeleton in estate.scss.
	.content-wrap .report-wrap {
		a {
			color: inherit !important; /** Override global link color */
		}
		:deep(.help-and-error-wrap) {
			margin-bottom: 8px;
		}
	}
}
</style>
