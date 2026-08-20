import { createStore, StoreOptions } from 'vuex';
import { IRootState, IUser } from '@/models/Interfaces';

import Config from '@/utils/Config';
import actions from './actions';
import mutations from './mutations';
import VuexPersist from 'vuex-persist';
import feedbackStore from './feedback/store';

const state: IRootState = {
	user: {
		isAuthenticated: false,
		authClientName: '',
	} as IUser,
};

const vuexPersistToSessionStorage = new VuexPersist({
	key: 'EstatePlatformVueApp',
	reducer: (state: IRootState) => ({
		user: state.user,
		feedback: state.feedback,
	}),
	storage: window.sessionStorage,
});

const store: StoreOptions<IRootState> = {
	strict: Config.NODE_ENV !== 'production',
	state,
	mutations,
	actions,
	plugins: [vuexPersistToSessionStorage.plugin],
	modules: {
		feedback: feedbackStore,
	},
};

export default createStore<IRootState>(store);
