import Config from '@/Config';
import { MutationType } from '@/models/Enums';
import { IFeedback } from '@/models/feedback/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { createHttpClient } from '@/utils/httpClient';
import { ActionContext } from 'vuex';

const httpClient = createHttpClient();

export default {
	async feedbackRate(
		context: ActionContext<IRootState, IRootState>,
		feedback: IFeedback
	) {
		await httpClient.post(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_FEEDBACK}/rate`,
			feedback,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);
		context.commit(MutationType.FeedbackGiven, feedback);
	},
	async feedbackComment(
		context: ActionContext<IRootState, IRootState>,
		feedback: IFeedback
	) {
		await httpClient.post(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_FEEDBACK}/comment`,
			feedback,
			{
				headers: {
					Authorization: 'Bearer ' + context.rootState.user.token,
				},
			}
		);
		context.commit(MutationType.FeedbackGiven, feedback);
	},
};
