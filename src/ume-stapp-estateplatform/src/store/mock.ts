import Config from '@/Config';
import MockedDefaultData from '@/mock/data';
import {
	ConsentStatus,
	ConsentTemplateStatus,
	UserConsentStatus,
} from '@/models/Enums';
import { IConsentTemplate } from '@/models/Interfaces';
import { Helper } from '@/utils/helper';
import { AxiosInstance } from 'axios';
import MockAdapter from 'axios-mock-adapter';
import store from './store';
import moment from 'moment';
import {
	loadDataFromLocalStorage,
	saveDataToLocalStorage,
} from '@/mock/mockStorageHandler';
import { IKvittensSummaryStudent } from '@/models/kvittens/Interfaces';
import { KvittensStatus } from '@/models/kvittens/Enums';

const LOG_ACTIVITY_TO_CONSOLE = true;
// Used when we want to replace a SSN in the mock data with the logged in users SSN
const USER_SSN_PLACEHOLDER = 'user-ssn';

function log(message?: unknown, ...optionalParams: unknown[]): void {
	if (LOG_ACTIVITY_TO_CONSOLE) {
		console.log(message, ...optionalParams);
	}
}

/**
 * Mockadapter för Axios. Tar hand om anrop till Axios och levererar mockdata istället för riktig data.
 */
export default function (axios: AxiosInstance): void {
	const delay = 200;
	const mock = new MockAdapter(axios, { delayResponse: delay });
	const data = loadDataFromLocalStorage() || MockedDefaultData;

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CHILDREN}/children`
	).reply(() => {
		log('Mock called on: GET CHILDREN/children');

		const responseData = data.children;
		log('Returning:', responseData);

		if (responseData) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onPost(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT}/childconsent`
	).reply((config) => {
		log('Mock called on: POST CONSENT/childconsent');

		const requestData = JSON.parse(config.data);
		log('Request Data:', requestData);

		const consent = data.consent.find(
			(f) =>
				f.childSSNo === requestData.childSSNo &&
				f.templateGuid === requestData.templateGuid
		);

		if (consent) {
			const consentCopy = {
				...consent,
				linkedPersons: consent.linkedPersons.map((person) => {
					if (person.socialSecurityNumber === USER_SSN_PLACEHOLDER) {
						return {
							...person,
							socialSecurityNumber:
								store.state.user.socialSecurityNumber,
						};
					}
					return person;
				}),
			};

			log('Returning:', consentCopy);
			return [200, consentCopy];
		} else {
			return [404];
		}
	});

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT}/childrenconsentlist`
	).reply(() => {
		log('Mock called on: GET CONSENT/childrenconsentlist');
		log('Returning:', data.consentList);

		if (data.consentList) {
			return [200, data.consentList];
		} else {
			return [404];
		}
	});

	mock.onPost(`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT}`).reply(
		(config) => {
			log('Mock called on: POST CONSENT');

			const requestData = JSON.parse(config.data);
			log('Request Data:', requestData);

			console.warn(
				'Mock not implemented! No changes to stored data; Returning: OK'
			);

			return [200];
		}
	);

	mock.onPut(`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT}`).reply(
		(config) => {
			log('Mock called on: PUT CONSENT/');

			const requestData = JSON.parse(config.data);
			log('Request Data:', requestData);

			const consentGuid = requestData.consentGuid;
			const userStatus = requestData.userStatus;
			const stamp = requestData.stamp;
			const signType = requestData.signType;

			const existingConsent = data.consent.find(
				(f) => f.guid === consentGuid
			);

			if (existingConsent) {
				let fullName = 'MekaIke Burgerking';
				let ssno = USER_SSN_PLACEHOLDER;

				existingConsent.linkedPersons.forEach((person) => {
					if (
						person.socialSecurityNumber === USER_SSN_PLACEHOLDER ||
						person.socialSecurityNumber ===
							store.state.user.socialSecurityNumber
					) {
						person.userStatus = userStatus;
						person.socialSecurityNumber =
							store.state.user.socialSecurityNumber;
						fullName = person.name;
						ssno = person.socialSecurityNumber;
					}
				});

				const signLog = {
					guid: Helper.generateUuid,
					consentGuid: existingConsent.guid as string,
					status: userStatus,
					stamp: stamp,
					guardianSocialSecurityNumber: ssno,
					guardianName: fullName,
					created: moment().format('YYYY-MM-DD HH:mm:ss'),
					createdBy: fullName,
					type: signType,
				};

				existingConsent.signLogs?.push(signLog);

				let consentStatus = ConsentStatus.Denied;
				if (
					existingConsent.linkedPersons?.some(
						(p) => p.userStatus == UserConsentStatus.Rejected
					)
				) {
					consentStatus = ConsentStatus.Denied;
				} else if (
					existingConsent.linkedPersons?.every(
						(p) => p.userStatus == UserConsentStatus.Approved
					)
				) {
					consentStatus = ConsentStatus.Approved;
				} else {
					consentStatus = ConsentStatus.Pending;
				}

				existingConsent.status = consentStatus;
				existingConsent.modifiedBy = fullName;

				const existingConsentInList = data.consentList.find(
					(f) => f.consentGuid === consentGuid
				);
				if (existingConsentInList) {
					existingConsentInList.userStatus = userStatus;
					existingConsentInList.consentStatus = consentStatus;
				}

				saveDataToLocalStorage(data);

				log('Some changes to stored data; Returning:', existingConsent);
				return [200, existingConsent];
			} else {
				return [404];
			}
		}
	);

	mock.onPost(`${Config.VUE_APP_SEND_ERROR_API_URL}`).reply((config) => {
		log('Mock called on: POST ERROR_API');

		const requestData = JSON.parse(config.data);
		log('No changes to stored data; Returning: OK');
		console.error(requestData);

		return [200];
	});

	mock.onGet(`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE}`).reply(
		() => {
			log('Mock called on: GET TEMPLATE');
			const responseData = data.consentTemplates as IConsentTemplate[];

			log('Returning:', responseData);

			if (responseData) {
				return [200, responseData];
			} else {
				return [404];
			}
		}
	);

	mock.onGet(
		new RegExp(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE}/templatebytemplateguid.+`
		)
	).reply((config) => {
		log('Mock called on: GET TEMPLATE/templatebytemplateguid');
		const url = config.url || '';
		const guid: string = url.substring(url.lastIndexOf('=') + 1);
		log('read template guid from url: ', guid);

		const template = data.consentTemplates.find(
			(f) => f.guid === guid
		) as IConsentTemplate;
		const responseData = template;

		log('Returning:', responseData);

		if (template) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onPut(`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE}`).reply(
		(config) => {
			log('Mock called on: PUT TEMPLATE');
			const templateData = JSON.parse(config.data) as IConsentTemplate;
			const guid = templateData.guid;

			const template = data.consentTemplates.find((t) => t.guid === guid);
			if (template) {
				if (template.status === ConsentTemplateStatus.Draft) {
					Object.assign(template, templateData);
					saveDataToLocalStorage(data);

					const responseData = template;
					log('Returning:', responseData);
					return [200, responseData];
				} else {
					return [403];
				}
			}

			return [404];
		}
	);

	mock.onPost(`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE}`).reply(
		(config) => {
			log('Mock called on: POST TEMPLATE');

			const templateData = JSON.parse(config.data) as IConsentTemplate;

			templateData.guid = Helper.generateUuid() ?? '';

			(data.consentTemplates as IConsentTemplate[]).push(templateData);

			saveDataToLocalStorage(data);

			const responseData = templateData;
			log('Returning:', responseData);
			return [200, responseData];
		}
	);

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION}/getAllSchoolForms`
	).reply(() => {
		log('Mock called on: GET ORGANIZATION/getAllSchoolForms');

		const responseData = data.schoolUnitTypes.map((g) => g.key);

		log('Returning:', responseData);

		if (responseData) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION}/getAllSchools`
	).reply(() => {
		log('Mock called on: GET ORGANIZATION/getAllSchools');

		const responseData = data.groups.map((g) => ({
			name: g.name,
			id: g.refId,
			type: g.type,
			schoolForms: g.schoolTypes,
		}));
		log('Returning:', responseData);

		if (responseData) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEST}/getAllSchools`
	).reply(() => {
		log('Mock called on: GET TEST/units');

		const responseData = data.groups.map((g) => ({
			name: g.name,
			id: g.refId,
			schoolForms: g.type,
		}));
		log('Returning:', responseData);

		if (responseData) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEST}/getTeachersInSchool`
	).reply(() => {
		log('Mock called on: POST TEST/getTeachersInSchool');

		const responseData = data.teachers;
		log('Returning:', responseData);

		if (responseData) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onPost(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION}/getGroupsInSchool`
	).reply((config) => {
		log('Mock called on: POST ORGANIZATION/getGroupsInSchool');

		const requestData = JSON.parse(config.data);
		log('Request Data:', requestData);

		const unit = data.groups.find((g) => g.refId === requestData.schoolId);
		const responseData = unit?.classes.map((c) => ({
			name: c.name,
			id: c.refId,
			type: c.type,
		}));
		log('Returning:', responseData);

		if (responseData) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSUMER}/ConsumerOverviewData`
	).reply(() => {
		log('Mock called on: GET CONSUMER/ConsumerOverviewData');
		const templates = data.consumerTemplates;
		const groups = data.consumerGroups;

		const responseData = { templates, groups };
		log('Returning:', responseData);

		if (templates) {
			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onGet(
		new RegExp(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSUMER}/consumerTemplateWithConsents.+`
		)
	).reply((config) => {
		log('Mock called on: GET CONSUMER/consumerTemplateWithConsents');
		if (!config.url) {
			return [500];
		}

		const params = new URLSearchParams(
			config.url.substring(config.url.indexOf('?'))
		);
		const templateGuid = params.get('templateGuid');
		const refId = params.get('groupRefId');

		const template = data.consumerTemplates.find(
			(template) => template.guid === templateGuid
		);

		const group = data.consumerGroups.find(
			(group) => group.refId === refId
		);

		if (template && group) {
			const consents = data.consentList
				.filter(
					(consent) =>
						consent.consentGuid &&
						consent.templateGuid === template.guid
				)
				.map((consent) => ({
					childName: consent.childName,
					status: consent.consentStatus as ConsentStatus,
				}));

			const responseData = {
				templateTitle: template.title,
				templateText: template.text,
				publishedDate: template.publishedDate,
				expireDate: template.expireDate,
				groupRefId: group.refId,
				groupName: group.name,
				groupType: group.type,
				consents: consents,
			};

			log('Returning:', responseData);

			return [200, responseData];
		} else {
			return [404];
		}
	});

	mock.onPost(
		new RegExp(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT}/consentagentconsents(?:test|$)`
		)
	).reply((config) => {
		log('Mock called on: POST CONSENT_AGENT/consentagentconsents');

		const params = JSON.parse(config.data);
		const consents = data.consentList.filter(
			(consent) => consent.childSSNo === params?.childSSNo
		);

		log('Returning:', consents);

		if (consents) {
			return [200, consents];
		} else {
			return [404];
		}
	});

	mock.onPost(
		new RegExp(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT}/consentagentchildconsent(?:test|$)`
		)
	).reply((config) => {
		log('Mock called on: POST CONSENT_AGENT/consentagentchildconsent');

		const requestData = JSON.parse(config.data);
		log('Request Data:', requestData);

		const consent = data.consent.find(
			(f) =>
				f.childSSNo === requestData.childSSNo &&
				f.templateGuid === requestData.templateGuid
		);

		if (consent) {
			log('Returning:', consent);
			return [200, { ...consent }];
		} else {
			return [404];
		}
	});

	mock.onPost(
		new RegExp(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT}/SendInGuardianAnswer(?:test|$)`
		)
	).reply((config) => {
		log('Mock called on: POST CONSENT_AGENT/SendInGuardianAnswer');

		const requestData = JSON.parse(config.data.get('body'));
		log('Request Data:', requestData);

		const consentGuid = requestData.consentGuid;
		const userStatus = requestData.guardianStatus;
		const guardianSSN = requestData.guardianSSN;
		const stamp = requestData.stamp;
		const signType = requestData.signType;

		const existingConsent = data.consent.find(
			(f) => f.guid === consentGuid
		);

		if (existingConsent) {
			const agentFullName = store.state.user.fullName ?? '';
			const agentSSN = store.state.user.socialSecurityNumber;
			let guardianName = '';

			existingConsent.linkedPersons.forEach((person) => {
				if (person.socialSecurityNumber === guardianSSN) {
					person.userStatus = userStatus;
					guardianName = person.name;
				}
			});

			const signLog = {
				guid: Helper.generateUuid(),
				consentGuid: existingConsent.guid as string,
				status: userStatus,
				stamp: stamp,
				guardianSocialSecurityNumber: guardianSSN,
				guardianName: guardianName,
				agentSocialSecurityNumber: agentSSN,
				agentName: agentFullName,
				imageIdToken: Helper.generateUuid(),
				created: moment().format('YYYY-MM-DD HH:mm:ss'),
				createdBy: guardianName,
				type: signType,
			};

			existingConsent.signLogs?.push(signLog);

			let consentStatus = ConsentStatus.Denied;
			if (
				existingConsent.linkedPersons?.some(
					(p) => p.userStatus == UserConsentStatus.Rejected
				)
			) {
				consentStatus = ConsentStatus.Denied;
			} else if (
				existingConsent.linkedPersons?.every(
					(p) => p.userStatus == UserConsentStatus.Approved
				)
			) {
				consentStatus = ConsentStatus.Approved;
			} else {
				consentStatus = ConsentStatus.Pending;
			}

			existingConsent.status = consentStatus;
			existingConsent.modifiedBy = agentFullName;

			const existingConsentInList = data.consentList.find(
				(f) => f.consentGuid === consentGuid
			);
			if (existingConsentInList) {
				existingConsentInList.userStatus = userStatus;
				existingConsentInList.consentStatus = consentStatus;
			}

			saveDataToLocalStorage(data);

			log('Some changes to stored data; Returning:', existingConsent);
			return [200, existingConsent];
		} else {
			return [404];
		}
	});

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS}/kvittensList`
	).reply(() => {
		log('Mock called on: GET KVITTENS/kvittensList');

		const kvittensList = data.kvittensList.map((kvittens) => {
			const mappedKvittens = {
				...kvittens,
				text: undefined,
				confirmText: undefined,
			};
			mappedKvittens.linkedPersons.forEach((linkedPerson) => {
				if (
					linkedPerson.socialSecurityNumber === USER_SSN_PLACEHOLDER
				) {
					linkedPerson.socialSecurityNumber =
						store.state.user.socialSecurityNumber;
				}
			});

			if (mappedKvittens?.personSSNo === USER_SSN_PLACEHOLDER) {
				mappedKvittens.personSSNo =
					store.state.user.socialSecurityNumber;
			}
			return mappedKvittens;
		});

		log('Returning:', kvittensList);

		if (kvittensList) {
			return [200, kvittensList];
		} else {
			return [404];
		}
	});

	mock.onPost(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS}/details`
	).reply((config) => {
		log('Mock called on: GET KVITTENS/details');
		const requestData = JSON.parse(config.data);
		log('Request Data:', requestData);

		const kvittens = data.kvittensList.find(
			(kvittens) =>
				kvittens.templateId === requestData.templateId &&
				(kvittens.personSSNo === requestData.personSSNo ||
					(requestData.personSSNo ===
						store.state.user.socialSecurityNumber &&
						kvittens.personSSNo === USER_SSN_PLACEHOLDER))
		);

		if (kvittens?.personSSNo === USER_SSN_PLACEHOLDER) {
			kvittens.personSSNo = store.state.user.socialSecurityNumber;
		}

		kvittens?.linkedPersons.forEach((linkedPerson) => {
			if (linkedPerson.socialSecurityNumber === USER_SSN_PLACEHOLDER) {
				linkedPerson.socialSecurityNumber =
					store.state.user.socialSecurityNumber;
			}
		});

		log('Returning:', kvittens);

		if (kvittens) {
			return [200, kvittens];
		} else {
			return [404];
		}
	});

	mock.onPost(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS}/answer`
	).reply((config) => {
		log('Mock called on: GET KVITTENS/answer');
		const requestData = JSON.parse(config.data);
		log('Request Data:', requestData);

		const kvittens = data.kvittensList.find(
			(kvittens) =>
				kvittens.templateId === requestData.templateId &&
				kvittens.personSSNo === requestData.personSSNo
		);

		kvittens?.linkedPersons.forEach((linkedPerson) => {
			if (linkedPerson.socialSecurityNumber === USER_SSN_PLACEHOLDER) {
				linkedPerson.socialSecurityNumber =
					store.state.user.socialSecurityNumber;
			}
			if (
				linkedPerson.socialSecurityNumber ===
				store.state.user.socialSecurityNumber
			) {
				linkedPerson.userHasAnswered = true;
				kvittens?.history.push({
					name: linkedPerson.name,
					date: moment().format(),
				});
			}
		});

		if (kvittens?.personSSNo === USER_SSN_PLACEHOLDER) {
			kvittens.personSSNo = store.state.user.socialSecurityNumber;
		}

		saveDataToLocalStorage(data);

		log('Returning:', kvittens);

		if (kvittens) {
			return [200, kvittens];
		} else {
			return [404];
		}
	});

	const GetMentorsSchoolsAndGroups = () => {
		const schools = data.consumerGroups
			.filter((group) => group.type === 'Unit')
			.map((school) => ({
				id: school.refId,
				name: school.name,
			}));
		const classes = data.consumerGroups
			.filter((group) => group.type === 'Class')
			.map((group) => ({
				id: group.refId,
				name: group.name,
				schoolId: group.parentRefId,
			}));
		const schoolsAndClasses = { schools, classes };

		log('Returning:', schoolsAndClasses);

		if (schoolsAndClasses) {
			return [200, schoolsAndClasses];
		} else {
			return [404];
		}
	};

	mock.onGet(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION}/mentoredSchoolsAndGroupsInGymnasiet`
	).reply(() => {
		log('Mock called on: GET ORG/mentorsSchoolsAndGroups');
		return GetMentorsSchoolsAndGroups();
	});

	mock.onPost(
		`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION}/mentoredSchoolsAndGroupsInGymnasietTest`
	).reply(() => {
		log('Mock called on: POST ORG/mentorsSchoolsAndGroups');
		return GetMentorsSchoolsAndGroups();
	});

	mock.onGet(
		new RegExp(
			`${Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS}/summary.+`
		)
	).reply(() => {
		log('Mock called on: GET KVITTENS/summary');

		const students: { [SSN: string]: IKvittensSummaryStudent } = {};
		data.kvittensList.forEach((kvittens) => {
			if (!students[kvittens.personSSNo]) {
				students[kvittens.personSSNo] = {
					name: kvittens.personName,
					dateOfBirth: (kvittens.personSSNo === USER_SSN_PLACEHOLDER
						? store.state.user.socialSecurityNumber
						: kvittens.personSSNo
					).substring(0, 8),
					answers: [],
				};
			}
			const someAgreed = kvittens.linkedPersons.some(
				(linkedPerson) => linkedPerson.userHasAnswered
			);
			const allAgreed = kvittens.linkedPersons.every(
				(linkedPerson) => linkedPerson.userHasAnswered
			);

			students[kvittens.personSSNo].answers.push({
				templateId: kvittens.templateId,
				status: someAgreed
					? allAgreed
						? KvittensStatus.Approved
						: KvittensStatus.NotAnsweredByAll
					: KvittensStatus.NotAnswered,
			});
		});

		const returning = {
			templates: data.kvittensTemplateList,
			students: Object.values(students),
		};

		log('Returning:', returning);

		if (returning) {
			return [200, returning];
		} else {
			return [404];
		}
	});
}
