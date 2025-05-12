import { MutationType } from '@/models/Enums';
import { IKvittens, IKvittensState } from '@/models/kvittens/Interfaces';

export default {
	[MutationType.UpdateKvittensList]: (
		state: IKvittensState,
		kvittensList: IKvittens[]
	) => {
		state.kvittensList = kvittensList;
	},
	[MutationType.UpdateAnswerInKvittensList]: (
		state: IKvittensState,
		{
			localId,
			linkedPersonSSN,
			hasAnswered,
		}: { localId: string; linkedPersonSSN: string; hasAnswered: boolean }
	) => {
		state.kvittensList.forEach((kvittens) => {
			if (kvittens.localId === localId) {
				kvittens.linkedPersons.forEach((linkedPerson) => {
					if (linkedPerson.socialSecurityNumber === linkedPersonSSN) {
						linkedPerson.userHasAnswered = hasAnswered;
					}
				});
			}
		});
	},
};
