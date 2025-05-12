export default class OauthConfig {
	public static Scope = 'admin:bot';
	public clientId: string;
	public loginUrl: string;
	public redirectUrl: string;
	public fetchTokenApiUrl: string;
	public logoutUrl: string;

	public constructor(env: any) {
		this.clientId = env.VUE_APP_OAUTH_ADMIN_CLIENT_ID;
		this.loginUrl = env.VUE_APP_OAUTH_LOGIN_URL;
		this.redirectUrl = env.VUE_APP_OAUTH_REDIRECT_URL;
		this.fetchTokenApiUrl = env.VUE_APP_OAUTH_FETCH_TOKEN_API_URL;
		this.logoutUrl = env.VUE_APP_OAUTH_LOGOUT_URL;
		if (!this.clientId) {
			throw new Error('No oauth client id for admin is configured');
		}
		if (!this.loginUrl) {
			throw new Error('No oauth login url is configured');
		}
		if (!this.redirectUrl) {
			throw new Error('No oauth redirect url is configured');
		}
		if (!this.fetchTokenApiUrl) {
			throw new Error('No api url used to fetch tokens is configured');
		}
		if (!this.logoutUrl) {
			throw new Error('No oauth logout url is configured');
		}
	}
}
