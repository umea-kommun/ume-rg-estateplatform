import { KvittensStatus } from './Enums';

export interface IKvittensState {
	kvittensList?: IKvittens[];
	kvittensAgentList?: IAgentKvittens[];
}

export interface IKvittens {
	localId: string; // A local unique identifier for the kvittens
	id: string;
	templateId: string;
	title: string;
	personName: string;
	personSSNo: string;
	linkedPersons: IKvittensLinkedPerson[];
}

export interface IKvittensLinkedPerson {
	name: string;
	socialSecurityNumber: string;
	userHasAnswered: boolean;
}

export interface IKvittensHistory {
	name: string;
	date: string;
	agentName?: string;
	imageIdToken?: string;
}
export interface IKvittensDetails {
	templateId: string;
	title: string;
	text: string;
	confirmText: string;
	gpdrText: string;
	personName: string;
	personSSNo: string;
	linkedPersons: IKvittensLinkedPerson[];
	history: IKvittensHistory[];
}

export interface IKvittensFilterSchool {
	name: string;
	refId: string;
}
export interface IKvittensFilterClass {
	name: string;
	refId: string;
	schoolRefId: string;
}

export interface IKvittensSummaryStudentAnswer {
	templateId: string;
	status: KvittensStatus;
	responders: IKvittensSummaryResponder[] | null;
}

export interface IKvittensSummaryResponder {
	name: string;
	dateOfBirth: string;
	hasAnswered: boolean;
}

export interface IKvittensSummaryStudent {
	name: string;
	dateOfBirth: string;
	answers: IKvittensSummaryStudentAnswer[];
}

export interface IKvittensSummaryTemplate {
	id: string;
	title: string;
	shortTitle: string;
}

export interface IKvittensSummary {
	templates: IKvittensSummaryTemplate[];
	students: IKvittensSummaryStudent[];
}

export interface IAgentKvittens {
	templateId: string;
	title: string;
	personName: string;
	personSSNo: string;
	status: KvittensStatus;
}

export interface ICreateKvittensTemplate {
	title: string;
	shortTitle: string;
	text: string;
	confirmText: string;
	gdprText: string;
	targets: IKvittensTemplateTarget[];
}

export interface IKvittensTemplate extends ICreateKvittensTemplate {
	id: string;
}

export interface IKvittensTemplateTarget {
	schoolForm: string;
	schoolYear: string;
}

export interface IDisplayKvittensTemplateTarget {
	schoolForm: string;
	schoolFormLabel: string;
	schoolYears: string[];
	schoolYearsLabel: string;
}

export interface ISchoolYearPerSchoolForm {
	schoolForm: string;
	schoolYears: string[];
}
