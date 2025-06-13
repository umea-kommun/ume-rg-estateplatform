import Config from '@/Config';
import { ActionContext } from 'vuex';
import { IRootState } from '@/models/Interfaces';
import ErrorService from '@/utils/ErrorService';
import Axios from 'axios';
import mapper from './mapper';

const httpClient = Axios.create();

export default {
	async getConsumerGroupsAndSchools(
		context: ActionContext<IRootState, IRootState>
	) {
		try {
			let response;
			if (context.rootState.tester.testAsPerson) {
				response = await httpClient.post(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_ORGANIZATION +
						'/getConsumerGroupsAndSchoolsTest',
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
				let endpoint = '/getConsumerGroupsAndSchools';
				if (context.getters.isPasswordTechnician) {
					endpoint = '/getTechnicianGroupsAndSchools';
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

			return mapper.mapResponseToPasswordGroupsAndSchools(response.data);
		} catch (err) {
			ErrorService.onError({ err });
		}
	},

	async getDefaultPasswordAssignments(
		context: ActionContext<IRootState, IRootState>,
		{ groupId }: { groupId: string }
	) {
		try {
			let response;
			if (context.rootState.tester.testAsPerson) {
				response = await httpClient.post(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_PASSWORD +
						'/getDefaultPasswordAssignmentsTest',
					JSON.stringify({
						testPerson: {
							name: context.rootState.tester.testAsPerson.name,
							personnummer:
								context.rootState.tester.testAsPerson
									.socialSecurityNumber,
						},
						groupId: groupId,
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
				let endpoint = '/getDefaultPasswordAssignments';
				if (context.getters.isPasswordTechnician) {
					endpoint = '/getDefaultPasswordAssignmentsTechnician';
				}
				response = await httpClient.get(
					Config.VUE_APP_CONSENT_BRIDGE_SERVICE_PASSWORD +
						endpoint +
						`?groupId=${groupId}`,
					{
						headers: {
							Authorization:
								'Bearer ' + context.rootState.user.token,
						},
					}
				);
			}

			return mapper.mapResponseToDefaultPasswordAssignments(
				response.data
			);
		} catch (err) {
			ErrorService.onError({ err });
		}
	},
};
