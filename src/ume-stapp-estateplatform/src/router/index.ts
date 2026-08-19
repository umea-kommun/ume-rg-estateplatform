import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import PageNotFound from '@/components/app/PageNotFound.vue';
import { useAuthMiddleware } from '@/plugins/auth';
import AuthCallback from '@/components/auth/AuthCallback.vue';
import AuthLogin from '@/components/auth/AuthLogin.vue';
import { AppRoutes, EstateRoutes } from './routes';
import { AppContentSize } from '@/models/Enums';
import { useFeatureFlags } from '@/utils/useFeatureFlags';

/**
 * Path rendered when a route's feature flag is off and there is no enabled
 * page to fall back to. It has no route record of its own, so the catch-all
 * picks it up and renders PageNotFound.
 */
const FEATURE_UNAVAILABLE_PATH = '/funktion-otillganglig';

const routes: Array<RouteRecordRaw> = [
	// Estate routes - gated by runtime feature flags
	{
		path: '/',
		name: EstateRoutes.Search,
		component: () => import('@/components/estate/search/EstateSearch.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresFeature: 'EstateService',
			contentSize: AppContentSize.FullWidth,
		},
	},
	{
		path: '/felanmalan',
		name: EstateRoutes.FaultReport,
		component: () =>
			import('@/components/estate/faultReport/EstateFaultReport.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresFeature: 'ErrorReport',
		},
	},
	{
		path: '/bestallning',
		name: EstateRoutes.Order,
		component: () => import('@/components/estate/order/EstateOrder.vue'),
		meta: {
			requiresInternalLogin: true,
			requiresFeature: 'ErrorReport',
		},
	},
	{
		path: '/lokalbehov',
		name: EstateRoutes.SpaceRequirement,
		component: () =>
			import(
				'@/components/estate/spaceRequirement/EstateSpaceRequirement.vue'
			),
		meta: {
			requiresInternalLogin: true,
			requiresFeature: 'ErrorReport',
		},
	},
	{
		path: '/fastighet/:estateId/:slug?',
		name: EstateRoutes.EstateDetails,
		component: () => import('@/components/estate/estate/EstateDetails.vue'),
		props: true,
		meta: {
			requiresInternalLogin: true,
			requiresFeature: 'EstateService',
			contentSize: AppContentSize.FullWidth,
		},
	},
	{
		path: '/byggnad/:buildingId/:slug?',
		name: EstateRoutes.BuildingDetails,
		component: () =>
			import('@/components/estate/building/BuildingDetails.vue'),
		props: true,
		meta: {
			requiresInternalLogin: true,
			requiresFeature: 'EstateService',
			contentSize: AppContentSize.FullWidth,
		},
	},

	/** Authentication */
	{
		component: AuthLogin,
		name: AppRoutes.AuthLogin,
		path: '/logga-in',
		props: true,
		meta: {
			requiresUnauthenticated: true,
		},
	},
	{
		component: AuthCallback,
		name: AppRoutes.AuthCallback,
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
	scrollBehavior(to, from, savedPosition) {
		if (to.hash) {
			return {
				el: to.hash,
				top: 100,
			};
		}

		// back/forward button
		if (savedPosition) return savedPosition;

		// same page (e.g., only query changed) -> keep scroll
		if (to.path === from.path) return false;

		// normal navigation -> scroll to top
		return { left: 0, top: 0 };
	},
});

useAuthMiddleware(router);

const { loadFeatures, isEnabled } = useFeatureFlags();

router.beforeEach(async (to) => {
	await loadFeatures();
	const requiredFeature = to.meta.requiresFeature as string | undefined;
	if (requiredFeature && !isEnabled(requiredFeature)) {
		// Mina sidor sent gated routes back to /internt. Here the start page is
		// the search view, so fall back to it - unless that is what was gated,
		// in which case there is no enabled page and 404 is the honest answer.
		return to.path === '/'
			? { path: FEATURE_UNAVAILABLE_PATH }
			: { path: '/' };
	}
});

export default router;
