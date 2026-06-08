import {
	EstateFaultLocation,
	EstateOrderCategory,
	EstateType,
	ExternalOwnerStatus,
} from './Enums';

export interface IEstateSearchResultEntry {
	id: number;
	type: EstateType;
	name: string;
	popularName: string | null;
	municipalityArea: string | null;
	operationalArea?: string | null;
	imageUrl: string | null;
	isFavorite: boolean;
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
	isFavorite: boolean;
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
	isFavorite: boolean;
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
	status: ExternalOwnerStatus | null;
	name: string | null;
	note: string | null;
}

export interface IBuildingContact {
	name: string;
	phone?: string | null;
	email?: string | null;
}

export interface IBuildingContactPersons {
	propertyManager: IBuildingContact | null;
	operationsManager: IBuildingContact | null;
	operationCoordinator: IBuildingContact | null;
	rentalAdministrator: IBuildingContact | null;
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
	numDocuments: number | null;
	imageUrl: string | null;
	isFavorite: boolean;
	workOrderTypes: string[];
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
	buildingId: number;
	isFavorite: boolean;
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

export interface IBuildingDocument {
	id: number;
	name: string;
	directoryId: number | null;
	sizeInBytes: number | null;
	categoryId: number | null;
	categoryName: string | null;
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
	grossArea?: number;
}

export interface IMapState {
	lon: number;
	lat: number;
	zoom: number;
}

export interface SearchFilter {
	businessTypes?: number[];
}

export interface ISubmitEstateFaultReport {
	buildingId: number;
	location: EstateFaultLocation;
	roomId: number | undefined;
	description: string;
	attachments: File[];
	notifierName: string;
	notifierEmail: string;
	notifierPhone: string;
}

export interface ISubmitEstateOrder {
	buildingId: number;
	category: EstateOrderCategory;
	/**
	 * Optional Pythagoras leaf category chosen by the user (space requirement flow).
	 * When set, the backend uses it directly and bypasses the LLM classifier.
	 */
	categoryId?: number;
	roomId: number | undefined;
	description: string;
	attachments: File[];
	notifierName: string;
	notifierEmail: string;
	notifierPhone: string;
}

/** A selectable Pythagoras leaf category from GET /workorders/categories. */
export interface IWorkOrderCategoryOption {
	id: number;
	name: string;
}
