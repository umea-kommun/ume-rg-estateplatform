import Config from '@/Config';
import { ActionContext } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import Axios from 'axios';
import mapper from './mapper';
import ErrorService from '@/utils/ErrorService';
import { SearchFilter } from '@/models/estate/Interfaces';

const httpClient = Axios.create();

const buildQueryParams = (params: {
	query?: string;
	searchFilter?: SearchFilter;
}): URLSearchParams => {
	const queryParams = new URLSearchParams();
	if (params.query) {
		queryParams.append('query', params.query);
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
			Config.VUE_APP_ESTATE_SERVICE + '/search?' + queryParams.toString(),
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
			Config.VUE_APP_ESTATE_SERVICE +
				'/search/geolocations?' +
				queryParams.toString(),
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
			const response = await httpClient.get(
				Config.VUE_APP_ESTATE_SERVICE + `/estates/${estateId}`,
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

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
			Config.VUE_APP_ESTATE_SERVICE + `/estates/${estateId}/buildings`,
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
			const response = await httpClient.get(
				Config.VUE_APP_ESTATE_SERVICE + '/buildings/geolocations',
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			return mapper.mapResponseToBuildingLocations(response.data);
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},
	async getBuildingById(
		context: ActionContext<IRootState, IRootState>,
		{ buildingId }: { buildingId: string }
	) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_ESTATE_SERVICE + `/buildings/${buildingId}`,
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			return mapper.mapResponseToBuildingDetails(response.data);
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},
	async getBuildingFloors(
		context: ActionContext<IRootState, IRootState>,
		{
			buildingId,
			includeRooms,
		}: { buildingId: string; includeRooms: boolean }
	) {
		const response = await httpClient.get(
			Config.VUE_APP_ESTATE_SERVICE +
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
			Config.VUE_APP_ESTATE_SERVICE +
				`/buildings/${buildingId}/rooms?limit=-1`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return mapper.mapResponseToBuildingRooms(response.data);
	},
	async getFloorBlueprint(
		context: ActionContext<IRootState, IRootState>,
		{ floorId }: { floorId: string }
	) {
		const response = await httpClient.get(
			Config.VUE_APP_ESTATE_SERVICE +
				`/floors/${floorId}/blueprint?format=svg`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return response.data.replace(/wstxns1:/g, '');
	},
	async getRoomById(
		context: ActionContext<IRootState, IRootState>,
		{ roomId }: { roomId: string }
	) {
		const response = await httpClient.get(
			Config.VUE_APP_ESTATE_SERVICE + `/rooms/${roomId}`,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return mapper.mapResponseToRoomDetails(response.data);
	},
	async getBusinessTypes(context: ActionContext<IRootState, IRootState>) {
		const response = await httpClient.get(
			Config.VUE_APP_ESTATE_SERVICE + '/businessTypes',
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);

		return mapper.mapResponseToBusinessTypes(response.data);
	},
};
