// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/plugins/auth/IAuthClientConfig.ts @ 84b4a5dc
export default interface IAuthClientConfig {
	clientId: string;
	clientName: string;
	clientDisplayName: string;
	clientSecret: string;
	clientLogo: string;
	authorityUrl: string;
	logoutUrl: string;
	redirectUrl: string;
	scope: string;
	internal?: boolean;
}
