import Config from '@/Config';
import { ActionContext } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import Axios from 'axios';
import mapper from './mapper';
import ErrorService from '@/utils/ErrorService';
import {
	ISubmitEstateFaultReport,
	ISubmitEstateOrder,
	SearchFilter,
} from '@/models/estate/Interfaces';

const httpClient = Axios.create({
	baseURL: Config.VUE_APP_ESTATE_SERVICE,
});

const buildQueryParams = (params: {
	query?: string;
	searchFilter?: SearchFilter;
	type?: string[];
	limit?: number;
}): URLSearchParams => {
	const queryParams = new URLSearchParams();
	if (params.query) {
		queryParams.append('query', params.query);
	}
	if (params.type) {
		params.type.forEach((type) => {
			queryParams.append('type', type);
		});
	}
	if (params.limit) {
		queryParams.append('limit', params.limit.toString());
	}

	if (params.searchFilter?.businessTypes) {
		params.searchFilter.businessTypes.forEach((typeId) => {
			queryParams.append('businessTypeId', typeId.toString());
		});
	}

	return queryParams;
};

export default {
	async getEstateSearch(
		context: ActionContext<IRootState, IRootState>,
		{
			params,
			abortController,
		}: {
			params: {
				query: string;
				searchFilter?: SearchFilter;
			};
			abortController: AbortController;
		}
	) {
		const queryParams = buildQueryParams(params);
		queryParams.append('limit', '50');

		const response = await httpClient.get(
			'/search?' + queryParams.toString(),
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
				signal: abortController.signal,
			}
		);

		return mapper.mapResponseToEstateSearchResult(response.data);
	},
	async getEstateSearchGeoLocations(
		context: ActionContext<IRootState, IRootState>,
		{
			params,
			abortController,
		}: {
			params: {
				query: string;
				searchFilter?: SearchFilter;
			};
			abortController?: AbortController;
		}
	) {
		const queryParams = buildQueryParams(params);

		const response = await httpClient.get(
			'/search/geolocations?' + queryParams.toString(),
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
				signal: abortController?.signal,
			}
		);

		return mapper.mapResponseToEstateSearchResultGeoLocations(
			response.data
		);
	},
	async getEstateById(
		context: ActionContext<IRootState, IRootState>,
		{ estateId }: { estateId: string }
	) {
		try {
			const response = await httpClient.get(`/estates/${estateId}`, {
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			});

			return mapper.mapResponseToEstateDetails(response.data);
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},
	async getEstateBuildings(
		context: ActionContext<IRootState, IRootState>,
		{ estateId }: { estateId: string }
	) {
		const response = await httpClient.get(
			`/estates/${estateId}/buildings`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return mapper.mapResponseToEstateBuildings(response.data);
	},
	async getBuildingLocations(context: ActionContext<IRootState, IRootState>) {
		try {
			const response = await httpClient.get('/buildings/geolocations', {
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			});

			return mapper.mapResponseToBuildingLocations(response.data);
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},
	async getBuildingById(
		context: ActionContext<IRootState, IRootState>,
		{ buildingId }: { buildingId: string }
	) {
		const response = await httpClient.get(`/buildings/${buildingId}`, {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		return mapper.mapResponseToBuildingDetails(response.data);
	},
	async getBuildingFloors(
		context: ActionContext<IRootState, IRootState>,
		{
			buildingId,
			includeRooms,
		}: { buildingId: string; includeRooms: boolean }
	) {
		const response = await httpClient.get(
			`/buildings/${buildingId}/floors?includeRooms=${includeRooms}`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return mapper.mapResponseToBuildingFloors(response.data);
	},
	async getBuildingRooms(
		context: ActionContext<IRootState, IRootState>,
		{ buildingId }: { buildingId: string }
	) {
		const response = await httpClient.get(
			`/buildings/${buildingId}/rooms?limit=-1`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return mapper.mapResponseToBuildingRooms(response.data);
	},
	async getBuildingRoomById(
		context: ActionContext<IRootState, IRootState>,
		{ roomId }: { roomId: number }
	) {
		const response = await httpClient.get(`/rooms/${roomId}`, {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		return mapper.mapResponseToBuildingRoom(response.data);
	},
	async getFloorBlueprint(
		context: ActionContext<IRootState, IRootState>,
		{
			floorId,
			abortController,
		}: { floorId: string; abortController: AbortController }
	) {
		const response = await httpClient.get(
			`/floors/${floorId}/blueprint?format=svg`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
				signal: abortController?.signal,
			}
		);

		return response.data;
	},
	async getRoomById(
		context: ActionContext<IRootState, IRootState>,
		{ roomId }: { roomId: string }
	) {
		const response = await httpClient.get(`/rooms/${roomId}`, {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		return mapper.mapResponseToRoomDetails(response.data);
	},
	async getBusinessTypes(context: ActionContext<IRootState, IRootState>) {
		const response = await httpClient.get('/businessTypes', {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		return mapper.mapResponseToBusinessTypes(response.data);
	},
	async getBuildingDocuments(
		context: ActionContext<IRootState, IRootState>,
		{ buildingId }: { buildingId: number }
	) {
		const response = await httpClient.get(
			`/documents/building/${buildingId}/tree`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return mapper.mapResponseToBuildingDocuments(response.data);
	},
	async downloadBuildingDocument(
		context: ActionContext<IRootState, IRootState>,
		{
			buildingId,
			directoryId,
			documentId,
		}: {
			buildingId: number;
			directoryId: number;
			documentId: number;
		}
	) {
		const response = await httpClient.get(
			`/documents/building/${buildingId}/directory/${directoryId}/download/${documentId}`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
				responseType: 'blob',
			}
		);

		return response.data;
	},
	async submitFaultReport(
		context: ActionContext<IRootState, IRootState>,
		reportData: ISubmitEstateFaultReport
	) {
		const formData = new FormData();
		formData.append('buildingId', reportData.buildingId.toString());
		formData.append('location', reportData.location);
		formData.append('workOrderType', 'errorReport');
		formData.append('description', reportData.description);
		formData.append('notifierName', reportData.notifierName);
		formData.append('notifierEmail', reportData.notifierEmail);

		if (reportData.roomId) {
			formData.append('roomId', reportData.roomId.toString());
		}
		if (reportData.notifierPhone) {
			formData.append('notifierPhone', reportData.notifierPhone);
		}

		reportData.attachments.forEach((file) => {
			formData.append('files', file, file.name);
		});

		const response = await httpClient.post('/workorders', formData, {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
				'Content-Type': 'multipart/form-data',
			},
		});

		return response.data;
	},
	async submitEstateOrder(
		context: ActionContext<IRootState, IRootState>,
		reportData: ISubmitEstateOrder
	) {
		const formData = new FormData();
		formData.append('buildingId', reportData.buildingId.toString());
		formData.append('workOrderType', reportData.category);
		formData.append('description', reportData.description);
		formData.append('notifierName', reportData.notifierName);
		formData.append('notifierEmail', reportData.notifierEmail);

		if (reportData.roomId) {
			formData.append('roomId', reportData.roomId.toString());
		}
		if (reportData.notifierPhone) {
			formData.append('notifierPhone', reportData.notifierPhone);
		}

		reportData.attachments.forEach((file) => {
			formData.append('files', file, file.name);
		});

		const response = await httpClient.post('/workorders', formData, {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
				'Content-Type': 'multipart/form-data',
			},
		});

		return response.data;
	},

	async setFavorite(
		context: ActionContext<IRootState, IRootState>,
		{ id, type }: { id: number; type: string }
	) {
		await httpClient.put(`/favorites/${type}/${id}`, null, {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});
	},
	async unsetFavorite(
		context: ActionContext<IRootState, IRootState>,
		{ id, type }: { id: number; type: string }
	) {
		await httpClient.delete(`/favorites/${type}/${id}`, {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});
	},
	async getFavorites(context: ActionContext<IRootState, IRootState>) {
		const response = await httpClient.get('/favorites', {
			headers: {
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		return mapper.mapResponseToEstateSearchResult(response.data);
	},
};
