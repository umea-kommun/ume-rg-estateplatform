import { createStore, StoreOptions } from 'vuex';
import { IRootState, IUser } from '@/models/Interfaces';

import Config from '@/utils/Config';
import actions from './actions';
import mutations from './mutations';
import VuexPersist from 'vuex-persist';
import kvittensStore from './kvittens/store';
import passwordStore from './password/store';

// export default createStore({
//   state: {},
//   getters: {},
//   mutations: {},
//   actions: {},
//   modules: {},
// });

const state: IRootState = {
	user: {
		isAuthenticated: false,
		userId: '',
		authClientName: '',
	} as IUser,
	consumer: {},
	tester: {},
	agent: {},

	guardianUser: null,
};

const vuexPersistToSessionStorage = new VuexPersist({
	key: 'MyPagesVueApp',
	reducer: (state: IRootState) => ({
		user: state.user,
		tester: { testAsPerson: state.tester?.testAsPerson },
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
		kvittens: kvittensStore,
		password: passwordStore,
	},
};

export default createStore<IRootState>(store);
