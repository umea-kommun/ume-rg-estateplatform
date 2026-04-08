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
}

export enum ExternalOwnerStatus {
	Egen = 'Egen',
	Inhyrd = 'Inhyrd',
}
