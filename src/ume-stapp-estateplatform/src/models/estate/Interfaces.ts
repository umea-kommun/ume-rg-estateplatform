import { EstateType } from './Enums';

export interface IEstateSearchResultEntry {
	id: number;
	type: EstateType;
	name: string;
	popularName: string | null;
	municipalityArea: string | null;
	operationalArea?: string | null;
	imageUrl: string | null;
	address: {
		street: string;
		zipCode: string;
		city: string;
	} | null;
	metrics: {
		buildingCount: number | null;
		floorCount: number | null;
		roomCount: number | null;
		areaSqm: number | null;
	};
	ancestors: {
		id: number;
		name: string;
		popularName: string | null;
		type: EstateType;
	}[];
	geoLocation: {
		lat: number;
		lon: number;
	} | null;
}

export interface IEstateDetails {
	id: number;
	type: EstateType;
	name: string;
	popularName: string | null;
	municipalityArea: string | null;
	operationalArea?: string | null;
	administrativeArea?: string | null;
	externalOwnerInfo: IExternalOwnerInfo | null;
	metrics: {
		buildingCount: number | null;
		areaSqm: number | null;
	};
}

export interface IEstateBuilding {
	id: number;
	name: string;
	popularName: string | null;
	grossArea: number;
	imageUrl: string | null;
	metrics: {
		floorCount: number | null;
		roomCount: number | null;
	};
	address: {
		street: string;
		zipCode: string;
		city: string;
	} | null;
	geoLocation: {
		lat: number;
		lon: number;
	} | null;
}

export interface IBuildingNoticeBoard {
	text: string;
	startDate: string;
	endDate: string;
}

export interface IExternalOwnerInfo {
	status: string | null;
	name: string | null;
	note: string | null;
}

export interface IBuildingContactPersons {
	propertyManager: string | null;
	operationsManager: string | null;
	operationCoordinator: string | null;
	rentalAdministrator: string | null;
}

export interface IBuildingGeoLocation {
	id: number;
	geoLocation: {
		lat: number;
		lon: number;
	};
}
export interface IBuildingDetails {
	id: number;
	type: EstateType;
	name: string;
	popularName: string | null;
	blueprintAvailable: boolean;
	imageUrl: string | null;
	address: {
		street: string;
		zipCode: string;
		city: string;
	} | null;
	metrics: {
		yearOfConstruction: string | null;
		floorCount: number | null;
		roomCount: number | null;
		areaSqm: number | null;
	};
	geoLocation: {
		lat: number;
		lon: number;
	} | null;
	estate: {
		id: number;
		name: string;
		popularName: string | null;
	};
	region: {
		id: number;
		name: string;
	};
	externalOwnerInfo: IExternalOwnerInfo | null;
	noticeBoard: IBuildingNoticeBoard | null;
	contactPersons: IBuildingContactPersons | null;
}

export interface IBuildingRoom {
	id: number;
	name: string;
	popularName: string | null;
	grossArea: number;
	floorName: string;
	floorId: number;
}

export interface IBuildingFloor {
	id: number;
	name: string;
	popularName: string | null;
}

export interface IBusinessType {
	id: number;
	name: string;
}

export interface IBlueprintPosition {
	s: number;
	tx: number;
	ty: number;
}

export interface IMapPoint {
	id: string | number;
	type: EstateType;
	lon: number;
	lat: number;
}

export interface IMapState {
	lon: number;
	lat: number;
	zoom: number;
}

export interface SearchFilter {
	businessTypes?: number[];
}
