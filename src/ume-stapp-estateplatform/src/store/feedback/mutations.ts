import { MutationType } from '@/models/Enums';
import { IFeedback, IFeedbackState } from '@/models/feedback/Interfaces';

export default {
	[MutationType.FeedbackGiven]: (
		state: IFeedbackState,
		feedback: IFeedback
	) => {
		const feedbackAlreadyExists = state.submittedFeedback.find(
			(fb) => fb.category === feedback.category
		);
		if (feedbackAlreadyExists) {
			state.submittedFeedback = state.submittedFeedback.map((fb) => {
				if (feedback.category === fb.category) {
					return feedback;
				}
				return fb;
			});
		} else {
			state.submittedFeedback.push(feedback);
		}
	},
};
