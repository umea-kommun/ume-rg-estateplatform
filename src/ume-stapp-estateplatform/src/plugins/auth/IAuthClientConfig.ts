// Duplicated from ume-rg-myplatform @ 84b4a5dc
// src/ume-stapp-minasidor/src/plugins/auth/IAuthClientConfig.ts
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
