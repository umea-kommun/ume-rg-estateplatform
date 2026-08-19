// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/plugins/auth/index.ts @ 84b4a5dc
import Config from '@/Config';
import store from '@/store/store';
import ErrorService from '@/utils/ErrorService';
import { Router } from 'vue-router';
import AuthConfig from './AuthConfig';
import IAuthManager from './IAuthManager';
import authLoader from './Oauth';
import { EstateRoutes } from '@/router/routes';
import { MutationType } from '@/models/Enums';

let auth: IAuthManager;

export default function Auth(options?: any): IAuthManager {
	const config = new AuthConfig(options.config);
	const _auth = authLoader(config) as IAuthManager;

	// Check if jwt expired every fifth second, and in that case logout
	const handleExpiredJwt = (): void => {
		if (store.state.user.isAuthenticated && store.state.user.exp) {
			const expireDate = new Date(store.state.user.exp * 1000);
			if (expireDate.getTime() < new Date().getTime()) {
				_auth.logoutRedirectingToStartPage(
					store.state.user.authClientName,
					'logoutReason=sessionExpired'
				);
			}
		}
	};
	handleExpiredJwt();
	setInterval(handleExpiredJwt, 5000);

	auth = _auth;
	return _auth;
}

export function useAuthMiddleware(router: Router): void {
	router.beforeEach((to, from, next) => {
		let loadNextMiddleware = true;

		if (store.state.error?.errorPage?.visible) {
			store.commit(MutationType.SetError, null);
		}

		if (
			(to.meta.requiresInternalLogin || to.meta.requiresGroup) &&
			!store.state.user.isAuthenticated
		) {
			// User needs to log in to access this page
			return next({
				name: 'AuthLogin',
				query: {
					comeBack: to.path.substring(1), // Return to this page after login
				},
			});
		} else if (
			to.meta.requiresUnauthenticated &&
			store.state.user.isAuthenticated
		) {
			// User can't be logged in when accessing this page
			return next({
				name: EstateRoutes.Search,
			});
		}
		if (
			to.meta.requiresInternalLogin &&
			store.state.user.authClientName !==
				Config.VUE_APP_AUTH_PUBLIC_AD_CLIENT_NAME
		) {
			// User is not logged in with AD, deny access
			return next({
				name: EstateRoutes.Search,
			});
		}

		if (
			loadNextMiddleware &&
			to.meta.requiresGroup &&
			(store.state.user.groups ?? []).indexOf(
				to.meta.requiresGroup as string
			) === -1
		) {
			// User does not have access to this page
			ErrorService.onError({
				err: 'Permission Denied',
				log: false,
				errorPage: {
					visible: true,
					titleKey: 'app.error.permissionDenied',
					hideReport: true,
				},
			});
			loadNextMiddleware = false;
		}

		if (loadNextMiddleware) {
			next();
		}
	});
}

export const b64DecodeUnicode = (str: string): string => {
	// // Going backwards: from bytestream, to percent-encoding, to original string.
	return decodeURIComponent(
		atob(str)
			.split('')
			.map((c) => {
				return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
			})
			.join('')
	);
};

// declare module 'vue/types/vue' {
//     interface Vue {
//         readonly $auth: IAuthManager & IAuthManager;
//     }
// }
