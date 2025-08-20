import Config from '@/Config';
import setupMock from '@/store/mock';
import Axios from 'axios';
import { ActionContext } from 'vuex';
import ErrorService from '@/utils/ErrorService';
import {
	IKvittens,
	IKvittensDetails,
	IKvittensState,
} from '@/models/kvittens/Interfaces';
import { IRootState } from '@/models/Interfaces';
import { MutationType } from '@/models/Enums';
import kvittensMapper from './mapper';

const httpClient = Axios.create();
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
};
