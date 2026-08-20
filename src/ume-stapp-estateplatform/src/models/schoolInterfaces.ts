export interface IFilterSchool {
	name: string;
	refId: string;
}

export interface IFilterClass {
	name: string;
	refId: string;
	schoolRefId: string;
}

export interface IFilterStudent {
	name: string;
	studentSsno: string;
}
