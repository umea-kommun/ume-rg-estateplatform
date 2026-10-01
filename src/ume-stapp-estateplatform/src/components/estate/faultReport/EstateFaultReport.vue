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
						:title="$t('component.faultReport.stepTitle.building')"
						:step="1"
						:step-count="stepCount"
						:state="faultStepState(1)"
						ref="buildingTitle"
						class="mt-0"
						rail
					>
						<building-selector
							:selected-building="selectedBuilding"
							@select="selectBuilding"
							@select-room="selectBuildingAndRoom"
						>
							<template #search-action>
								<building-map-selector
									@select="selectBuilding"
								/>
							</template>
						</building-selector>
						<!-- Rented-building notice: shown once a rented building is
					picked, gating the next step until acknowledged -->
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
						:title="$t('component.faultReport.stepTitle.location')"
						:step="2"
						:step-count="stepCount"
						:state="faultStepState(2)"
						ref="locationTitle"
						rail
					>
						<p
							v-if="!problemLocation"
							class="text-medium-emphasis mt-2"
						>
							{{ $t('component.faultReport.location.select') }}
						</p>
						<fault-location-selector
							:problem-location="problemLocation"
							@select="selectLocation"
						/>
					</estate-order-step>

					<!-- ROOM SELECTOR -->
					<estate-order-step
						:title="$t('component.faultReport.stepTitle.room')"
						:step="3"
						:step-count="stepCount"
						:state="faultStepState(3)"
						ref="roomTitle"
						rail
					>
						<!-- Building has no real room breakdown: the step is skipped
						automatically with an explanation, like an outdoor fault. -->
						<selection-card
							v-if="roomInfoUnavailable"
							class="mt-2"
							muted
							icon="no_meeting_room"
							:title="
								$t('component.faultReport.room.noInfoTitle')
							"
							:description="
								$t('component.faultReport.room.noInfo')
							"
						/>
						<room-selector
							v-else-if="
								problemLocation ===
									EstateFaultLocation.Indoor &&
								selectedBuilding
							"
							class="mt-2"
							:building="selectedBuilding"
							:skipped-room="skippedRoom"
							:selected-room="selectedRoom"
							@select="selectRoom"
							@skip="selectRoom(null, true)"
						/>
						<selection-card
							v-else-if="
								problemLocation === EstateFaultLocation.Outdoor
							"
							class="mt-2"
							muted
							icon="no_meeting_room"
							:title="$t('component.roomSelector.noneTitle')"
							:description="
								$t('component.faultReport.room.noneOutdoor')
							"
						/>
					</estate-order-step>

					<!-- PROBLEM DESCRIPTION -->
					<vee-form
						ref="formValidator"
						v-slot="{ errors }"
						@submit.prevent="submitReport"
					>
						<!-- Final step: problem description, attachments and the
						(pre-filled) contact details together, so the rail stays
						strictly one-step-at-a-time through to submit. -->
						<estate-order-step
							:title="
								$t('component.faultReport.general.problemTitle')
							"
							:step="stepCount"
							:step-count="stepCount"
							:state="faultStepState(4)"
							ref="problemTitle"
							rail
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

							<!-- Contact details: a labelled subsection rather than a
							separate step, since they are pre-filled from the user. -->
							<div class="contact-section mt-8">
								<h3 class="ma-0 mb-1">
									{{
										$t(
											'component.faultReport.general.contactLabel'
										)
									}}
								</h3>
								<p class="text-medium-emphasis mt-0">
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
							</div>
							<v-alert
								v-if="
									showLastSteps && Object.keys(errors).length
								"
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
								v-if="showLastSteps"
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
									{{
										$t('component.faultReport.submitButton')
									}}
								</v-btn>
							</div>
						</estate-order-step>
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
					<p
						v-html="
							$t('component.faultReport.info.responsibilityLink')
						"
					></p>
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
import { computed, onMounted, ref, useTemplateRef, watch } from 'vue';
import { EstateRoutes } from '@/router/routes';
import NavBreadcrumbs from '../../shared/NavBreadcrumbs.vue';
import { useI18n } from 'vue-i18n';
import { EstateFaultLocation, ExternalOwnerStatus } from '@/models/Enums';
import BuildingSelector from './buildingSelector/BuildingSelector.vue';
import RoomSelector from './roomSelector/RoomSelector.vue';
import { DispatchType } from '@/models/Enums';
import BaseFileUpload from '@/components/shared/BaseFileUpload.vue';
import FaultLocationSelector from './FaultLocationSelector.vue';
import EstateFaultReportCompleted from './EstateFaultReportCompleted.vue';
import { Form as VeeForm } from 'vee-validate';
import BaseTextBox from '@/components/shared/BaseTextBox.vue';
import { useWorkOrderForm } from '@/utils/useWorkOrderForm';
import { useStepScroll } from '@/utils/useStepScroll';
import FaultContactInfo from './FaultContactInfo.vue';
import EstateOrderStep from '../order/EstateOrderStep.vue';
import SelectionCard from '../order/SelectionCard.vue';
import BuildingMapSelector from './buildingSelector/BuildingMapSelector.vue';
import ExternalOwnerInfo from '../estate/ExternalOwnerInfo.vue';

const { t, locale } = useI18n();

const breadcrumbs = computed(() => {
	if (!locale.value) return [];
	return [
		{
			title: t('component.faultReport.title'),
			to: { name: EstateRoutes.FaultReport },
		},
	];
});

const {
	selectedBuilding,
	selectedRoom,
	skippedRoom,
	isLoadingFromQuery,
	isBusySubmitting,
	hasSubmitted,
	problemDescription,
	attachments,
	uploadMaxFiles,
	uploadMaxSizeMb,
	uploadAccept,
	serverErrors,
	fileServerErrors,
	fieldError,
	descriptionServerError,
	contactName,
	contactEmail,
	contactPhone,
	updateQueryParams,
	loadFromQueryParams,
	submit,
} = useWorkOrderForm();

const { scrollToStep } = useStepScroll();

const buildingTitleRef = useTemplateRef('buildingTitle');
const locationTitleRef = useTemplateRef('locationTitle');
const roomTitleRef = useTemplateRef('roomTitle');
const problemTitleRef = useTemplateRef('problemTitle');

const formValidator = useTemplateRef('formValidator');

// Fault-report-specific state: indoor/outdoor location and the rented-building
// acknowledgement. Building and room come from the shared work-order flow.
const problemLocation = ref<EstateFaultLocation | null>(null);
const roomInfoUnavailable = computed(
	() =>
		problemLocation.value === EstateFaultLocation.Indoor &&
		selectedBuilding.value?.hasRoomInformation === false
);

const hasConfirmedRentedBuildingNotice = ref(false);

const selectedBuildingIsRented = computed(() => {
	return (
		selectedBuilding.value?.externalOwnerInfo?.status ===
		ExternalOwnerStatus.Inhyrd
	);
});

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
// A building without room info auto-resolves the room step, so a room carried in
// from a deep link or favourite would otherwise stay selected and be submitted
// behind the "no room information" card. Clear it so what's shown is what's sent.
// Watch both conditions together since the room may be set after the location.
watch(
	() => roomInfoUnavailable.value && !!selectedRoom.value,
	(hasOrphanedRoom) => {
		if (hasOrphanedRoom) {
			selectedRoom.value = null;
		}
	},
	{ immediate: true }
);

const showLastSteps = computed(() => {
	return (
		(selectedRoom.value &&
			problemLocation.value === EstateFaultLocation.Indoor) ||
		skippedRoom.value ||
		roomInfoUnavailable.value ||
		problemLocation.value === EstateFaultLocation.Outdoor
	);
});

const selectBuilding = async (building: IBuildingDetails | null) => {
	selectedRoom.value = null;
	skippedRoom.value = false;
	problemLocation.value = null;
	selectedBuilding.value = building;

	// Scroll to the next step on selection, or back to this step's title when
	// cleared so it isn't left hidden behind the sticky header.
	scrollToStep(building ? locationTitleRef : buildingTitleRef);
	updateQueryParams();
};

const selectLocation = async (location: EstateFaultLocation | null) => {
	selectedRoom.value = null;
	skippedRoom.value = false;
	problemLocation.value = location;

	// Always land on the room step: when there's a room to pick, to pick it; when
	// it's auto-resolved (outdoor or a building without room info), so the user
	// sees why. The step is compact when skipped, so the problem step shows right
	// below it.
	scrollToStep(location ? roomTitleRef : locationTitleRef);
};

const selectRoom = async (room: IBuildingRoom | null, skipped = false) => {
	selectedRoom.value = room;
	skippedRoom.value = skipped;

	scrollToStep(room || skipped ? problemTitleRef : roomTitleRef);
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

// Building, location, room, and a final description-and-contact step. The room
// step is always present (step 3); an outdoor fault or a building without room
// info auto-resolves it rather than dropping it, so the numbering stays stable.
const stepCount = 4;

// Step 2 unlocks only once a building is chosen and, for rented buildings, the
// tenant has acknowledged the ownership notice.
const buildingReady = computed(
	() =>
		!!selectedBuilding.value &&
		(!selectedBuildingIsRented.value ||
			hasConfirmedRentedBuildingNotice.value)
);

const faultStepState = (
	step: number
): 'completed' | 'current' | 'skipped' | 'upcoming' => {
	switch (step) {
		case 1:
			return buildingReady.value ? 'completed' : 'current';
		case 2:
			if (!buildingReady.value) return 'upcoming';
			return problemLocation.value ? 'completed' : 'current';
		case 3:
			if (!problemLocation.value) return 'upcoming';
			// Outdoor faults, an explicit skip, and buildings without room
			// info all pass the room step without a real choice, so mark it
			// skipped rather than answered.
			if (problemLocation.value === EstateFaultLocation.Outdoor)
				return 'skipped';
			if (selectedRoom.value) return 'completed';
			if (skippedRoom.value || roomInfoUnavailable.value)
				return 'skipped';
			return 'current';
		case 4:
			return showLastSteps.value ? 'current' : 'upcoming';
		default:
			return 'upcoming';
	}
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

	await submit(
		reportData,
		DispatchType.SubmitFaultReport,
		t('app.error.estate.unableToSubmitFaultReport')
	);
};

onMounted(() => {
	loadFromQueryParams({
		onBuilding: (building) => selectBuilding(building),
		// A deep-linked room is always indoor; set the location before selecting it.
		onRoom: (room) => {
			problemLocation.value = EstateFaultLocation.Indoor;
			selectRoom(room);
		},
		errorContext:
			'Failed to load building/room from query params on fault report page, user have to manually select',
	});
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

	// Contact details sit in the final step as a divided subsection.
	.contact-section {
		border-top: solid 1px $grey-lighten-3;
		padding-top: 1.5rem;

		h3 {
			font-size: size(18);
		}
	}
}
</style>
