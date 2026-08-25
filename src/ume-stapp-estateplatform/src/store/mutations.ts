import { MutationType } from '@/models/Enums';
import {
	IRootState,
	IUserContactInfo,
	IUser,
	IError,
} from '@/models/Interfaces';

export default {
	[MutationType.UserLogIn]: (
		state: IRootState,
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		{ jwtPayload, rawJwt, authClientName }: any
	) => {
		const userContactInfo = {
			socialSecurityNumber: jwtPayload.socialSecurityNumber || '?',
			firstName: jwtPayload.firstName || '',
			lastName: jwtPayload.lastName || '',
			address: jwtPayload.address || '',
			postalNumber: jwtPayload.postalNumber || '',
			city: jwtPayload.city || '',
			phoneNumber: jwtPayload.phoneNumber || '',
			email: jwtPayload.email || '',
		} as IUserContactInfo;

		state.user = {
			...jwtPayload,
			token: rawJwt,
			isAuthenticated: true,
			groups: jwtPayload.groups?.split(',') ?? [],
			userContactInfo,
			authClientName,
		} as IUser;
	},

	[MutationType.UserLogOut]: (state: IRootState) => {
		state.user = {
			isAuthenticated: false,
			authClientName: '',
		} as IUser;

		delete state.feedback;
	},

	[MutationType.HideWarningMessage]: (
		state: IRootState,
		hideWarningMessage: boolean
	) => {
		state.hideWarningMessage = hideWarningMessage;
	},

	[MutationType.SetError]: (state: IRootState, error: IError | null) => {
		state.error = error;
	},
};
