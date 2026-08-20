export interface IPasswordGroupsAndSchools {
	schools: IPasswordSchool[];
	groups: IPasswordGroup[];
}

export interface IPasswordSchool {
	id: string;
	name: string;
}

export interface IPasswordGroup {
	id: string;
	name: string;
	type: string;
	schoolId: string;
}

export interface IPasswordDefaultAssignment {
	name: string;
	dateOfBirth: string;
	defaultPassword: string;
	email: string;
}
