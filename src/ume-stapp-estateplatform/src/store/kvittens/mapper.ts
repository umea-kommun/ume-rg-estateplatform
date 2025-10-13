import {
	IKvittens,
	IKvittensDetails,
	IKvittensFilterSchool,
	IKvittensFilterClass,
	IKvittensHistory,
	IKvittensLinkedPerson,
	IKvittensSummary,
	IKvittensSummaryTemplate,
	IKvittensSummaryStudent,
	IKvittensSummaryStudentAnswer,
	IAgentKvittens,
} from '@/models/kvittens/Interfaces';
import { Helper } from '@/utils/helper';

export default {
	mapResponseToKvittensList: (
		kvittensListResponse: {
			id: string;
			templateId: string;
			title: string;
			personName: string;
			personSSNo: string;
			linkedPersons: {
				name: string;
				socialSecurityNumber: string;
				userHasAnswered: boolean;
			}[];
		}[]
	): IKvittens[] => {
		return kvittensListResponse.map((kvittensData) => {
			const linkedPersons: IKvittensLinkedPerson[] =
				kvittensData.linkedPersons?.map((linkedPersonData) => {
					const linkedPerson: IKvittensLinkedPerson = {
						name: linkedPersonData.name,
						socialSecurityNumber:
							linkedPersonData.socialSecurityNumber,
						userHasAnswered: linkedPersonData.userHasAnswered,
					};
					return linkedPerson;
				});

			const kvittens: IKvittens = {
				localId: Helper.generateUuid(),
				id: kvittensData.id,
				templateId: kvittensData.templateId,
				title: kvittensData.title,
				personName: kvittensData.personName,
				personSSNo: kvittensData.personSSNo,
				linkedPersons,
			};

			return kvittens;
		});
	},
	mapResponseToKvittensDetails: (kvittensDetailsResponse: {
		templateId: string;
		title: string;
		text: string;
		confirmText: string;
		personName: string;
		personSSNo: string;
		gdprText: string;
		linkedPersons: {
			name: string;
			socialSecurityNumber: string;
			userHasAnswered: boolean;
		}[];
		history: {
			name: string;
			date: string;
			agentName?: string;
			imageIdToken?: string;
		}[];
	}): IKvittensDetails => {
		const linkedPersons: IKvittensLinkedPerson[] =
			kvittensDetailsResponse.linkedPersons?.map((linkedPersonData) => {
				const linkedPerson: IKvittensLinkedPerson = {
					name: linkedPersonData.name,
					socialSecurityNumber: linkedPersonData.socialSecurityNumber,
					userHasAnswered: linkedPersonData.userHasAnswered,
				};
				return linkedPerson;
			});

		const history: IKvittensHistory[] =
			kvittensDetailsResponse.history?.map((historyData) => {
				const linkedPerson: IKvittensHistory = {
					name: historyData.name,
					date: historyData.date,
					agentName: historyData.agentName,
					imageIdToken: historyData.imageIdToken,
				};
				return linkedPerson;
			});

		const kvittens: IKvittensDetails = {
			templateId: kvittensDetailsResponse.templateId,
			title: kvittensDetailsResponse.title,
			text: kvittensDetailsResponse.text,
			confirmText: kvittensDetailsResponse.confirmText,
			gpdrText: kvittensDetailsResponse.gdprText,
			personName: kvittensDetailsResponse.personName,
			personSSNo: kvittensDetailsResponse.personSSNo,
			linkedPersons,
			history,
		};

		return kvittens;
	},
	mapResponseToSchoolsAndClasses: (response: {
		schools: {
			id: string;
			name: string;
		}[];
		classes: {
			id: string;
			name: string;
			schoolId: string;
		}[];
	}): {
		schools: IKvittensFilterSchool[];
		groups: IKvittensFilterClass[];
	} => {
		const schools = response.schools.map((responseSchool) => {
			const school: IKvittensFilterSchool = {
				refId: responseSchool.id,
				name: responseSchool.name,
			};
			return school;
		});
		const groups = response.classes.map((responseClass) => {
			const classGroup: IKvittensFilterClass = {
				refId: responseClass.id,
				name: responseClass.name,
				schoolRefId: responseClass.schoolId,
			};
			return classGroup;
		});
		return { schools, groups };
	},
	mapResponseToSummary: (response: {
		templates: {
			id: string;
			title: string;
			shortTitle: string;
		}[];
		students: {
			name: string;
			dateOfBirth: string;
			answers: {
				templateId: string;
				status: number;
			}[];
		}[];
	}): IKvittensSummary => {
		const templates = response.templates.map((responseTemplate) => {
			const template: IKvittensSummaryTemplate = {
				id: responseTemplate.id,
				title: responseTemplate.title,
				shortTitle: responseTemplate.shortTitle,
			};
			return template;
		});
		const students = response.students.map((responseStudent) => {
			const answers = responseStudent.answers.map((responseAnswer) => {
				const answer: IKvittensSummaryStudentAnswer = {
					templateId: responseAnswer.templateId,
					status: responseAnswer.status,
				};
				return answer;
			});

			const student: IKvittensSummaryStudent = {
				name: responseStudent.name,
				dateOfBirth: responseStudent.dateOfBirth,
				answers: answers,
			};
			return student;
		});
		return { templates, students };
	},
	mapResponseToAgentKvittensList: (
		kvittensListResponse: {
			kvittensTemplateId: string;
			title: string;
			personName: string;
			personSsNo: string;
			status: number;
		}[]
	): IAgentKvittens[] => {
		return kvittensListResponse.map((kvittensData) => {
			const kvittens: IAgentKvittens = {
				templateId: kvittensData.kvittensTemplateId,
				title: kvittensData.title,
				personName: kvittensData.personName,
				personSSNo: kvittensData.personSsNo,
				status: kvittensData.status,
			};

			return kvittens;
		});
	},
};
