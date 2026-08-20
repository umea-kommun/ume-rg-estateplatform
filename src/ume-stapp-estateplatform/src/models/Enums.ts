/**
 * Size for app content. Default is used to show a public form.
 * Wide is used to provide more space for content, for example in admin.
 */
export enum AppContentSize {
	Narrow = 'Size-Narrow',
	Default = 'Size-Default',
	Wide = 'Size-Wide',
	FullWidth = 'Size-FullWidth',
}

export enum AppHeaderTitle {
	Default = 'default',
}

/** ErrorCode som kommer från backend */
export enum ErrorCode {}

/** Mutationtypes for store */
export enum MutationType {
	HideWarningMessage = 'hideWarningMessage',

	// User
	UserLogIn = 'userLogIn',
	UserLogOut = 'userLogOut',

	// Error handler
	SetError = 'setError',

	// Feedback
	FeedbackGiven = 'feedbackGiven',
}

export enum DispatchType {
	// Estate
	GetEstateSearch = 'getEstateSearch',
	GetEstateSearchGeoLocations = 'getEstateSearchGeoLocations',
	GetEstateById = 'getEstateById',
	GetEstateBuildings = 'getEstateBuildings',
	GetBuildingById = 'getBuildingById',
	GetBuildingRooms = 'getBuildingRooms',
	GetBuildingFloors = 'getBuildingFloors',
	GetFloorBlueprint = 'getFloorBlueprint',
	GetRoomById = 'getRoomById',
	GetBusinessTypes = 'getBusinessTypes',
	GetBuildingDocuments = 'getBuildingDocuments',
	DownloadBuildingDocument = 'downloadBuildingDocument',
	GetWorkOrderConfig = 'getWorkOrderConfig',
	GetWorkOrderDefaults = 'getWorkOrderDefaults',
	GetWorkOrderCategories = 'getWorkOrderCategories',
	SubmitFaultReport = 'submitFaultReport',
	SubmitEstateOrder = 'submitEstateOrder',
	SetFavorite = 'setFavorite',
	UnsetFavorite = 'unsetFavorite',
	GetFavorites = 'getFavorites',

	// Feedback
	FeedbackRate = 'feedbackRate',
	FeedbackComment = 'feedbackComment',
}

export enum EstateType {
	Estate = 'estate',
	Building = 'building',
	Room = 'room',
}

export enum ActiveMapType {
	Map = 'map',
	Blueprint = 'blueprint',
}

export enum EstateFaultLocation {
	Indoor = 'indoor',
	Outdoor = 'outdoor',
}

export enum MapBaseLayer {
	Lovisa = 'Lovisa',
	Ortofoto = 'Ortofoto',
}

export enum EstateOrderCategory {
	TownHallService = 'townHallService', // Stadshusservice
	BuildingService = 'buildingService', // Byggservice
	FacilityService = 'facilityService', // Verksamhetsvaktmästare
	SpaceRequirement = 'spaceRequirement', // Förändrade lokalbehov
}

export enum ExternalOwnerStatus {
	Egen = 'Egen',
	Inhyrd = 'Inhyrd',
}
