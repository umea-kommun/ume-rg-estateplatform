import Config from '@/Config';
import setupMock from '@/store/mock';
import { createHttpClient } from '@/utils/httpClient';
import { ActionContext } from 'vuex';
import ErrorService from '@/utils/ErrorService';
import {
	IAgentKvittens,
	ICreateKvittensTemplate,
	IKvittens,
	IKvittensDetails,
	IKvittensState,
} from '@/models/kvittens/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { MutationType } from '@/models/Enums';
import kvittensMapper from './mapper';
import { mappingHelper } from '../mappingHelper';

const httpClient = createHttpClient();
if ((Config.VUE_APP_MOCK_DATA || '').trim() === 'yes') {
	console.warn('Using Mocked Axios Client!');
	setupMock(httpClient);
}

export default {
	async getKvittensList(
		context: ActionContext<IKvittensState, IRootState>,
		{ hideError } = { hideError: false }
	) {
		if (context.state.kvittensList) {
			return;
		}
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS +
					'/kvittensList',
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			const kvittensList: IKvittens[] =
				kvittensMapper.mapResponseToKvittensList(response.data);

			context.commit(MutationType.UpdateKvittensList, kvittensList);
		} catch (err) {
			if (hideError) {
				ErrorService.onError({ err, hidden: true });
			} else {
				ErrorService.onError({
					err,
					errorPage: {
						visible: true,
						hideReport: true,
					},
				});
			}
		}
	},
	async getKvittensDetails(
		context: ActionContext<IKvittensState, IRootState>,
		{ personSSNo, templateId }: { personSSNo: string; templateId: string }
	) {
		try {
			const response = await httpClient.post(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS + '/details',
				JSON.stringify({ personSSNo, templateId }),
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			const kvittensDetails: IKvittensDetails =
				kvittensMapper.mapResponseToKvittensDetails(response.data);

			return kvittensDetails;
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},
	async saveKvittensAnswer(
		context: ActionContext<IKvittensState, IRootState>,
		{
			personSSNo,
			templateId,
		}: {
			personSSNo: string;
			templateId: string;
		}
	) {
		try {
			const response = await httpClient.post(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS + '/answer',
				JSON.stringify({ personSSNo, templateId }),
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			const kvittensDetails: IKvittensDetails =
				kvittensMapper.mapResponseToKvittensDetails(response.data);

			return kvittensDetails;
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async getKvittensFilterGroups(
		context: ActionContext<IKvittensState, IRootState>
	) {
		try {
			let response;
			if (context.rootState.tester.testAsPerson) {
				response = await httpClient.post(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION +
						'/mentoredSchoolsAndClassesForKvittensTest',
					JSON.stringify({
						name: context.rootState.tester.testAsPerson.name,
						personnummer:
							context.rootState.tester.testAsPerson
								.socialSecurityNumber,
					}),
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization:
								'Bearer ' + context.rootState.user.token,
						},
					}
				);
			} else {
				let endpoint = '/mentoredSchoolsAndClassesForKvittens';
				if (context.getters.isKvittensTechnician) {
					endpoint = '/technicianSchoolsAndClassesForKvittens';
				} else if (context.getters.isSchoolAdministrator) {
					endpoint =
						'/schoolAdministratorSchoolsAndClassesForKvittens';
				}
				response = await httpClient.get(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION +
						endpoint,
					{
						headers: {
							Authorization:
								'Bearer ' + context.rootState.user.token,
						},
					}
				);
			}

			return kvittensMapper.mapResponseToSchoolsAndClasses(response.data);
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},
	async getKvittensSummary(
		context: ActionContext<IKvittensState, IRootState>,
		{ classRefId }: { classRefId: string }
	) {
		try {
			let response;
			if (context.rootState.tester.testAsPerson) {
				response = await httpClient.post(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS +
						'/summaryTest',
					JSON.stringify({
						testPerson: {
							name: context.rootState.tester.testAsPerson.name,
							personnummer:
								context.rootState.tester.testAsPerson
									.socialSecurityNumber,
						},
						groupId: classRefId,
					}),
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization:
								'Bearer ' + context.rootState.user.token,
						},
					}
				);
			} else {
				let endpoint = '/summary';
				if (context.getters.isKvittensTechnician) {
					endpoint = '/summaryTechnician';
				} else if (context.getters.isSchoolAdministrator) {
					endpoint = '/summarySchoolAdministrator';
				}
				response = await httpClient.get(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS +
						endpoint +
						`?groupId=${classRefId}`,
					{
						headers: {
							Authorization:
								'Bearer ' + context.rootState.user.token,
						},
					}
				);
			}

			return kvittensMapper.mapResponseToSummary(response.data);
		} catch (err) {
			ErrorService.onError({ err });
		}
	},
	async getAgentKvittensList(
		context: ActionContext<IKvittensState, IRootState>,
		studentSsno: string
	) {
		context.commit(MutationType.UpdateKvittensAgentList, []);
		if (!studentSsno) {
			return;
		}

		let URL;
		let requestPayload;

		if (context.rootState.tester.testAsPerson) {
			URL =
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS_AGENT +
				'/kvittensAgentGetKvittensBriefsTest';

			requestPayload = {
				pupilSsNo: studentSsno,
				impersonatedPerson: mappingHelper.mapTesterToTestPerson(
					context.rootState.tester.testAsPerson
				),
			};
		} else {
			URL =
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS_AGENT +
				'/kvittensAgentGetKvittensBriefs';

			requestPayload = { pupilSsNo: studentSsno };
		}

		const response = await httpClient.post(URL, requestPayload, {
			headers: {
				'Content-Type': 'application/json',
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		const kvittensList: IAgentKvittens[] =
			kvittensMapper.mapResponseToAgentKvittensList(response.data);

		context.commit(MutationType.UpdateKvittensAgentList, kvittensList);
	},
	async getAgentKvittensDetails(
		context: ActionContext<IKvittensState, IRootState>,
		{ studentSsno, templateId }: { studentSsno: string; templateId: string }
	) {
		let URL;
		let requestPayload;

		if (context.rootState.tester.testAsPerson) {
			URL =
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS_AGENT +
				'/kvittensAgentGetKvittensDetailsTest';

			requestPayload = {
				personSSNo: studentSsno,
				templateId: templateId,
				impersonatedPerson: mappingHelper.mapTesterToTestPerson(
					context.rootState.tester.testAsPerson
				),
			};
		} else {
			URL =
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS_AGENT +
				'/kvittensAgentGetKvittensDetails';

			requestPayload = {
				personSSNo: studentSsno,
				templateId: templateId,
			};
		}

		const response = await httpClient.post(URL, requestPayload, {
			headers: {
				'Content-Type': 'application/json',
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		const kvittensDetails: IKvittensDetails =
			kvittensMapper.mapResponseToKvittensDetails(response.data);

		return kvittensDetails;
	},
	async agentAnswerKvittens(
		context: ActionContext<IKvittensState, IRootState>,
		{
			templateId,
			subjectSsno,
			respondents,
			image,
		}: {
			templateId: string;
			subjectSsno: string;
			respondents: string[];
			image: File;
		}
	) {
		let URL;
		const form = new FormData();
		form.append('image', image, image.name);

		if (context.rootState.tester.testAsPerson) {
			URL =
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS_AGENT +
				'/kvittensAgentAnswerTest';

			form.append(
				'body',
				JSON.stringify({
					personSSNo: subjectSsno,
					templateId: templateId,
					respondentsSsno: respondents,
					impersonatedPerson: mappingHelper.mapTesterToTestPerson(
						context.rootState.tester.testAsPerson
					),
				})
			);
		} else {
			URL =
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_KVITTENS_AGENT +
				'/kvittensAgentAnswer';

			form.append(
				'body',
				JSON.stringify({
					personSSNo: subjectSsno,
					templateId: templateId,
					respondentsSsno: respondents,
				})
			);
		}

		const response = await httpClient.post(URL, form, {
			headers: {
				'Content-Type': 'multipart/form-data',
				Authorization: 'Bearer ' + context.rootState.user.token,
			},
		});

		const kvittensDetails: IKvittensDetails =
			kvittensMapper.mapResponseToKvittensDetails(response.data);

		context.commit(MutationType.UpdateAnswerInAgentKvittensList, {
			templateId,
			subjectSsno,
			linkedPersons: kvittensDetails.linkedPersons,
		});

		return kvittensDetails;
	},
	async getKvittensTemplates(
		context: ActionContext<IKvittensState, IRootState>
	) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_BASE_URL +
					'/kvittenstemplate',
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			return kvittensMapper.mapResponseToTemplateList(response.data);
		} catch (err) {
			ErrorService.onError({
				err,
				errorPage: {
					visible: true,
				},
			});
		}
	},
	async getKvittensTemplate(
		context: ActionContext<IKvittensState, IRootState>,
		{ templateId }: { templateId: string }
	) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_BASE_URL +
					'/kvittenstemplate/' +
					templateId,
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			return kvittensMapper.mapResponseToTemplate(response.data);
		} catch (err) {
			ErrorService.onError({
				err,
				errorPage: {
					visible: true,
				},
			});
		}
	},
	async createKvittensTemplate(
		context: ActionContext<IKvittensState, IRootState>,
		kvittensTemplate: ICreateKvittensTemplate
	) {
		try {
			const response = await httpClient.post(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_BASE_URL +
					'/kvittenstemplate/',
				kvittensTemplate,
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			return kvittensMapper.mapResponseToTemplate(response.data);
		} catch (err) {
			ErrorService.onError({
				err,
			});
		}
	},
	async getSchoolYearsPerSchoolForm(
		context: ActionContext<IKvittensState, IRootState>
	) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_BASE_URL +
					'/organization/getSchoolYearsPerSchoolForm',
				{
					headers: {
						Authorization: 'Bearer ' + context.rootState.user.token,
					},
				}
			);

			return kvittensMapper.mapResponseToSchoolYearsPerSchoolForm(
				response.data
			);
		} catch (err) {
			ErrorService.onError({
				err,
			});
		}
	},
};
