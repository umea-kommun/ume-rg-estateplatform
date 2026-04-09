export interface IBuildingDocumentDto {
	id: number;
	name: string;
	directoryId: number | null;
	sizeInBytes: number | null;
	categoryId: number | null;
	categoryName: string | null;
}
