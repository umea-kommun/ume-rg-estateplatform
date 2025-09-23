import { MutationType } from '@/models/Enums';
import { KvittensStatus } from '@/models/kvittens/Enums';
import {
	IAgentKvittens,
	IKvittens,
	IKvittensLinkedPerson,
	IKvittensState,
} from '@/models/kvittens/Interfaces';

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
		state.kvittensList?.forEach((kvittens) => {
			if (kvittens.localId === localId) {
				kvittens.linkedPersons.forEach((linkedPerson) => {
					if (linkedPerson.socialSecurityNumber === linkedPersonSSN) {
						linkedPerson.userHasAnswered = hasAnswered;
					}
				});
			}
		});
	},
	[MutationType.UpdateKvittensAgentList]: (
		state: IKvittensState,
		kvittensList: IAgentKvittens[]
	) => {
		state.kvittensAgentList = kvittensList;
	},
	[MutationType.UpdateAnswerInAgentKvittensList]: (
		state: IKvittensState,
		{
			templateId,
			subjectSsno,
			linkedPersons,
		}: {
			templateId: string;
			subjectSsno: string;
			linkedPersons: IKvittensLinkedPerson[];
		}
	) => {
		state.kvittensAgentList?.forEach((kvittens) => {
			if (
				kvittens.templateId === templateId &&
				kvittens.personSSNo === subjectSsno
			) {
				if (linkedPersons.every((p) => p.userHasAnswered)) {
					kvittens.status = KvittensStatus.Approved;
				} else if (linkedPersons.some((p) => p.userHasAnswered)) {
					kvittens.status = KvittensStatus.NotAnsweredByAll;
				}
			}
		});
	},
};
