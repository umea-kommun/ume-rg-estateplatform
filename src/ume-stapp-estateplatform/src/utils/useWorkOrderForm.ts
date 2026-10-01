import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useStore } from 'vuex';
import { DispatchType } from '@/models/Enums';
import {
	IBuildingDetails,
	IBuildingRoom,
	IRootState,
} from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';
import { useServerValidation } from '@/utils/useServerValidation';
import { useWorkOrderConfig } from '@/utils/useWorkOrderConfig';

/**
 * Shared plumbing for the estate work-order forms (fault report, order and space
 * requirement). All three are surfaces of one domain concept - a work order:
 * a building, an optional room, a description with attachments and contact
 * details, submitted to the estate service (order and space requirement even
 * share the `ISubmitEstateOrder` payload). This composable owns that shared form
 * plumbing - selection refs, contact prefill, upload limits, server validation,
 * query-param sync and the submit lifecycle.
 *
 * It deliberately owns no step/rail structure: how the steps are laid out (the
 * numbered rail in fault report and order, the opt-in sections in space
 * requirement) stays in each page, as does the scroll wiring (`useStepScroll`).
 * Keep this API narrow - push flow-specific behaviour into the caller via the
 * handler callbacks rather than adding flags here.
 */
export const useWorkOrderForm = () => {
	const route = useRoute();
	const router = useRouter();
	const store = useStore<IRootState>();

	const selectedBuilding = ref<IBuildingDetails | null>(null);
	const selectedRoom = ref<IBuildingRoom | null>(null);
	const skippedRoom = ref(false);

	const isLoadingFromQuery = ref(false);
	const isBusySubmitting = ref(false);
	const hasSubmitted = ref(false);

	const user = computed(() => store.state.user);

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

	// Contact details default to the signed-in user; they remain editable.
	const contactName = ref(user.value?.fullName ?? '');
	const contactEmail = ref(user.value?.email ?? '');
	const contactPhone = ref('');

	// Editing any submitted field clears the matching server-side error so stale
	// messages don't linger while the user fixes them.
	watch(
		[
			problemDescription,
			attachments,
			contactName,
			contactEmail,
			contactPhone,
		],
		clearServerErrors,
		{ deep: true }
	);

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

	/**
	 * Restores a deep-linked building (and room) from the query string, handing
	 * each fetched entity to the caller's own select handler so flow-specific
	 * resets and scrolling still run. `?submitted=true` short-circuits to the
	 * completed view. Failures are swallowed (logged) - the user just re-selects.
	 */
	const loadFromQueryParams = async (handlers: {
		onBuilding: (building: IBuildingDetails | null) => void | Promise<void>;
		onRoom?: (room: IBuildingRoom | null) => void | Promise<void>;
		errorContext: string;
	}) => {
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
					{ buildingId }
				);
				await handlers.onBuilding(building ?? null);

				if (building && roomId && handlers.onRoom) {
					const room = await store.dispatch(
						DispatchType.GetRoomById,
						{
							roomId,
						}
					);
					await handlers.onRoom(room);
				}
			} catch (err) {
				ErrorService.onError({
					err,
					hidden: true,
					message: handlers.errorContext,
				});
			}
		}

		isLoadingFromQuery.value = false;
	};

	/**
	 * Runs the submit lifecycle: busy flag, dispatch, and on success the completed
	 * view (+ query sync + scroll to top). The caller validates and builds the
	 * payload first; server-side field errors are mapped back, otherwise
	 * `unableMessage` is surfaced. Returns whether the submit succeeded.
	 */
	const submit = async <TPayload>(
		payload: TPayload,
		dispatchType: DispatchType,
		unableMessage: string
	): Promise<boolean> => {
		isBusySubmitting.value = true;
		try {
			clearServerErrors();
			await store.dispatch(dispatchType, payload);

			hasSubmitted.value = true;
			updateQueryParams();
			window.scrollTo({ top: 0 });
			return true;
		} catch (err) {
			if (!setFromError(err)) {
				ErrorService.onError({ err, message: unableMessage });
			}
			return false;
		} finally {
			isBusySubmitting.value = false;
		}
	};

	return {
		// Selection state
		selectedBuilding,
		selectedRoom,
		skippedRoom,
		// Page/loading state
		isLoadingFromQuery,
		isBusySubmitting,
		hasSubmitted,
		user,
		// Description + attachments
		problemDescription,
		attachments,
		uploadMaxFiles,
		uploadMaxSizeMb,
		uploadAccept,
		// Server validation
		serverErrors,
		fileServerErrors,
		fieldError,
		descriptionServerError,
		setFromError,
		clearServerErrors,
		// Contact
		contactName,
		contactEmail,
		contactPhone,
		// Behaviour
		updateQueryParams,
		loadFromQueryParams,
		submit,
	};
};
