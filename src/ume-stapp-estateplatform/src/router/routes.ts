export enum AppRoutes {
	AuthLogin = 'AuthLogin',
	AuthCallback = 'AuthCallback',
}

export enum EstateRoutes {
	Home = 'Estate.EstateHome',
	Search = 'Estate.EstateSearch',
	AboutWebsite = 'Estate.AboutWebsite',
	ReleaseNotes = 'Estate.ReleaseNotes',
	FaultReport = 'Estate.FaultReport',
	Order = 'Estate.Order',
	SpaceRequirement = 'Estate.SpaceRequirement',

	EstateDetails = 'Estate.EstateDetails',

	BuildingDetails = 'Estate.BuildingDetails',
}

/** Element id of the "Mina ärenden" section on the start page. */
export const MyErrandsId = 'mina-arenden';

/** Start page scrolled to "Mina ärenden" (see scrollBehavior in router/index.ts). */
export const MyErrandsRoute = {
	name: EstateRoutes.Home,
	hash: `#${MyErrandsId}`,
};
