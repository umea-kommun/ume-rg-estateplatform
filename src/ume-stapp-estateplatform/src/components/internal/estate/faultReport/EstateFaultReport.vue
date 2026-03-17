<template>
	<app-content
		class="estate-default estate-fault-report"
		:pageTitle="$t('component.appHeader.title.internalEstateFaultReport')"
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
						<h1>
							{{ $t('component.internal.faultReport.title') }}
						</h1>
						<p>
							{{
								$t('component.internal.faultReport.description')
							}}
						</p>
					</div>

					<!-- BUILDING SELECTOR -->
					<div class="choice-wrap">
						<div class="choice-header">
							<div class="choice-title">
								<h2>
									{{
										selectedBuilding
											? $t(
													'component.internal.faultReport.building.selected'
											  )
											: $t(
													'component.internal.faultReport.building.select'
											  )
									}}
								</h2>
								<v-btn
									v-if="selectedBuilding"
									@click="selectBuilding(null)"
									rounded="xl"
									variant="tonal"
									size="small"
									color="grey-darken-2"
									class="regular-text ma-0 ml-2"
								>
									{{
										$t(
											'component.internal.faultReport.changeAnswer'
										)
									}}
								</v-btn>
							</div>
							<div class="choice-step">1/{{ stepCount }}</div>
						</div>
						<building-selector
							:selected-building="selectedBuilding"
							@select="selectBuilding"
							@select-room="selectBuildingAndRoom"
						/>
					</div>

					<!-- OUTDOOR / INDOOR SELECTOR -->
					<div v-if="selectedBuilding" class="mt-6 choice-wrap">
						<div class="choice-header">
							<div class="choice-title">
								<h2 ref="locationTitle">
									<span
										v-if="
											problemLocation ===
											EstateFaultLocation.Indoor
										"
									>
										{{
											$t(
												'component.internal.faultReport.location.indoor.title'
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
												'component.internal.faultReport.location.outdoor.title'
											)
										}}
									</span>
									<span v-else>
										{{
											$t(
												'component.internal.faultReport.location.select'
											)
										}}
									</span>
								</h2>
								<v-btn
									v-if="problemLocation"
									@click="selectLocation(null)"
									rounded="xl"
									variant="tonal"
									size="small"
									color="grey-darken-2"
									class="regular-text ma-0 ml-2"
								>
									{{
										$t(
											'component.internal.faultReport.changeAnswer'
										)
									}}
								</v-btn>
							</div>
							<div class="choice-step">2/{{ stepCount }}</div>
						</div>
						<fault-location-selector
							:problem-location="problemLocation"
							@select="selectLocation"
						/>
					</div>

					<!-- ROOM SELECTOR -->
					<div
						v-if="
							problemLocation === EstateFaultLocation.Indoor &&
							selectedBuilding
						"
						class="mt-6 choice-wrap"
					>
						<div class="choice-header">
							<div class="choice-title">
								<h2 ref="roomTitle">
									<span v-if="selectedRoom && !skippedRoom">
										{{
											$t(
												'component.internal.faultReport.room.selected'
											)
										}}
									</span>
									<span
										v-else-if="!selectedRoom && skippedRoom"
									>
										{{
											$t(
												'component.internal.faultReport.room.none'
											)
										}}
									</span>
									<span v-else>
										{{
											$t(
												'component.internal.faultReport.room.select'
											)
										}}
									</span>
								</h2>
								<v-btn
									v-if="selectedRoom || skippedRoom"
									@click="selectRoom(null)"
									rounded="xl"
									variant="tonal"
									size="small"
									color="grey-darken-2"
									class="regular-text"
								>
									{{
										$t(
											'component.internal.faultReport.changeAnswer'
										)
									}}
								</v-btn>
							</div>

							<div class="choice-step">
								<v-btn
									v-if="!selectedRoom && !skippedRoom"
									@click="selectRoom(null, true)"
									rounded="xl"
									variant="tonal"
									color="grey-darken-2"
									class="regular-text"
								>
									{{
										$t(
											'component.internal.faultReport.room.skip'
										)
									}}
								</v-btn>
								3/{{ stepCount }}
							</div>
						</div>
						<room-selector
							:building="selectedBuilding"
							:skipped-room="skippedRoom"
							:selected-room="selectedRoom"
							@select="selectRoom"
						/>
					</div>

					<!-- PROBLEM DESCRIPTION -->
					<vee-form
						v-if="showLastSteps"
						ref="formValidator"
						v-slot="{ errors }"
						@submit.prevent="submitReport"
					>
						<div class="mt-6 choice-wrap">
							<div class="choice-header">
								<div class="choice-title">
									<h2
										ref="problemTitle"
										id="problem-description-label"
									>
										{{
											$t(
												'component.internal.faultReport.general.problemTitle'
											)
										}}
									</h2>
								</div>
								<div class="choice-step">
									{{ stepCount - 1 }}/{{ stepCount }}
								</div>
							</div>
							<base-text-box
								id="problem-description"
								:label="
									$t(
										'component.internal.faultReport.general.problemLabel'
									)
								"
								v-model="problemDescription"
								rules="required"
								variant="outlined"
								rounded="lg"
								aria-labelledby="problem-description-label"
								text-area
								auto-grow
							/>
							<p class="text-medium-emphasis">
								{{
									$t(
										'component.internal.faultReport.general.problemHelpText'
									)
								}}
							</p>

							<base-file-upload
								v-model="attachments"
								accept=".pdf,image/*"
								:max-files="10"
								:max-size-mega-bytes="10"
								class="mt-6"
							/>
						</div>

						<!-- CONTACT INFORMATION -->
						<div class="mt-6 choice-wrap">
							<div class="choice-header">
								<div class="choice-title">
									<h2>
										{{
											$t(
												'component.internal.faultReport.general.contactLabel'
											)
										}}
									</h2>
								</div>
								<div class="choice-step">
									{{ stepCount }}/{{ stepCount }}
								</div>
							</div>

							<p class="text-medium-emphasis">
								{{
									$t(
										'component.internal.faultReport.general.contactHelpText'
									)
								}}
							</p>
							<fault-contact-info
								v-model:contactName="contactName"
								v-model:contactEmail="contactEmail"
								v-model:contactPhone="contactPhone"
							/>
						</div>
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

						<div
							class="d-flex align-center justify-center pa-4 mt-4"
						>
							<v-btn
								color="primary"
								class="regular-text"
								size="large"
								rounded="lg"
								:disabled="isBusySubmitting"
								:loading="isBusySubmitting"
								@click="submitReport"
							>
								{{
									$t(
										'component.internal.faultReport.submitButton'
									)
								}}
							</v-btn>
						</div>
					</vee-form>
				</div>
			</div>
			<div class="info-wrap">
				<v-alert rounded="lg">
					<h2>
						{{ $t('component.internal.faultReport.info.title') }}
					</h2>
					<p>
						{{ $t('component.internal.faultReport.info.text1') }}
					</p>
					<p class="mt-4">
						{{ $t('component.internal.faultReport.info.text2') }}
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
	ISubmitEstateFaultReport,
} from '@/models/estate/Interfaces';
import {
	computed,
	nextTick,
	onMounted,
	Ref,
	ref,
	useTemplateRef,
	watch,
} from 'vue';
import { EstateRoutes, MyPagesRoutes } from '@/router/routes';
import NavBreadcrumbs from '../../shared/NavBreadcrumbs.vue';
import { useI18n } from 'vue-i18n';
import { EstateFaultLocation } from '@/models/estate/Enums';
import BuildingSelector from './buildingSelector/BuildingSelector.vue';
import RoomSelector from './roomSelector/RoomSelector.vue';
import { useRoute, useRouter } from 'vue-router';
import { DispatchType } from '@/models/Enums';
import { useStore } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import BaseFileUpload from '@/components/base/BaseFileUpload.vue';
import FaultLocationSelector from './FaultLocationSelector.vue';
import EstateFaultReportCompleted from './EstateFaultReportCompleted.vue';
import { Form as VeeForm } from 'vee-validate';
import BaseTextBox from '@/components/base/BaseTextBox.vue';
import ErrorService from '@/utils/ErrorService';
import FaultContactInfo from './FaultContactInfo.vue';

const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const store = useStore<IRootState>();

const breadcrumbs = [
	{
		title: t('app.nav.home'),
		to: { name: MyPagesRoutes.InternalStart },
	},
	{
		title: t('component.internal.faultReport.title'),
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

const user = computed(() => store.state.user);

const formValidator = useTemplateRef('formValidator');
const problemDescription = ref('');
const attachments = ref<File[]>([]);
const contactName = ref(user.value?.fullName ?? '');
const contactEmail = ref(user.value?.email ?? '');
const contactPhone = ref('');

watch(
	() => selectedBuilding.value,
	(_, oldVal) => {
		if (oldVal) {
			problemLocation.value = null;
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

const scrollToAfterUiUpdate = async (elRef: Ref<HTMLElement | null>) => {
	await nextTick();
	setTimeout(() => {
		elRef.value?.scrollIntoView({
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
				const room = await store.dispatch(
					DispatchType.GetBuildingRoomById,
					{
						roomId,
					}
				);
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
		await store.dispatch(DispatchType.SubmitFaultReport, reportData);

		hasSubmitted.value = true;
		updateQueryParams();
		window.scrollTo({ top: 0 });
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

	.choice-wrap {
		padding-bottom: 24px;
		border-bottom: solid 1px #f2f2f2;

		.choice-header {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: 16px;

			.choice-title {
				display: flex;
				align-items: center;
			}
			.choice-step {
				font-size: size(14);
				color: $grey-darken-1;
			}
		}
	}

	.content-wrap {
		display: flex;
		flex-wrap: wrap;
		gap: 2rem;
		justify-content: space-between;

		.report-wrap {
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
			.report-wrap {
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
