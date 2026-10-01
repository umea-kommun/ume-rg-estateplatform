import type { WorkOrderListItem } from '@/utils/useWorkOrders';

export const workOrder = (
	overrides: Partial<WorkOrderListItem> = {}
): WorkOrderListItem => ({
	id: 'one',
	workOrderType: 'errorReport',
	workOrderNumber: null,
	buildingId: null,
	buildingName: 'Stadshuset',
	buildingPopularName: null,
	buildingImageUrl: null,
	roomName: null,
	location: null,
	description: 'Trasig dörr',
	syncStatus: 'Submitted',
	status: null,
	statusCategory: null,
	displayStatus: null,
	statusCheckedAt: null,
	performedDescription: null,
	performedDescriptionAt: null,
	createdAt: '2026-09-16T10:00:00Z',
	submittedAt: null,
	lastChangedAt: '2026-09-16T10:00:00Z',
	...overrides,
});
