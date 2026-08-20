import { IFeedbackState } from '@/models/feedback/Interfaces';
import actions from './actions';
import mutations from './mutations';

const state: IFeedbackState = {
	submittedFeedback: [],
};

export default {
	state,
	actions,
	mutations,
};
