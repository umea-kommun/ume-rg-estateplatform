import {
	IPasswordDefaultAssignment,
	IPasswordGroup,
	IPasswordGroupsAndSchools,
	IPasswordSchool,
} from '@/models/password/Interfaces';

export default {
	mapResponseToPasswordGroupsAndSchools: (
		responseData: IPasswordGroupsAndSchools
	): IPasswordGroupsAndSchools => {
		const schools: IPasswordSchool[] = responseData.schools.map((i) => {
			const school: IPasswordSchool = {
				name: i.name,
				id: i.id,
			};
			return school;
		});

		const groups: IPasswordGroup[] = responseData.groups.map((i) => {
			const group: IPasswordGroup = {
				id: i.id,
				name: i.name,
				type: i.type,
				schoolId: i.schoolId,
			};
			return group;
		});

		const passwordGroupsAndSchools: IPasswordGroupsAndSchools = {
			schools: schools,
			groups: groups,
		};

		return passwordGroupsAndSchools;
	},

	mapResponseToDefaultPasswordAssignments: (
		responseData: IPasswordDefaultAssignment[]
	): IPasswordDefaultAssignment[] => {
		const assignedPasswords = responseData.map((i) => {
			const assignment: IPasswordDefaultAssignment = {
				name: i.name,
				dateOfBirth: i.dateOfBirth,
				defaultPassword: i.defaultPassword,
				email: i.email,
			};
			return assignment;
		});
		return assignedPasswords;
	},
};
