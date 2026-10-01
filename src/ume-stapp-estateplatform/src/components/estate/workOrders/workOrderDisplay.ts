import type { WorkOrderListItem } from '@/utils/useWorkOrders';

const typeByWorkOrderType: Record<string, string> = {
	errorReport: 'faultReport',
	buildingService: 'order',
	facilityService: 'order',
	townHallService: 'order',
	spaceRequirement: 'spaceRequirement',
};
const iconByType: Record<string, string> = {
	faultReport: 'warning',
	order: 'handyman',
	spaceRequirement: 'space_dashboard',
};

/** Portal action key (faultReport, order, spaceRequirement), or null for unknown types. */
export const workOrderTypeKey = (order: WorkOrderListItem) =>
	typeByWorkOrderType[order.workOrderType ?? ''] ?? null;

export const workOrderTypeIcon = (order: WorkOrderListItem) =>
	iconByType[workOrderTypeKey(order) ?? ''] ?? 'assignment';

// Until Pythagoras has answered, including failed sends, the order is just "sent".
// A dismissed order never reaches Pythagoras, so it reads as closed.
export const workOrderStatusKey = (order: WorkOrderListItem) =>
	order.displayStatus ??
	(order.syncStatus === 'Dismissed' ? 'closed' : 'sent');

export const isClosedWorkOrder = (order: WorkOrderListItem) =>
	workOrderStatusKey(order) === 'closed';
