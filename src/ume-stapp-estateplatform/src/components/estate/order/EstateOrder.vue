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
						:title="$t('component.order.stepTitle.building')"
						:step="stepNumber('building')"
						:step-count="stepCount"
						:state="orderStepState('building')"
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
					</estate-order-step>

					<!-- CATEGORY SELECTOR -->
					<estate-order-step
						:title="$t('component.order.category.title')"
						:step="stepNumber('category')"
						:step-count="stepCount"
						:state="orderStepState('category')"
						ref="categoryTitle"
						rail
					>
						<order-category-selector
							v-if="availableCategories.length > 0"
							:available-categories="availableCategories"
							:category="selectedCategory"
							@select="selectCategory"
						/>
						<!-- No orderable services for this building: a dead-end the
						user sees explained, like the fault-report no-room-info step. -->
						<v-alert
							v-else
							type="info"
							variant="tonal"
							rounded="lg"
							class="mt-2"
						>
							{{ $t('component.order.category.noneAvailable') }}
						</v-alert>
					</estate-order-step>

					<!-- INDOOR / OUTDOOR - only for Byggservice, where an outdoor
					order needs no room. Other order types skip straight to the room
					step, so this step isn't rendered (and the numbering closes up). -->
					<estate-order-step
						v-if="hasLocationStep"
						:title="$t('component.order.stepTitle.location')"
						:step="stepNumber('location')"
						:step-count="stepCount"
						:state="orderStepState('location')"
						ref="locationTitle"
						rail
					>
						<p
							v-if="!problemLocation"
							class="text-medium-emphasis mt-2"
						>
							{{ $t('component.order.location.select') }}
						</p>
						<location-selector
							:options="orderLocationOptions"
							:selected="problemLocation"
							@select="selectLocation"
						/>
					</estate-order-step>

					<!-- ROOM SELECTOR -->
					<estate-order-step
						:title="$t('component.order.stepTitle.room')"
						:step="stepNumber('room')"
						:step-count="stepCount"
						:state="orderStepState('room')"
						ref="roomTitle"
						rail
					>
						<!-- Byggservice outdoor: no room to pick, resolved with an
						explanation like the fault-report outdoor room step. -->
						<selection-card
							v-if="isOutdoor"
							class="mt-2"
							muted
							icon="no_meeting_room"
							:title="$t('component.roomSelector.noneTitle')"
							:description="
								$t('component.order.room.noneOutdoor')
							"
						/>
						<!-- Building has no room breakdown: the step is resolved with
						an explanation instead of asking for a room. -->
						<selection-card
							v-else-if="roomInfoUnavailable"
							class="mt-2"
							muted
							icon="no_meeting_room"
							:title="$t('component.order.room.noInfoTitle')"
							:description="$t('component.order.room.noInfo')"
						/>
						<room-selector
							v-else-if="selectedBuilding"
							class="mt-2"
							:building="selectedBuilding"
							:skipped-room="skippedRoom"
							:selected-room="selectedRoom"
							@select="selectRoom"
							@skip="selectRoom(null, true)"
						/>
					</estate-order-step>

					<!-- DESCRIPTION + CONTACT (final step) -->
					<vee-form
						ref="formValidator"
						v-slot="{ errors }"
						@submit.prevent="submitReport"
					>
						<!-- Description, attachments and the (pre-filled) contact
						details together, so the rail stays one-step-at-a-time to
						submit - matching the fault report. -->
						<estate-order-step
							:title="
								$t('component.order.general.descriptionTitle')
							"
							:step="stepNumber('description')"
							:step-count="stepCount"
							:state="orderStepState('description')"
							ref="problemTitle"
							rail
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

							<!-- Contact details: a labelled subsection rather than a
							separate step, since they are pre-filled from the user. -->
							<div class="contact-section mt-8">
								<h3 class="ma-0 mb-1">
									{{
										$t(
											'component.order.general.contactLabel'
										)
									}}
								</h3>
								<p class="text-medium-emphasis mt-0">
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
							</div>
						</estate-order-step>
						<v-alert
							v-if="showLastSteps && Object.keys(errors).length"
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
					<p
						v-for="(paragraph, index) in infoParagraphs"
						:key="index"
					>
						{{ paragraph }}
					</p>
					<p
						v-html="$t('component.order.info.responsibilityLink')"
					></p>
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
import { computed, onMounted, ref, useTemplateRef, watch } from 'vue';
import { EstateRoutes } from '@/router/routes';
import NavBreadcrumbs from '../../shared/NavBreadcrumbs.vue';
import { useI18n } from 'vue-i18n';
import {
	EstateOrderCategory,
	DispatchType,
	EstateFaultLocation,
} from '@/models/Enums';
import BaseFileUpload from '@/components/shared/BaseFileUpload.vue';
import { Form as VeeForm } from 'vee-validate';
import BaseTextBox from '@/components/shared/BaseTextBox.vue';
import { useWorkOrderForm } from '@/utils/useWorkOrderForm';
import { useStepScroll } from '@/utils/useStepScroll';
import BuildingSelector from '../faultReport/buildingSelector/BuildingSelector.vue';
import RoomSelector from '../faultReport/roomSelector/RoomSelector.vue';
import EstateOrderCompleted from './EstateOrderCompleted.vue';
import OrderCategorySelector from './OrderCategorySelector.vue';
import LocationSelector from './LocationSelector.vue';
import type { LocationOption } from './LocationSelector.vue';
import SelectionCard from './SelectionCard.vue';
import FaultContactInfo from '../faultReport/FaultContactInfo.vue';
import EstateOrderStep from './EstateOrderStep.vue';
import BuildingMapSelector from '../faultReport/buildingSelector/BuildingMapSelector.vue';

const { t, locale, tm } = useI18n();

const breadcrumbs = computed(() => {
	if (!locale.value) return [];
	return [
		{
			title: t('component.order.title'),
			to: { name: EstateRoutes.Order },
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
const categoryTitleRef = useTemplateRef('categoryTitle');
const locationTitleRef = useTemplateRef('locationTitle');
const roomTitleRef = useTemplateRef('roomTitle');
const problemTitleRef = useTemplateRef('problemTitle');

const formValidator = useTemplateRef('formValidator');

// Order type and (for Byggservice) the indoor/outdoor location are this flow's own
// state; building and room come from the shared work-order form.
const selectedCategory = ref<EstateOrderCategory | null>(null);
const problemLocation = ref<EstateFaultLocation | null>(null);

// Only Byggservice distinguishes indoor/outdoor: outdoor orders need no room, so
// the room step is resolved without a pick. The other order types go straight to
// the room step, so the location step isn't shown for them.
const hasLocationStep = computed(
	() => selectedCategory.value === EstateOrderCategory.BuildingService
);
const isOutdoor = computed(
	() =>
		hasLocationStep.value &&
		problemLocation.value === EstateFaultLocation.Outdoor
);

// The building has no room breakdown, so the room step is resolved without a pick
// (explained in place), like the fault-report no-room-info step.
const roomInfoUnavailable = computed(
	() => selectedBuilding.value?.hasRoomInformation === false
);

// The room step counts as answered when there's nothing to pick (outdoor order or
// a building without room info) or the user has picked/skipped a room.
const roomResolved = computed(
	() =>
		isOutdoor.value ||
		roomInfoUnavailable.value ||
		!!selectedRoom.value ||
		skippedRoom.value
);

const orderLocationOptions = computed<LocationOption[]>(() => [
	{
		value: EstateFaultLocation.Indoor,
		icon: 'meeting_room',
		title: t('component.order.location.indoor.select'),
		description: t('component.order.location.indoor.description'),
		selected: t('component.order.location.indoor.selected'),
	},
	{
		value: EstateFaultLocation.Outdoor,
		icon: 'park',
		title: t('component.order.location.outdoor.select'),
		description: t('component.order.location.outdoor.description'),
		selected: t('component.order.location.outdoor.selected'),
	},
]);

watch(
	() => selectedBuilding.value,
	(_, oldVal) => {
		if (oldVal) {
			selectedCategory.value = null;
			problemLocation.value = null;
			selectedRoom.value = null;
			skippedRoom.value = false;
		}
	}
);
// A building without room info resolves the room step in place, so a room carried
// in from a deep link or favourite would otherwise stay selected and be submitted
// behind the "no room information" card. Clear it so what's shown is what's sent.
// Watch both conditions together since the room may be set after the building.
watch(
	() => roomInfoUnavailable.value && !!selectedRoom.value,
	(hasOrphanedRoom) => {
		if (hasOrphanedRoom) {
			selectedRoom.value = null;
		}
	},
	{ immediate: true }
);

// Only the three service types are ordered here - SpaceRequirement has its own
// flow. A category counts as available when the API marks it enabled for this
// building; disabled and absent both mean it cannot be ordered.
const ORDER_CATEGORIES = [
	EstateOrderCategory.BuildingService,
	EstateOrderCategory.TownHallService,
	EstateOrderCategory.FacilityService,
];

const availableCategories = computed(() => {
	const access = selectedBuilding.value?.workOrderTypeAccess ?? {};
	return ORDER_CATEGORIES.filter(
		(category) => access[category] === 'enabled'
	);
});

// The info box follows the chosen order type - a general text until a
// category is picked, then the text for that specific service. Categories
// without their own text fall back to the general one. Note that te()
// reports false for array messages, so the presence check has to be made
// on what tm() actually returns - it yields {} for a missing key.
const infoParagraphs = computed<string[]>(() => {
	const key = `component.order.info.${selectedCategory.value ?? 'general'}`;
	const paragraphs = tm(key) as unknown;
	return (
		Array.isArray(paragraphs) && paragraphs.length > 0
			? paragraphs
			: tm('component.order.info.general')
	) as string[];
});

const showLastSteps = computed(() => {
	if (!selectedBuilding.value || !selectedCategory.value) return false;
	// Byggservice must answer indoor/outdoor before the room step is reached.
	if (hasLocationStep.value && !problemLocation.value) return false;
	return roomResolved.value;
});

const selectBuilding = async (building: IBuildingDetails | null) => {
	selectedRoom.value = null;
	skippedRoom.value = false;
	selectedCategory.value = null;
	problemLocation.value = null;
	selectedBuilding.value = building;

	// Scroll to the next step on selection, or back to this step's title when
	// cleared so it isn't left hidden behind the sticky header.
	scrollToStep(building ? categoryTitleRef : buildingTitleRef);
	updateQueryParams();
};

const selectCategory = async (category: EstateOrderCategory | null) => {
	const previousCategory = selectedCategory.value;
	selectedCategory.value = category;

	// Changing the order type restarts the downstream steps - but only when an
	// existing choice is actually being replaced. On the first (mandatory) category
	// selection there is nothing to restart, and clearing here would discard a room
	// pre-picked from a favourite or restored from ?buildingId=&roomId=.
	if (previousCategory !== null && previousCategory !== category) {
		problemLocation.value = null;
		selectedRoom.value = null;
		skippedRoom.value = false;
	} else if (
		category === EstateOrderCategory.BuildingService &&
		selectedRoom.value
	) {
		// Byggservice adds an indoor/outdoor step before the room, but a room can
		// only be indoors - so a room pre-picked from a favourite or the query
		// implies Indoor. Resolve the location step to Indoor rather than stranding
		// the room behind an empty step (which selectLocation would then clear).
		problemLocation.value = EstateFaultLocation.Indoor;
	}

	// Byggservice asks indoor/outdoor next; the others go straight to the room.
	// When that location step is already resolved (auto-Indoor for a pre-picked
	// room above), skip past it to the room instead of landing on a done step.
	const next =
		hasLocationStep.value && !problemLocation.value
			? locationTitleRef
			: roomTitleRef;
	scrollToStep(category ? next : categoryTitleRef);
};

const selectLocation = async (location: EstateFaultLocation | null) => {
	selectedRoom.value = null;
	skippedRoom.value = false;
	problemLocation.value = location;

	// Always land on the room step: indoor to pick a room, outdoor to see it
	// resolved (no room) with the problem step right below.
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
	selectedRoom.value = room;
	skippedRoom.value = false;
	// Category is still required and comes before the room, so land on it next;
	// the pre-picked room shows filled in once a category is chosen.
	scrollToStep(categoryTitleRef);
	updateQueryParams();
};

// Building, order type, (Byggservice only) location, room and a final
// description-and-contact step. The location step is present only for Byggservice,
// so the count and numbers derive from the steps actually shown rather than being
// hardcoded - the rail closes up for the other order types.
type OrderStep = 'building' | 'category' | 'location' | 'room' | 'description';

const steps = computed<OrderStep[]>(() => [
	'building',
	'category',
	...(hasLocationStep.value ? (['location'] as const) : []),
	'room',
	'description',
]);
const stepCount = computed(() => steps.value.length);
const stepNumber = (step: OrderStep) => steps.value.indexOf(step) + 1;

const orderStepState = (
	step: OrderStep
): 'completed' | 'current' | 'skipped' | 'upcoming' => {
	switch (step) {
		case 'building':
			return selectedBuilding.value ? 'completed' : 'current';
		case 'category':
			if (!selectedBuilding.value) return 'upcoming';
			// No orderable services for this building is a dead end: keep the
			// step current so its explanation stays visible.
			if (availableCategories.value.length === 0) return 'current';
			return selectedCategory.value ? 'completed' : 'current';
		case 'location':
			if (!selectedCategory.value) return 'upcoming';
			return problemLocation.value ? 'completed' : 'current';
		case 'room':
			if (!selectedCategory.value) return 'upcoming';
			// Byggservice must answer indoor/outdoor first.
			if (hasLocationStep.value && !problemLocation.value)
				return 'upcoming';
			// An outdoor order, or a building without room info, has no room to
			// pick - the step is resolved rather than answered.
			if (isOutdoor.value || roomInfoUnavailable.value) return 'skipped';
			if (selectedRoom.value) return 'completed';
			if (skippedRoom.value) return 'skipped';
			return 'current';
		case 'description':
			return showLastSteps.value ? 'current' : 'upcoming';
		default:
			return 'upcoming';
	}
};

const submitReport = async () => {
	const validationResult = await formValidator.value?.validate();
	if (
		!selectedBuilding.value ||
		!selectedCategory.value ||
		(hasLocationStep.value && !problemLocation.value) ||
		!roomResolved.value ||
		!validationResult?.valid
	) {
		return;
	}

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

	await submit(
		reportData,
		DispatchType.SubmitEstateOrder,
		t('app.error.estate.unableToSubmitOrder')
	);
};

onMounted(() => {
	loadFromQueryParams({
		onBuilding: (building) => selectBuilding(building),
		onRoom: (room) => selectRoom(room),
		errorContext:
			'Failed to load building from query params on order page, user have to manually select',
	});
});
</script>

<style scoped lang="scss">
.estate-order {
	:deep(.v-container) {
		padding-top: 1rem;
	}

	// Layout comes from the shared .content-wrap skeleton in estate.scss.
	.content-wrap .order-wrap {
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
