export interface IBuildingDocumentDto {
	id: number;
	name: string;
	directoryId: number;
	sizeInBytes: number;
	actionTypeId: number | null;
	actionTypeName: string | null;
}
export interface IBuildingDirectoryDto {
	id: number;
	name: string;
	subdirectories: IBuildingDirectoryDto[];
	documents: IBuildingDocumentDto[];
}

export interface IBuildingDocumentTreeDto {
	totalDocumentCount: number;
	totalDirectoryCount: number;
	directories: IBuildingDirectoryDto[];
	rootDocuments: IBuildingDocumentDto[];
}
