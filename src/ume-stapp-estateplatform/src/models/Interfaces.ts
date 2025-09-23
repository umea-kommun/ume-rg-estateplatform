// import * as Enums from '@/models/Enums';

import { AxiosError } from 'axios';
import {
	ConsentTemplateStatus,
	ConsentStatus,
	UserConsentStatus,
	TemplateConnectionType,
} from './Enums';
import { IKvittensState } from './kvittens/Interfaces';

/**
 * State för vuex-store
 */
export interface IRootState {
	user: IUser;
	childConsentList?: IChildConsent[];
	guardianConsent?: IGuardianConsent;
	guardianUser: null | IGuardianUser;
	consentAgentConsentList?: IChildConsent[];
	consentTemplates?: null | IConsentTemplate[];
	consentTemplate?: null | IConsentTemplate;
	consentTemplateGroups?: null | IConsentTemplateGroup[];
	consentTemplateUnitTypes?: string[];
	consumer: IConsentConsumerState;
	tester: ITesterState;
	error?: IError | null;
	hideWarningMessage?: null | boolean;

	kvittens?: IKvittensState;
}

export interface IError {
	error: unknown | Error | AxiosError;
	userMessage?: null | string;
	errorPage?: IErrorPage;
}

export interface IErrorPage {
	visible: boolean;
	title?: string;
	titleKey?: string; // i18n translation key
	message?: string;
	messageKey?: string; // i18n translation key
	hideReport?: boolean;
}

export interface IErrorToDisplay {
	title: string;
	message?: string;
}

/**
 * Interface for IUserContactInfo
 */
export interface IUserContactInfo {
	socialSecurityNumber: string;
	firstName: string;
	lastName: string;
	address: string;
	postalNumber: string;
	city: string;
	phoneNumber: string;
	email: string;
}

/**
 * User En inloggad användare
 */
export interface IUser {
	socialSecurityNumber: string;
	fullName?: string;
	firstName: string;
	lastName: string;
	token?: string;
	email: string | null;
	scope?: string[];
	userContactInfo?: IUserContactInfo | null;
	isAuthenticated: boolean;
	protectedIdentity?: string;
	userId: string;

	/**
	 * Unix timestamp telling when user session expires
	 */
	exp?: number;

	/**
	 * The auth client used to login this user
	 */
	authClientName: string;

	/**
	 * Which idp do we have behind the user token
	 */
	idp?: string;

	/**
	 * Which ad groups the user is a member of (ids)
	 */
	groups?: string[];
}

export interface IChild {
	name: string;
	socialSecurityNumber: string;
	guardianIsNotFolkbokford: boolean;
}

export interface IGuardianUser {
	children: IChild[];
}

/**
 * Interface for a guardians consent
 */
export interface IGuardianConsent {
	guid: string;
	title: string;
	data: string;
	childName: string;
	childSSNo: string;
	templateGuid: string;
	expireDate: string;
	isActive: boolean;
	consentStatus: ConsentStatus;
	linkedPersons: IGuardian[];
	consentHistory: IGuardianConsentHistory[];
}

export interface IGuardianConsentHistory {
	status: UserConsentStatus;
	created: string;
	guardianName: string;
	agentName?: string;
	imageIdToken?: string;
}

export interface IAgentConsent extends IGuardianConsent {
	currentlyResponsiblePersons: IGuardian[] | null;
}

export interface IGuardian {
	consentGuid: string;
	name: string;
	socialSecurityNumber: string;
	userStatus: UserConsentStatus;
}

export interface IConsentTemplateUnitType {
	name: string;
	refId: string;
	type: TemplateConnectionType;
	key: string;
}

export interface IConsentTemplateGroup {
	refId: string;
	title: string;
	type: TemplateConnectionType;
	schoolTypes?: string[];
	parentRefId?: string;
	startDate?: string;
	endDate?: string;
}

export interface IConsentTemplate {
	guid?: string;
	title: string;
	content: string;
	publishedDate: string | null;
	expireDate: string | null;
	status: ConsentTemplateStatus;
	templateConnections: ITemplateConnection[];
}

export interface ITemplateConnection {
	refId: string;
	name: string;
	type: TemplateConnectionType;
}

export interface IChildConsent {
	title: string;
	childName: string;
	childSSNo: string;
	templateGuid: string;
	consentStatus: ConsentStatus;
	userStatus: UserConsentStatus;
	isActive: boolean;
}

export interface IConsentTemplateWithConsents {
	title: string;
	text: string;
	group: IConsentTemplateGroup;
	publishedDate: string;
	expireDate: string;
	consents: {
		consentGuid?: string;
		name: string;
		status: ConsentStatus;
	}[];
}
export interface IConsumerGroup {
	refId: string;
	name: string;
	type: TemplateConnectionType;
	parents:
		| {
				id: string;
				name: string;
		  }[]
		| null;
	startDate: null | string;
	endDate: null | string;
}

export interface IConsentConsumerTemplateGroup {
	refId: string;
	title: string;
	type: TemplateConnectionType;
	schoolTypes?: string[];
	parentIds?: string[];
	startDate?: string;
	endDate?: string;
}

export interface IFilteredConsumerTemplate {
	guid?: string;
	title: string;
	groups: IConsentConsumerTemplateGroup[];
	period: {
		start: string;
		end: string;
	};
}

export interface IConsentConsumerTemplate
	extends Omit<IConsentTemplate, 'groups'> {
	publishedDate: string;
	expireDate: string;
	groups: IConsumerGroup[];
}
export interface IConsentConsumerState {
	groups?: IConsumerGroup[];
	templates?: IConsentConsumerTemplate[];
	templateWithConsents?: IConsentTemplateWithConsents;
}
export interface IConsentAgentEditData {
	childSSNo: string;
	templateGuid: string;
}
export interface IChildConsentRequest {
	childSSNo: string;
	templateGuid: string;
}
export interface IConsentTemplateRequest {
	refId: string;
	groupName: string;
}
export interface IResponseConsent {
	guid: string;
	title: string;
	data: string;
	childName: string;
	childSSNo: string;
	templateGuid: string;
	expireDate: string;
	isActive: boolean;
	status: ConsentStatus;
	linkedPersons: IGuardian[];
	currentlyResponsiblePersons: IGuardian[];
	signLogs: IResponseConsentHistory[];
}
export interface IResponseConsentHistory {
	guardianName: string;
	agentName?: string;
	imageIdToken?: string;
	status: UserConsentStatus;
	created: string;
}

export interface ITesterTestAsPerson {
	name: string;
	socialSecurityNumber: string;
	unitRefId: string;
	unitTitle: string;
}
interface ITesterState {
	schoolUnits?: IConsentTemplateGroup[];
	testAsPerson?: ITesterTestAsPerson;
}

export interface ISortBy {
	key: string;
	order: boolean | 'asc' | 'desc';
}

export interface ITableHeader {
	title: string;
	description?: string;
	key: string;
	align?: 'start' | 'end' | 'center';
	sortable?: boolean;
}
