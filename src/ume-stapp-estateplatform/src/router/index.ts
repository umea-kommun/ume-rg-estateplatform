import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import AppStart from '@/components/app/AppStart.vue';
import PageNotFound from '@/components/app/PageNotFound.vue';
import ConsentStart from '@/components/external/consent/ConsentStart.vue';
import KvittensStart from '@/components/external/kvittens/KvittensStart.vue';
import KvittensDetails from '@/components/external/kvittens/KvittensDetails.vue';
import InternalStart from '@/components/internal/InternalStart.vue';
import ConsentTemplateList from '@/components/internal/consent/templateAdmin/ConsentTemplateList.vue';
import ConsentTemplateEdit from '@/components/internal/consent/templateAdmin/ConsentTemplateEdit.vue';
import ConsentConsumerOverview from '@/components/internal/consent/consumer/ConsentConsumerOverview.vue';
import ConsentConsumerDetails from '@/components/internal/consent/consumer/ConsentConsumerDetails.vue';
import ConsentAgentStart from '@/components/internal/consent/agent/ConsentAgentStart.vue';
import ConsentAgentEdit from '@/components/internal/consent/agent/ConsentAgentEdit.vue';
import KvittensSummary from '@/components/internal/kvittens/KvittensSummary.vue';
import { useAuthMiddleware } from '@/plugins/auth';
import AuthCallback from '@/components/auth/AuthCallback.vue';
import AuthLogin from '@/components/auth/AuthLogin.vue';
import { MyPagesRoutes } from './routes';
import { AppContentSize, AppHeaderTitle } from '@/models/Enums';
import Config from '@/Config';

const routes: Array<RouteRecordRaw> = [
	/** External routes */
	{
		path: '/',
		name: MyPagesRoutes.AppStart,
		component: AppStart,
		meta: {
			requiresExternalLogin: true,
			contentSize: AppContentSize.Wide,
			breadcrumb: () => [{ name: 'AppStart', to: '/' }],
		},
	},
	{
		path: '/consent',
		name: MyPagesRoutes.ConsentStart,
		component: ConsentStart,
		meta: {
			requiresExternalLogin: true,
			contentSize: AppContentSize.Wide,
			breadcrumb: () => [{ name: 'AppStart', to: '/' }],
		},
	},
	{
		path: '/kvittens',
		name: MyPagesRoutes.KvittensStart,
		component: KvittensStart,
		meta: {
			requiresExternalLogin: true,
			contentSize: AppContentSize.Wide,
			breadcrumb: () => [{ name: 'AppStart', to: '/' }],
		},
	},
	{
		path: '/kvittens/:localId',
		name: MyPagesRoutes.KvittensDetails,
		component: KvittensDetails,
		props: true,
		meta: {
			requiresExternalLogin: true,
			contentSize: AppContentSize.Narrow,
		},
	},

	/** Internal routes */
	{
		path: '/internal',
		name: MyPagesRoutes.InternalStart,
		component: InternalStart,
		meta: {
			requiresInternalLogin: true,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.Internal,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internal/consent/template',
		name: MyPagesRoutes.InternalConsentTemplateList,
		component: ConsentTemplateList,
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_TEMPLATE_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.AdminConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internal/consent/template/:templateGuid',
		name: MyPagesRoutes.InternalConsentTemplateEdit,
		props: true,
		component: ConsentTemplateEdit,
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_TEMPLATE_ID,
			contentSize: AppContentSize.Narrow,
			title: AppHeaderTitle.AdminConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internal/consent/consumer',
		name: MyPagesRoutes.InternalConsentConsumerList,
		component: ConsentConsumerOverview,
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.InternalConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internal/consent/consumer/:templateGuid,:groupId',
		name: MyPagesRoutes.InternalConsentConsumerDetails,
		component: ConsentConsumerDetails,
		props: true,
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID,
			contentSize: AppContentSize.Narrow,
			title: AppHeaderTitle.InternalConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internal/consent/agent',
		name: MyPagesRoutes.InternalConsentAgentStart,
		component: ConsentAgentStart,
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.AgentConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internal/consent/agent/edit',
		name: MyPagesRoutes.InternalConsentAgentEdit,
		component: ConsentAgentEdit,
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID,
			contentSize: AppContentSize.Narrow,
			title: AppHeaderTitle.AgentConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internal/kvittens',
		name: MyPagesRoutes.InternalKvittensSummary,
		component: KvittensSummary,
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_KVITTENS_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.InternalKvittens,
			breadcrumb: () => [],
		},
	},

	/** Authentication */
	{
		component: AuthLogin,
		name: MyPagesRoutes.AuthLogin,
		path: '/login',
		props: true,
		meta: {
			requiresUnauthenticated: true,
		},
	},
	{
		component: AuthCallback,
		name: MyPagesRoutes.AuthCallback,
		path: '/oauthCallback',
		props: true,
		meta: {
			requiresUnauthenticated: true,
			breadcrumb: () => [],
		},
	},

	/** 404 page */
	{
		component: PageNotFound,
		path: '/:pathMatch(.*)*',
		meta: {
			contentSize: AppContentSize.Narrow,
		},
	},
];

const router = createRouter({
	history: createWebHistory(),
	routes,
	scrollBehavior() {
		return { top: 0 };
	},
});

useAuthMiddleware(router);

export default router;
