import Config from '@/Config';
import { IRootState } from '@/models/Interfaces';
import { IKvittensState } from '@/models/kvittens/Interfaces';

export default {
	isKvittensTechnician(
		state: IKvittensState,
		getters: unknown,
		rootState: IRootState
	) {
		if (rootState.user.groups) {
			return rootState.user.groups.includes(
				Config.VUE_APP_AUTH_GROUP_KVITTENS_TECHNICIAN_ID
			);
		}
		return false;
	},
	isSchoolAdministrator(
		state: IKvittensState,
		getters: unknown,
		rootState: IRootState
	) {
		if (rootState.user.groups) {
			return rootState.user.groups.includes(
				Config.VUE_APP_AUTH_GROUP_KVITTENS_SCHOOL_ADMIN_ID
			);
		}
		return false;
	},
};
