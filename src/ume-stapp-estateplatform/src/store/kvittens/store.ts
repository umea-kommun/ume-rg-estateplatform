import { IKvittensState } from '@/models/kvittens/Interfaces';
import actions from './actions';
import mutations from './mutations';
import getters from './getters';

const state: IKvittensState = {};

export default {
	state,
	actions,
	mutations,
	getters,
};
