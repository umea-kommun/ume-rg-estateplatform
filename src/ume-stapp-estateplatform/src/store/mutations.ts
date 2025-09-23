import {
	ConsentStatus,
	ConsentTemplateStatus,
	MutationType,
	UserConsentStatus,
} from '@/models/Enums';
import {
	IRootState,
	IUserContactInfo,
	IUser,
	IGuardianConsent,
	IGuardianUser,
	IConsentTemplate,
	IChildConsent,
	IConsumerGroup,
	IConsentConsumerTemplate,
	IConsentTemplateWithConsents,
	IConsentTemplateGroup,
	ITesterTestAsPerson,
	IError,
	IAgentConsent,
	IChild,
} from '@/models/Interfaces';
import { Helper } from '@/utils/helper';
import moment from 'moment';

export default {
	[MutationType.UserLogIn]: (
		state: IRootState,
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		{ jwtPayload, rawJwt, authClientName }: any
	) => {
		const userContactInfo = {
			socialSecurityNumber: jwtPayload.socialSecurityNumber || '?',
			firstName: jwtPayload.firstName || '',
			lastName: jwtPayload.lastName || '',
			address: jwtPayload.address || '',
			postalNumber: jwtPayload.postalNumber || '',
			city: jwtPayload.city || '',
			phoneNumber: jwtPayload.phoneNumber || '',
			email: jwtPayload.email || '',
		} as IUserContactInfo;

		state.user = {
			...jwtPayload,
			token: rawJwt,
			isAuthenticated: true,
			groups: jwtPayload.groups?.split(',') ?? [],
			userContactInfo,
			authClientName,
		} as IUser;
	},

	[MutationType.UserEnterPage]: (
		state: IRootState,
		{ userId }: { userId: string }
	) => {
		if (!state.user.userId) {
			state.user.userId = userId;
		}
	},

	[MutationType.UserLogOut]: (state: IRootState) => {
		state.user = {
			isAuthenticated: false,
			userId: '',
			authClientName: '',
		} as IUser;
	},

	[MutationType.HideWarningMessage]: (
		state: IRootState,
		hideWarningMessage: boolean
	) => {
		state.hideWarningMessage = hideWarningMessage;
	},

	[MutationType.GetChildren]: (
		state: IRootState,
		payload: IChild[] | null
	) => {
		if (state.guardianUser) {
			state.guardianUser.children = payload || [];
		} else {
			state.guardianUser = {
				children: payload || [],
			} as IGuardianUser;
		}
	},

	[MutationType.GetGuardianConsent]: (
		state: IRootState,
		consent: IGuardianConsent
	) => {
		state.guardianConsent = consent;
	},

	[MutationType.GetGuardianConsentList]: (
		state: IRootState,
		consents: IChildConsent[]
	) => {
		state.childConsentList = consents;
	},

	[MutationType.UpdateGuardianConsentListAnswer]: (
		state: IRootState,
		payload: {
			templateGuid: string;
			childSSN: string;
			guardianStatus: UserConsentStatus;
			consentStatus: ConsentStatus;
		}
	) => {
		state.childConsentList?.forEach((consent) => {
			if (
				consent.templateGuid === payload.templateGuid &&
				consent.childSSNo === payload.childSSN
			) {
				consent.userStatus = payload.guardianStatus;
				consent.consentStatus = payload.consentStatus;
			}
		});
	},

	[MutationType.GetConsentTemplates]: (
		state: IRootState,
		templates: IConsentTemplate[]
	) => {
		state.consentTemplates = templates;
	},

	[MutationType.GetConsentTemplate]: (
		state: IRootState,
		template: IConsentTemplate
	) => {
		state.consentTemplate = template;
	},

	[MutationType.NewConsentTemplate]: (state: IRootState) => {
		state.consentTemplate = {
			title: '',
			content: '',
			publishedDate: moment().format('YYYY-MM-DD'),
			expireDate: null,
			status: ConsentTemplateStatus.Draft,
			templateConnections: [],
		};
	},

	[MutationType.UpdateConsentTemplate]: (
		state: IRootState,
		{ prop, value }: { prop: keyof IConsentTemplate; value: never }
	) => {
		if (state.consentTemplate && prop) {
			state.consentTemplate[prop] = value;
		}
	},
	[MutationType.GetConsentTemplateUnitTypes]: (
		state: IRootState,
		schoolForms: string[]
	) => {
		state.consentTemplateUnitTypes = schoolForms;
	},
	[MutationType.GetConsentTemplateGroups]: (
		state: IRootState,
		groups: IConsentTemplateGroup[]
	) => {
		state.consentTemplateGroups = groups.sort(Helper.sortByTitle);
	},

	[MutationType.GetConsumerList]: (
		state: IRootState,
		{
			templates,
			groups,
		}: { templates: IConsentConsumerTemplate[]; groups: IConsumerGroup[] }
	) => {
		state.consumer.templates = templates;
		state.consumer.groups = groups;
	},
	[MutationType.GetConsumerDetails]: (
		state: IRootState,
		{
			templateWithConsents,
		}: { templateWithConsents: IConsentTemplateWithConsents }
	) => {
		state.consumer.templateWithConsents = templateWithConsents;
	},

	[MutationType.SetError]: (state: IRootState, error: IError | null) => {
		state.error = error;
	},

	[MutationType.GetTesterSchoolUnits]: (
		state: IRootState,
		groups: IConsentTemplateGroup[]
	) => {
		state.tester.schoolUnits = groups.sort(Helper.sortByTitle);
	},
	[MutationType.SetTesterTestAs]: (
		state: IRootState,
		testAsPerson: ITesterTestAsPerson
	) => {
		state.tester.testAsPerson = testAsPerson;
	},

	[MutationType.UpdateConsentAgentConsentList]: (
		state: IRootState,
		consents: IChildConsent[]
	) => {
		state.consentAgentConsentList = consents ?? [];
	},
	[MutationType.UpdateConsentAgentConsentStatus]: (
		state: IRootState,
		updatedConsent: IAgentConsent
	) => {
		state.consentAgentConsentList?.forEach((consent) => {
			if (
				consent.templateGuid === updatedConsent.templateGuid &&
				consent.childSSNo === updatedConsent.childSSNo
			) {
				consent.consentStatus = updatedConsent.consentStatus;
			}
		});
	},
};
