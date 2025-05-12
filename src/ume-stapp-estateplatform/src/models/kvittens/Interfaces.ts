import { KvittensStatus } from './Enums';

export interface IKvittensState {
	kvittensList: IKvittens[];
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
