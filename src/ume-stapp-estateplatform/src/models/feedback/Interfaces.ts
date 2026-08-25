export interface IFeedbackState {
	submittedFeedback: IFeedback[];
}

export interface IFeedback {
	category: string;
	rating: number;
	additionalInfo?: Record<string, unknown>;
	comment?: string;
}
