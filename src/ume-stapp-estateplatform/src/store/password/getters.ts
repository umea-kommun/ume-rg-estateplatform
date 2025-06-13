import Config from '@/Config';
import { IRootState } from '@/models/Interfaces';
import { IKvittensState } from '@/models/kvittens/Interfaces';

export default {
	isPasswordTechnician(
		state: IKvittensState,
		getters: unknown,
		rootState: IRootState
	) {
		if (rootState.user.groups) {
			return rootState.user.groups.includes(
				Config.VUE_APP_AUTH_GROUP_PASSWORD_TECHNICIAN_ID
			);
		}
		return false;
	},
};
