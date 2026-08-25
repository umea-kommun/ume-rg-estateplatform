// import * as Enums from '@/models/Enums';

import { AxiosError } from 'axios';
import {
	EstateFaultLocation,
	EstateOrderCategory,
	EstateType,
	ExternalOwnerStatus,
} from './Enums';
import { IFeedbackState } from './feedback/Interfaces';

/**
 * State för vuex-store
 */
export interface IRootState {
	user: IUser;
	error?: IError | null;
	hideWarningMessage?: null | boolean;

	feedback?: IFeedbackState;
}

export interface IError {
	error: unknown | Error | AxiosError;
	userMessage?: null | string;
	errorPage?: IErrorPage;
}

export interface IErrorPage {
	visible: boolean;
	title?: string;
	titleKey?: string; // i18n translation key
	message?: string;
	messageKey?: string; // i18n translation key
	hideReport?: boolean;
}

export interface IErrorToDisplay {
	title: string;
	message?: string;
}

/**
 * Interface for IUserContactInfo
 */
export interface IUserContactInfo {
	socialSecurityNumber: string;
	firstName: string;
	lastName: string;
	address: string;
	postalNumber: string;
	city: string;
	phoneNumber: string;
	email: string;
}

/**
 * User En inloggad användare
 */
export interface IUser {
	socialSecurityNumber: string;
	fullName?: string;
	firstName: string;
	lastName: string;
	token?: string;
	email: string | null;
	scope?: string[];
	userContactInfo?: IUserContactInfo | null;
	isAuthenticated: boolean;
	protectedIdentity?: string;

	/**
	 * Unix timestamp telling when user session expires
	 */
	exp?: number;

	/**
	 * The auth client used to login this user
	 */
	authClientName: string;

	/**
	 * Which idp do we have behind the user token
	 */
	idp?: string;

	/**
	 * Which ad groups the user is a member of (ids)
	 */
	groups?: string[];
}

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
	caretaker: IBuildingContact | null;
	operationsTechnician: IBuildingContact | null;
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
	/**
	 * Optional: the space requirement flow may be submitted without a building. All other
	 * flows always set it.
	 */
	buildingId?: number;
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
