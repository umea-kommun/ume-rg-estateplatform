import {
	IBuildingDocumentDto,
} from '@/models/estate/Dto';
import Config from '@/Config';
import {
	EstateType,
	ExternalOwnerStatus,
} from '@/models/estate/Enums';
import {
	IBuildingContact,
	IBuildingDetails,
	IBuildingDocument,
	IBuildingFloor,
	IBuildingGeoLocation,
	IBuildingRoom,
	IBusinessType,
	IEstateBuilding,
	IEstateDetails,
	IEstateSearchResultEntry,
} from '@/models/estate/Interfaces';
import ErrorService from '@/utils/ErrorService';

function capitalizeWords(str?: string | null) {
	if (str === null || str === undefined) {
		return null;
	}

	if (str.match(/^[a-zåäö]+\spå\s[a-zåäö]+$/i)) {
		// capitalize first letter only for strings like "Väst på stan"
		return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
	}

	return str
		.toLowerCase()
		.replace(
			/\p{L}+/gu,
			(word) => word.charAt(0).toUpperCase() + word.slice(1)
		);
}

function removeLeadingZerosRegex(str: string) {
	return str.replace(/^0+(?=\d)/, '');
}

function getBuildingImageUrl(buildingId: number): string {
	return `${Config.VUE_APP_ESTATE_SERVICE}/buildings/${buildingId}/image`;
}

function mapResponseToBuildingRoom(r: {
	id: number;
	name: string;
	popularName: string | null;
	grossArea: number | null;
	floorId: number;
	floorName: string;
	buildingId: number;
	isFavorite?: boolean;
}): IBuildingRoom {
	return {
		id: r.id,
		name: r.name,
		popularName: r.popularName,
		grossArea: Math.round(r.grossArea ?? 0),
		floorId: r.floorId,
		floorName: removeLeadingZerosRegex(r.floorName),
		buildingId: r.buildingId,
		isFavorite: r.isFavorite ?? false,
	};
}
function mapExternalOwnerInfo(
	r:
		| {
				status: string;
				name: string;
				note: string;
		  }
		| null
		| undefined,

	entityName?: string,
	entityId?: number
) {
	if (!r) {
		return null;
	}

	const status =
		r.status === ExternalOwnerStatus.Inhyrd
			? ExternalOwnerStatus.Inhyrd
			: r.status === ExternalOwnerStatus.Egen
			? ExternalOwnerStatus.Egen
			: null;

	if (!status) {
		// If unable to map external owner status,
		// log error but return null to avoid breaking the entire building/estate details page
		ErrorService.onError({
			err: new Error(
				`Unknown external owner status "${r.status}" for building/estate "${entityName}" (ID: ${entityId})`
			),
			hidden: true,
		});
	}

	return {
		status,
		name: r.name,
		note: r.note,
	};
}

export default {
	mapResponseToEstateSearchResult: (
		responseData: {
			id: number;
			type: string;
			name: string;
			popularName: string | null;
			numChildren: number | null;
			numFloors: number | null;
			grossArea: number | null;
			imageUrl: string | null;
			isFavorite: boolean;
			address: {
				street: string;
				zipCode: string;
				city: string;
				country: string;
			} | null;
			ancestors: {
				id: number;
				name: string;
				popularName: string | null;
				type: string;
			}[];
			_geo: {
				lat: number;
				lng: number;
			};
			extendedProperties: {
				municipalityArea: string | null;
				operationalArea: string | null;
			} | null;
		}[]
	): IEstateSearchResultEntry[] => {
		const entries: IEstateSearchResultEntry[] = responseData.map((item) => {
			const entry: IEstateSearchResultEntry = {
				id: item.id,
				type: item.type as EstateType,
				name: item.name,
				popularName: capitalizeWords(item.popularName),
				municipalityArea: capitalizeWords(
					item.extendedProperties?.municipalityArea
				),
				operationalArea: capitalizeWords(
					item.extendedProperties?.operationalArea
				),
				imageUrl: item.imageUrl ? getBuildingImageUrl(item.id) : null,
				isFavorite: item.isFavorite ?? false,
				address: item.address
					? {
							street: capitalizeWords(
								item.address.street
							) as string,
							zipCode: item.address.zipCode,
							city: capitalizeWords(item.address.city) as string,
					  }
					: null,
				metrics: {
					buildingCount:
						item.type === EstateType.Estate
							? item.numChildren
							: null,
					floorCount: item.numFloors,
					roomCount:
						item.type === EstateType.Building
							? item.numChildren
							: null,
					areaSqm:
						item.grossArea !== null
							? Math.round(item.grossArea)
							: null,
				},
				ancestors: item.ancestors.map((ancestor) => ({
					id: ancestor.id,
					name: ancestor.name,
					popularName: ancestor.popularName,
					type: ancestor.type as EstateType,
				})),
				geoLocation:
					item._geo?.lat && item._geo?.lng
						? {
								lat: item._geo.lat,
								lon: item._geo.lng,
						  }
						: null,
			};
			return entry;
		});

		return entries;
	},
	mapResponseToEstateSearchResultGeoLocations: (
		responseData: {
			id: number;
			geoLocation: {
				lat: number;
				lon: number;
			};
		}[]
	): IBuildingGeoLocation[] => {
		const entries: IBuildingGeoLocation[] = responseData.map((item) => {
			const entry: IBuildingGeoLocation = {
				id: item.id,
				geoLocation: {
					lat: item.geoLocation.lat,
					lon: item.geoLocation.lon,
				},
			};
			return entry;
		});

		return entries;
	},
	mapResponseToEstateDetails: (r: {
		id: number;
		type: string;
		name: string;
		popularName: string | null;
		address: string | null;
		grossArea: number | null;
		buildingCount: number | null;
		isFavorite?: boolean;
		extendedProperties: {
			municipalityArea: string | null;
			operationalArea: string | null;
			administrativeArea?: string | null;
			externalOwnerInfo: {
				status: string;
				name: string;
				note: string;
			} | null;
		} | null;
	}): IEstateDetails => {
		const estate: IEstateDetails = {
			id: r.id,
			type: r.type as EstateType,
			name: r.name,
			popularName: capitalizeWords(r.popularName),
			isFavorite: r.isFavorite ?? false,
			externalOwnerInfo: mapExternalOwnerInfo(
				r.extendedProperties?.externalOwnerInfo,
				r.name,
				r.id
			),
			municipalityArea: capitalizeWords(
				r.extendedProperties?.municipalityArea
			),
			operationalArea: capitalizeWords(
				r.extendedProperties?.operationalArea
			),
			administrativeArea: capitalizeWords(
				r.extendedProperties?.administrativeArea
			),
			metrics: {
				buildingCount: r.buildingCount,
				areaSqm: r.grossArea !== null ? Math.round(r.grossArea) : null,
			},
		};
		return estate;
	},
	mapResponseToEstateBuildings: (
		buildingsResponse: {
			id: number;
			name: string;
			popularName: string | null;
			grossArea: number | null;
			numFloors: number | null;
			numRooms: number | null;
			imageUrl: string | null;
			isFavorite?: boolean;
			address: {
				street: string;
				zipCode: string;
				city: string;
				country: string;
			} | null;
			geoLocation: {
				lat: number;
				lon: number;
			} | null;
		}[]
	): IEstateBuilding[] => {
		const buildings: IEstateBuilding[] = buildingsResponse.map((b) => ({
			id: b.id,
			name: b.name,
			popularName: capitalizeWords(b.popularName),
			grossArea: Math.round(b.grossArea ?? 0),
			imageUrl: b.imageUrl ? getBuildingImageUrl(b.id) : null,
			isFavorite: b.isFavorite ?? false,
			metrics: {
				floorCount: b.numFloors,
				roomCount: b.numRooms,
			},
			address: b.address
				? {
						street: b.address.street,
						zipCode: b.address.zipCode,
						city: b.address.city,
				  }
				: null,
			geoLocation: b.geoLocation
				? {
						lat: b.geoLocation.lat,
						lon: b.geoLocation.lon,
				  }
				: null,
		}));

		return buildings;
	},
	mapResponseToBuildingLocations: (
		responseData: {
			id: number;
			geoLocation: {
				lat: number;
				lon: number;
			};
		}[]
	): IBuildingGeoLocation[] => {
		const buildingLocations = responseData.map((r) => {
			const buildingLocation: IBuildingGeoLocation = {
				id: r.id,
				geoLocation: {
					lat: r.geoLocation.lat,
					lon: r.geoLocation.lon,
				},
			};
			return buildingLocation;
		});
		return buildingLocations;
	},
	mapResponseToBuildingDetails: (r: {
		id: number;
		type: string;
		name: string;
		popularName: string | null;
		address: { street: string; zipCode: string; city: string } | null;
		grossArea: number | null;
		numFloors: number | null;
		numRooms: number | null;
		numDocuments: number | null;
		estate: { id: number; name: string; popularName: string | null };
		region: { id: number; name: string };
		imageUrl: string | null;
		isFavorite?: boolean;
		extendedProperties?: {
			blueprintAvailable: boolean | null;
			noticeBoard: {
				text: string;
				startDate: string;
				endDate: string;
			} | null;
			yearOfConstruction: string | null;
			externalOwnerInfo?: {
				status: string;
				name: string;
				note: string;
			};
			contactPersons: {
				propertyManager: IBuildingContact | null;
				operationsManager: IBuildingContact | null;
				operationCoordinator: IBuildingContact | null;
				rentalAdministrator: IBuildingContact | null;
			} | null;
		};
		workOrderTypes?: string[];
		geoLocation: {
			lat: number;
			lon: number;
		} | null;
	}): IBuildingDetails => {
		const rContactPersons = r.extendedProperties?.contactPersons;

		const building: IBuildingDetails = {
			id: r.id,
			type: r.type as EstateType,
			name: r.name,
			popularName: capitalizeWords(r.popularName),
			numDocuments: r.numDocuments,
			blueprintAvailable:
				r.extendedProperties?.blueprintAvailable ?? false,
			imageUrl: r.imageUrl ? getBuildingImageUrl(r.id) : null,
			isFavorite: r.isFavorite ?? false,
			workOrderTypes: r.workOrderTypes ?? [],
			externalOwnerInfo: mapExternalOwnerInfo(
				r.extendedProperties?.externalOwnerInfo,
				r.name,
				r.id
			),
			address: r.address
				? {
						street: r.address.street,
						zipCode: r.address.zipCode,
						city: r.address.city,
				  }
				: null,
			metrics: {
				yearOfConstruction:
					r.extendedProperties?.yearOfConstruction ?? null,
				floorCount: r.numFloors,
				roomCount: r.numRooms,
				areaSqm: r.grossArea !== null ? Math.round(r.grossArea) : null,
			},
			geoLocation: r.geoLocation
				? {
						lat: r.geoLocation.lat,
						lon: r.geoLocation.lon,
				  }
				: null,
			estate: {
				id: r.estate.id,
				name: r.estate.name,
				popularName: capitalizeWords(r.estate.popularName),
			},
			region: {
				id: r.region.id,
				name: r.region.name,
			},
			noticeBoard: r.extendedProperties?.noticeBoard
				? {
						text: r.extendedProperties.noticeBoard.text,
						startDate: r.extendedProperties.noticeBoard.startDate,
						endDate: r.extendedProperties.noticeBoard.endDate,
				  }
				: null,
			contactPersons: rContactPersons
				? {
						propertyManager: rContactPersons.propertyManager ?? null,
						operationsManager:
							rContactPersons.operationsManager ?? null,
						operationCoordinator:
							rContactPersons.operationCoordinator ?? null,
						rentalAdministrator:
							rContactPersons.rentalAdministrator ?? null,
				  }
				: null,
		};
		return building;
	},
	mapResponseToBuildingRoom,
	mapResponseToBuildingRooms: (
		roomsResponse: {
			id: number;
			name: string;
			popularName: string | null;
			grossArea: number | null;
			floorId: number;
			floorName: string;
			buildingId: number;
		}[]
	): IBuildingRoom[] => {
		return roomsResponse.map(mapResponseToBuildingRoom);
	},
	mapResponseToBuildingFloors: (
		floorsResponse: {
			id: number;
			name: string;
			popularName: string | null;
		}[]
	): IBuildingFloor[] => {
		const floors: IBuildingFloor[] = floorsResponse.map((r) => ({
			id: r.id,
			name: removeLeadingZerosRegex(r.name),
			popularName: r.popularName,
		}));

		return floors;
	},
	mapResponseToBusinessTypes: (
		businessTypesResponse: {
			id: number;
			name: string;
		}[]
	): IBusinessType[] => {
		const businessTypes: IBusinessType[] = businessTypesResponse.map(
			(r) => ({
				id: r.id,
				name: r.name,
			})
		);

		return businessTypes;
	},
	mapResponseToBuildingDocuments: (
		r: IBuildingDocumentDto[]
	): IBuildingDocument[] =>
		r.map((doc) => ({
			id: doc.id,
			name: doc.name,
			directoryId: doc.directoryId,
			sizeInBytes: doc.sizeInBytes,
			categoryId: doc.categoryId,
			categoryName: doc.categoryName,
		})),
};
