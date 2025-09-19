import Config from '@/Config';
import {
	ConsentTemplateStatus,
	MutationType,
	TemplateConnectionType,
	UserConsentStatus,
} from '@/models/Enums';
import {
	IChildConsent,
	IConsentTemplate,
	IRootState,
	IChildConsentRequest,
	IGuardianConsent,
	IConsentTemplateWithConsents,
	IAgentConsent,
	IChild,
} from '@/models/Interfaces';
import setupMock from '@/store/mock';
import Axios, { AxiosError } from 'axios';
import { ActionContext } from 'vuex';
import ErrorService, { ComposedError } from '@/utils/ErrorService';
import { mappingHelper } from '../store/mappingHelper';

const httpClient = Axios.create();
if ((Config.VUE_APP_MOCK_DATA || '').trim() === 'yes') {
	console.warn('Using Mocked Axios Client!');
	setupMock(httpClient);
}

export default {
	async getChildren(context: ActionContext<IRootState, IRootState>) {
		try {
			if (context.state.guardianUser?.children) {
				return;
			}
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CHILDREN + '/children',
				{
					headers: {
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);

			const items: IChild[] = mappingHelper.mapResponseChildDataToItem(
				response.data
			);

			context.commit(MutationType.GetChildren, items);
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async getConsent(
		context: ActionContext<IRootState, IRootState>,
		payload: { childConsentRequest: IChildConsentRequest }
	) {
		try {
			const response = await httpClient.post(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT + '/childconsent',
				JSON.stringify(payload.childConsentRequest),
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);
			if (response?.data) {
				const responseAsGuardianConsent: IGuardianConsent =
					mappingHelper.mapResponseDataToGuardianConsent(
						response.data
					);

				context.commit(
					MutationType.GetGuardianConsent,
					responseAsGuardianConsent
				);
			}
		} catch (err) {
			ErrorService.onError({ err });
			// We couldn't fetch the consent, set state consent to undefined so we don't show a previous consent
			context.commit(MutationType.GetGuardianConsent, undefined);
		}
	},

	async getConsentList(
		context: ActionContext<IRootState, IRootState>,
		{ hideError } = { hideError: false }
	) {
		try {
			if (context.state.childConsentList) {
				return;
			}
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT +
					'/childrenconsentlist',
				{
					headers: {
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);

			if (response.data) {
				const responseData: {
					title: string;
					childName: string;
					childSSNo: string;
					templateGuid: string;
					consentStatus: 1 | 2 | 3 | 4;
					userStatus: 0 | 1 | 2 | 3;
					isActive: boolean;
				}[] = response.data;

				const responeAsIChildConsentList: IChildConsent[] =
					responseData.map<IChildConsent>((response) => {
						const childConsent: IChildConsent = {
							title: response.title,
							childName: response.childName,
							childSSNo: response.childSSNo,
							templateGuid: response.templateGuid,
							consentStatus: response.consentStatus,
							userStatus: response.userStatus,
							isActive: response.isActive,
						};
						return childConsent;
					});

				context.commit(
					MutationType.GetGuardianConsentList,
					responeAsIChildConsentList
				);
			} else {
				context.commit(MutationType.GetGuardianConsentList, []);
			}
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

	async updateConsent(
		context: ActionContext<IRootState, IRootState>,
		payload: {
			templateGuid: string;
			childSSN: string;
			guardianStatus: UserConsentStatus;
			stamp: string;
			signType: string;
		}
	) {
		try {
			const response = await httpClient.post(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT +
					'/SendInGuardianAnswer',
				JSON.stringify(payload),
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);

			if (response?.data) {
				const updatedGuardianConsent: IGuardianConsent =
					mappingHelper.mapResponseDataToGuardianConsent(
						response.data
					);

				context.commit(
					MutationType.GetGuardianConsent,
					updatedGuardianConsent
				);
				context.commit(MutationType.UpdateGuardianConsentListAnswer, {
					childSSN: updatedGuardianConsent.childSSNo,
					templateGuid: updatedGuardianConsent.templateGuid,
					guardianStatus: payload.guardianStatus,
					consentStatus: updatedGuardianConsent.consentStatus,
				});
			}
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async sendError(
		context: ActionContext<IRootState, IRootState>,
		{ error }: { error: ComposedError }
	) {
		if (!Config.VUE_APP_SEND_ERROR_API_URL) {
			return;
		}
		if (!context.state.user?.token) {
			// Remove this when we can send unauthenticated errors
			return;
		}

		try {
			await httpClient.post(
				Config.VUE_APP_SEND_ERROR_API_URL,
				JSON.stringify(error),
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);
		} catch (err) {
			return;
		}
	},

	async getConsentTemplates(context: ActionContext<IRootState, IRootState>) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE,
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);

			if (response?.data) {
				context.commit(MutationType.GetConsentTemplates, response.data);
			}
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},

	async getConsentTemplate(
		context: ActionContext<IRootState, IRootState>,
		{ guid }: { guid: string }
	) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE +
					'/templatebytemplateguid?templateGuid=' +
					guid,
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);

			if (response?.data) {
				context.commit(MutationType.GetConsentTemplate, response.data);
			}
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},

	async saveConsentTemplate(
		context: ActionContext<IRootState, IRootState>,
		{ template, publish }: { template: IConsentTemplate; publish: boolean }
	) {
		try {
			const templateLocal = {
				...template,
				status: publish
					? ConsentTemplateStatus.Published
					: ConsentTemplateStatus.Draft,
			};

			let response;
			if (templateLocal.guid) {
				// Update existing template
				response = await httpClient.put(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE,
					JSON.stringify(templateLocal),
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization: 'Bearer ' + context.state.user.token,
						},
					}
				);
			} else {
				// Create new template
				response = await httpClient.post(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEMPLATE,
					JSON.stringify(templateLocal),
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization: 'Bearer ' + context.state.user.token,
						},
					}
				);
			}

			if (response?.data) {
				context.commit(MutationType.GetConsentTemplate, response.data);
			}
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async getConsentTemplateUnitTypes(
		context: ActionContext<IRootState, IRootState>
	) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION +
					'/getAllSchoolForms',
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);
			if (response?.data?.length) {
				context.commit(
					MutationType.GetConsentTemplateUnitTypes,
					response.data
				);
			}
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async getConsentTemplateUnits(
		context: ActionContext<IRootState, IRootState>
	) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION +
					'/getAllSchools',
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);
			if (response?.data) {
				const groups = response.data.map(
					(item: { [key: string]: string }) => {
						return {
							refId: item.id,
							title: item.name,
							type: TemplateConnectionType.Unit,
							schoolTypes: item.schoolForms,
						};
					}
				);
				context.commit(MutationType.GetConsentTemplateGroups, groups);
			}
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async getConsentTemplateUnitGroups(
		context: ActionContext<IRootState, IRootState>,
		params: unknown //{ schoolId: string, schoolForms: string[] | undefined }
	) {
		const response = await httpClient.post(
			Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION +
				'/getGroupsInSchool',
			params,
			{
				headers: {
					'Content-Type': 'application/json',
					Authorization: 'Bearer ' + context.state.user.token,
				},
			}
		);
		if (response?.data) {
			const groups = response.data.map(
				(item: { [key: string]: string }) => {
					return {
						refId: item.id,
						title: item.name,
						type: item.type,
						schoolTypes: item.schoolTypes,
						startDate: item.startDate,
						endDate: item.endDate,
					};
				}
			);
			return groups;
		}
		return [];
	},

	async getConsentConsumerList(
		context: ActionContext<IRootState, IRootState>
	) {
		try {
			let response;
			if (context.state.tester.testAsPerson) {
				response = await httpClient.post(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSUMER +
						'/ConsumerOverviewDataTest',
					JSON.stringify({
						name: context.state.tester.testAsPerson.name,
						personnummer:
							context.state.tester.testAsPerson
								.socialSecurityNumber,
					}),
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization: 'Bearer ' + context.state.user.token,
						},
					}
				);
			} else {
				response = await httpClient.get(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSUMER +
						'/ConsumerOverviewData',
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization: 'Bearer ' + context.state.user.token,
						},
					}
				);
			}
			context.commit(MutationType.GetConsumerList, {
				templates: response?.data.templates ?? [],
				groups: response?.data.groups ?? [],
			});
		} catch (err) {
			ErrorService.onError({ err, errorPage: { visible: true } });
		}
	},

	async getConsentConsumerTemplateWithConsents(
		context: ActionContext<IRootState, IRootState>,
		{ templateGuid, groupId }: { templateGuid: string; groupId: string }
	) {
		try {
			let response;
			if (context.state.tester.testAsPerson) {
				response = await httpClient.post(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSUMER +
						'/consumerTemplateWithConsentsTest',
					{
						loggedInSsn:
							context.state.tester.testAsPerson
								.socialSecurityNumber,
						templateGuid,
						groupId,
					},
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization: 'Bearer ' + context.state.user.token,
						},
					}
				);
			} else {
				response = await httpClient.get(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSUMER +
						`/consumerTemplateWithConsents?templateGuid=${templateGuid}&groupId=${groupId}`,
					{
						headers: {
							'Content-Type': 'application/json',
							Authorization: 'Bearer ' + context.state.user.token,
						},
					}
				);
			}

			const data = response.data;
			const templateWithConsents: IConsentTemplateWithConsents = {
				title: data.templateTitle,
				text: data.templateText,
				group: {
					refId: data.groupRefId,
					title: data.groupName,
					type: data.groupType,
				},
				publishedDate: data.publishedDate,
				expireDate: data.expireDate,
				consents: data.consents.map((consentData: any) => ({
					name: consentData.childName,
					status: consentData.status,
				})),
			};
			context.commit(MutationType.GetConsumerDetails, {
				templateWithConsents,
			});
		} catch (err) {
			if ((err as AxiosError)?.response?.status === 403) {
				ErrorService.onError({
					err,
					errorPage: {
						visible: true,
						titleKey:
							'component.internal.consentConsumerDetails.accessDeniedError.title',
						messageKey:
							'component.internal.consentConsumerDetails.accessDeniedError.message',
						hideReport: true,
					},
				});
			} else {
				ErrorService.onError({
					err,
					errorPage: { visible: true },
				});
			}
		}
	},

	async getTesterSchoolUnits(context: ActionContext<IRootState, IRootState>) {
		try {
			const response = await httpClient.get(
				Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEST + '/getAllSchools',
				{
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + context.state.user.token,
					},
				}
			);
			if (response?.data) {
				const groups = response.data.map(
					(item: { [key: string]: string }) => {
						return {
							refId: item.id,
							title: item.name,
							type: TemplateConnectionType.Unit,
							schoolTypes: item.schoolForms,
						};
					}
				);
				context.commit(MutationType.GetTesterSchoolUnits, groups);
			}
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async getTesterSchoolTeachers(
		context: ActionContext<IRootState, IRootState>,
		schoolId: string
	) {
		const response = await httpClient.get(
			Config.VUE_APP_CONSENT_BRIDGE_SERVICE_TEST +
				'/getTeachersInSchool?schoolId=' +
				schoolId,
			{
				headers: {
					'Content-Type': 'application/json',
					Authorization: 'Bearer ' + context.state.user.token,
				},
			}
		);
		if (response?.data) {
			const groups = response.data.map(
				(item: { [key: string]: string }) => {
					return {
						name: item.name,
						socialSecurityNumber: item.personalNumber,
					};
				}
			);
			return groups;
		}
		return [];
	},

	async getAgentChildConsentList(
		context: ActionContext<IRootState, IRootState>,
		params: { childSSN: string }
	) {
		try {
			let URL;
			let requestPayload;

			if (context.state.tester.testAsPerson) {
				URL =
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT +
					'/consentagentconsentstest';

				requestPayload = JSON.stringify({
					childSSNo: params.childSSN,
					consentAgentSSNo:
						context.state.tester.testAsPerson.socialSecurityNumber,
				});
			} else {
				URL =
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT +
					'/consentagentconsents';

				requestPayload = JSON.stringify({ childSSNo: params.childSSN });
			}

			const response = await httpClient.post(URL, requestPayload, {
				headers: {
					'Content-Type': 'application/json',
					Authorization: 'Bearer ' + context.state.user.token,
				},
			});

			if (response?.data) {
				return response.data;
			}
		} catch (err) {
			ErrorService.onError({ err });
		}
		return [];
	},

	async getAgentConsent(
		context: ActionContext<IRootState, IRootState>,
		payload: { childConsentRequest: IChildConsentRequest }
	) {
		try {
			let URL;
			let requestPayload;
			if (context.state.tester.testAsPerson) {
				URL =
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT +
					'/consentagentchildconsenttest';

				requestPayload = JSON.stringify({
					...payload.childConsentRequest,
					consentAgentSSNo:
						context.state.tester.testAsPerson.socialSecurityNumber,
				});
			} else {
				URL =
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT +
					'/consentagentchildconsent';

				requestPayload = JSON.stringify(payload.childConsentRequest);
			}

			const response = await httpClient.post(URL, requestPayload, {
				headers: {
					'Content-Type': 'application/json',
					Authorization: 'Bearer ' + context.state.user.token,
				},
			});

			if (response?.data) {
				const responseAsGuardianConsent: IAgentConsent =
					mappingHelper.mapResponseDataToAgentConsent(response.data);

				return responseAsGuardianConsent;
			}
			return null;
		} catch (err) {
			ErrorService.onError({ err });
			return null;
		}
	},
	async agentUpdateConsent(
		context: ActionContext<IRootState, IRootState>,
		payload: {
			templateGuid: string;
			childSSN: string;
			guardianStatus: UserConsentStatus;
			guardianSSN: string;
			image: File;
			stamp: string;
			signType: string;
		}
	) {
		try {
			let URL;

			const form = new FormData();
			form.append('image', payload.image, payload.image.name);

			if (context.state.tester.testAsPerson) {
				URL =
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT +
					'/SendInGuardianAnswerTest';

				form.append(
					'body',
					JSON.stringify({
						...payload,
						image: undefined,
						consentAgentSSNo:
							context.state.tester.testAsPerson
								.socialSecurityNumber,
						consentAgentName:
							context.state.tester.testAsPerson.name,
					})
				);
			} else {
				URL =
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_CONSENT_AGENT +
					'/SendInGuardianAnswer';

				form.append(
					'body',
					JSON.stringify({
						...payload,
						image: undefined,
					})
				);
			}

			const response = await httpClient.post(URL, form, {
				headers: {
					'Content-Type': 'multipart/form-data',
					Authorization: 'Bearer ' + context.state.user.token,
				},
			});

			if (response?.data) {
				const updatedGuardianConsent: IAgentConsent =
					mappingHelper.mapResponseDataToAgentConsent(response.data);

				return updatedGuardianConsent;
			}
			return null;
		} catch (err) {
			ErrorService.onError({ err });
			return null;
		}
	},
};
