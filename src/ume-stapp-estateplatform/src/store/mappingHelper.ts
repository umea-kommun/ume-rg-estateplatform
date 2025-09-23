import {
	IAgentConsent,
	IChild,
	IGuardian,
	IGuardianConsent,
	IGuardianConsentHistory,
	IResponseConsent,
	ITesterTestAsPerson,
} from '@/models/Interfaces';
import { ConsentStatus, UserConsentStatus } from '@/models/Enums';

export class mappingHelper {
	public static mapResponseDataToGuardianConsent(
		responseData: IResponseConsent
	): IGuardianConsent {
		const history: IGuardianConsentHistory[] = responseData.signLogs.map(
			(log) => ({
				status: log.status,
				created: log.created,
				guardianName: log.guardianName,
				agentName: log?.agentName,
				imageIdToken: log?.imageIdToken,
			})
		);

		const guardians: IGuardian[] = [];

		responseData.linkedPersons.forEach((linkedPerson: IGuardian) => {
			guardians.push({
				name: linkedPerson.name,
				socialSecurityNumber: linkedPerson.socialSecurityNumber,
				userStatus:
					linkedPerson.userStatus === 0
						? UserConsentStatus.NotAnswered
						: linkedPerson.userStatus === 1
						? UserConsentStatus.Approved
						: linkedPerson.userStatus === 2
						? UserConsentStatus.Rejected
						: UserConsentStatus.NotApplicable,
			} as IGuardian);
		});

		const responseAsGuardianConsent: IGuardianConsent | null = {
			guid: responseData.guid,
			title: responseData.title,
			data: responseData.data,
			childName: responseData.childName,
			childSSNo: responseData.childSSNo,
			templateGuid: responseData.templateGuid,
			expireDate: responseData.expireDate,
			isActive: responseData.isActive,
			consentStatus:
				responseData.status === 1
					? ConsentStatus.Approved
					: responseData.status === 2
					? ConsentStatus.Denied
					: ConsentStatus.Pending,
			linkedPersons: responseData.linkedPersons,
			consentHistory: history,
		};
		return responseAsGuardianConsent;
	}

	public static mapResponseDataToAgentConsent(
		responseData: IResponseConsent
	): IAgentConsent {
		return {
			...this.mapResponseDataToGuardianConsent(responseData),
			currentlyResponsiblePersons:
				responseData.currentlyResponsiblePersons,
		};
	}

	public static mapResponseChildDataToItem(
		responseChildren: {
			name: string;
			socialSecurityNumber: string;
			guardianIsNotFolkbokford?: boolean;
		}[]
	): IChild[] {
		return responseChildren.map((element) => ({
			name: element.name,
			socialSecurityNumber: element.socialSecurityNumber,
			guardianIsNotFolkbokford: element.guardianIsNotFolkbokford ?? false,
		}));
	}

	public static mapTesterToTestPerson(testAsPerson: ITesterTestAsPerson) {
		return {
			name: testAsPerson.name,
			personnummer: testAsPerson.socialSecurityNumber,
		};
	}
}
