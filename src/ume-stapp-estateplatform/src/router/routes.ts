export enum MyPagesRoutes {
	AppStart = 'AppStart',

	ConsentStart = 'ConsentStart',
	ConsentEdit = 'ConsentEdit',

	KvittensStart = 'KvittensStart',
	KvittensDetails = 'KvittensDetails',

	GradeStart = 'GradeStart',

	InternalStart = 'InternalStart',
	InternalConsentTemplateList = 'Internal.ConsentTemplateList',
	InternalConsentTemplateEdit = 'Internal.ConsentTemplateEdit',
	InternalConsentConsumerList = 'Internal.ConsentConsumerList',
	InternalConsentConsumerDetails = 'Internal.ConsentConsumerDetails',
	InternalConsentAgentStart = 'Internal.ConsentAgentStart',
	InternalKvittensSummary = 'Internal.KvittensSummary',
	InternalKvittensAgent = 'Internal.KvittensAgent',
	InternalDefaultPassword = 'Internal.DefaultPassword',

	AuthLogin = 'AuthLogin',
	AuthCallback = 'AuthCallback',
}

export enum EstateRoutes {
	Search = 'Estate.EstateSearch',
	FaultReport = 'Estate.FaultReport',
	Order = 'Estate.Order',
	SpaceRequirement = 'Estate.SpaceRequirement',

	EstateDetails = 'Estate.EstateDetails',
	EstateDetailsBuildings = 'Estate.EstateDetails.Buildings',

	BuildingDetails = 'Estate.BuildingDetails',
}
