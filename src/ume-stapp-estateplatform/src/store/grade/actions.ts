import Config from '@/Config';
import { IRootState } from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';
import Axios from 'axios';
import { ActionContext } from 'vuex';
import mapper from './mapper';

const httpClient = Axios.create();

export default {
	async getGrades(context: ActionContext<IRootState, IRootState>) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_ARCHIVE_SERVICE_GRADE,
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);
			return mapper.mapResponseToGrades(response.data);
		} catch (err) {
			ErrorService.onError({ err });
		}
	},
	async downloadGrade(
		context: ActionContext<IRootState, IRootState>,
		documentId: string
	) {
		try {
			const response = await httpClient.get(
				`${Config.VUE_APP_ARCHIVE_SERVICE_GRADE}/download?documentId=${documentId}`,
				{
					responseType: 'blob',
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			const blobUrl = URL.createObjectURL(response.data);
			const newWindow = window.open(blobUrl, '_blank');

			if (!newWindow) {
				window.location.href = blobUrl;
			}
		} catch (err) {
			ErrorService.onError({ err });
		}
	},
};
