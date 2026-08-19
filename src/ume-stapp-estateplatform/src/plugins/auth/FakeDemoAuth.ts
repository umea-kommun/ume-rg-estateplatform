// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/plugins/auth/FakeDemoAuth.ts @ 84b4a5dc
/* eslint-disable @typescript-eslint/no-explicit-any */
/* eslint-disable @typescript-eslint/no-unused-vars */
import { IUser, IUserContactInfo } from '@/models/Interfaces';
import { MutationType } from '@/models/Enums';
import store from '@/store/store';
import IAuthManager from './IAuthManager';
import AuthConfig from './AuthConfig';
import IAuthClientConfig from './IAuthClientConfig';

class FakeDemoAuth implements IAuthManager {
	private config: AuthConfig;

	public constructor(config: AuthConfig) {
		this.config = config;
	}

	public loginCitizen(
		routeAfterLogin: string,
		authClientName: string[]
	): void {
		this.login(routeAfterLogin, this.config.publicScope, authClientName);
	}

	public loginInternal(routeAfterLogin: string): void {
		this.login(routeAfterLogin, this.config.internalScope);
	}

	public getAllClientsConfig(): IAuthClientConfig[] {
		return this.config.getAllClientsConfig();
	}

	public getAllAuthClientUsedForCitizenLogin(): Array<{
		displayName: string;
		name: string;
		logo: string;
	}> {
		return this.config.getPublicAuthClientConfigs().map((config) => {
			return {
				displayName: config.clientDisplayName,
				name: config.clientName,
				logo: config.clientLogo,
			};
		});
	}

	public getDefaultAuthClientUsedForCitizenLogin(): string {
		return this.config.getPublicAuthClientConfigs()[0].clientName;
	}

	public getAuthClientLogo(authClientName: string): string {
		return this.config.getClientConfigByName(authClientName).clientLogo;
	}

	public getAuthClientsUsedForCitizenLogin(): Array<{
		displayName: string;
		name: string;
	}> {
		return this.config.getPublicAuthClientConfigs().map((config) => {
			return {
				displayName: config.clientDisplayName,
				name: config.clientName,
			};
		});
	}

	public logoutRedirectingToStartPage(
		authClientName: string,
		extraQueryParams: string = ''
	): void {
		store.commit(MutationType.UserLogOut);
		let urlAfterLogout = window.location.origin;
		urlAfterLogout +=
			extraQueryParams === '' ? '/' : '/?' + extraQueryParams;
		window.location.href = urlAfterLogout;
	}

	public logout(routeAfterLogin: string): void {
		store.commit(MutationType.UserLogOut);
		window.location.href = window.location.origin + routeAfterLogin;
	}

	public async handleLoginCallbackAsync(
		state: string,
		code: string
	): Promise<string> {
		throw new Error('This method should not have to be implemented...');
	}

	private login(
		routeAfterLogin: string,
		scope: string,
		authClientName: any = null
	): void {
		const isAdminLogin = scope === this.config.adminScope;
		if (authClientName[0] === 'AllLoginMethods') {
			authClientName = 'Publik';
		} else if (authClientName && Array.isArray(authClientName)) {
			authClientName = authClientName[0];
		}
		const userContactInfo = {
			socialSecurityNumber: isAdminLogin ? '' : '19831108xxyy',
			firstName: isAdminLogin ? 'Admin' : 'Uma',
			lastName: isAdminLogin ? '' : 'Svensson',
			email: 'test@website.com',
			address: '',
			postalNumber: '',
			city: '',
		} as IUserContactInfo;
		const jwtPayload = {
			...userContactInfo,
			fullName:
				userContactInfo.firstName + ' ' + userContactInfo.lastName,
			token: 'fake-demo-jwt',
			scope: [scope],
			userContactInfo,
			exp: new Date().getTime() + 10 * 60 * 60, // 10h expiretime on token
			isAuthenticated: true,
			protectedIdentity: 'YES',
			authClientName,
		} as IUser;
		store.commit(MutationType.UserLogIn, { jwtPayload });
		window.location.replace(routeAfterLogin);
	}
}

const loader = (config: any): FakeDemoAuth => {
	return new FakeDemoAuth(config);
};

export default loader;
