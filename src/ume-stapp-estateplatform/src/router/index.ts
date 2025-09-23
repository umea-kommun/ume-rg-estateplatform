import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import AppStart from '@/components/app/AppStart.vue';
import PageNotFound from '@/components/app/PageNotFound.vue';
import ConsentStart from '@/components/external/consent/ConsentStart.vue';
import KvittensStart from '@/components/external/kvittens/KvittensStart.vue';
import KvittensDetails from '@/components/external/kvittens/KvittensDetails.vue';
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
		path: '/samtycken',
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
	{
		path: '/betyg',
		name: MyPagesRoutes.GradeStart,
		component: () => import('@/components/external/grade/GradeStart.vue'),
		meta: {
			requiresExternalLogin: true,
			contentSize: AppContentSize.Wide,
			breadcrumb: () => [{ name: 'AppStart', to: '/' }],
		},
	},

	/** Internal routes */
	{
		path: '/internt',
		name: MyPagesRoutes.InternalStart,
		component: () => import('@/components/internal/InternalStart.vue'),
		meta: {
			requiresInternalLogin: true,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.Internal,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/samtycken/mallar',
		name: MyPagesRoutes.InternalConsentTemplateList,
		component: () =>
			import(
				'@/components/internal/consent/templateAdmin/ConsentTemplateList.vue'
			),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_TEMPLATE_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.AdminConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/samtycken/mallar/:templateGuid',
		name: MyPagesRoutes.InternalConsentTemplateEdit,
		props: true,
		component: () =>
			import(
				'@/components/internal/consent/templateAdmin/ConsentTemplateEdit.vue'
			),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_TEMPLATE_ID,
			contentSize: AppContentSize.Narrow,
			title: AppHeaderTitle.AdminConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/samtycken/svar',
		name: MyPagesRoutes.InternalConsentConsumerList,
		component: () =>
			import(
				'@/components/internal/consent/consumer/ConsentConsumerOverview.vue'
			),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.InternalConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/samtycken/svar/:templateGuid,:groupId',
		name: MyPagesRoutes.InternalConsentConsumerDetails,
		component: () =>
			import(
				'@/components/internal/consent/consumer/ConsentConsumerDetails.vue'
			),
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
		path: '/internt/samtycken/ombud',
		name: MyPagesRoutes.InternalConsentAgentStart,
		component: () =>
			import('@/components/internal/consent/agent/ConsentAgentStart.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.AgentConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/samtycken/ombud/svara',
		name: MyPagesRoutes.InternalConsentAgentEdit,
		component: () =>
			import('@/components/internal/consent/agent/ConsentAgentEdit.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_CONSENT_CONSUMER_ID,
			contentSize: AppContentSize.Narrow,
			title: AppHeaderTitle.AgentConsent,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/kvittens',
		name: MyPagesRoutes.InternalKvittensSummary,
		component: () =>
			import('@/components/internal/kvittens/KvittensSummary.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_KVITTENS_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.InternalKvittens,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/kvittens/ombud',
		name: MyPagesRoutes.InternalKvittensAgent,
		component: () =>
			import('@/components/internal/kvittens/KvittensAgent.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_KVITTENS_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.AgentKvittens,
			breadcrumb: () => [],
		},
	},
	{
		path: '/internt/tilldelade-losenord',
		name: MyPagesRoutes.InternalDefaultPassword,
		component: () =>
			import('@/components/internal/password/DefaultPasswords.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresGroup: Config.VUE_APP_AUTH_GROUP_PASSWORD_CONSUMER_ID,
			contentSize: AppContentSize.Wide,
			title: AppHeaderTitle.InternalDefaultPasswords,
			breadcrumb: () => [],
		},
	},

	/** Authentication */
	{
		component: AuthLogin,
		name: MyPagesRoutes.AuthLogin,
		path: '/logga-in',
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
