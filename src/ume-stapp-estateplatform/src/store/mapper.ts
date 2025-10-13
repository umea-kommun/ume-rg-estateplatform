import { IFilterClass, IFilterSchool } from '@/models/schoolInterfaces';

export default {
	mapResponseToSchoolsAndGroups: (response: {
		schools: {
			id: string;
			name: string;
		}[];
		groups: {
			id: string;
			name: string;
			type?: string;
			schoolId: string;
		}[];
	}): {
		schools: IFilterSchool[];
		groups: IFilterClass[];
	} => {
		const schools = response.schools.map((responseSchool) => {
			const school: IFilterSchool = {
				refId: responseSchool.id,
				name: responseSchool.name,
			};
			return school;
		});
		const groups = response.groups.map((responseClass) => {
			const classGroup: IFilterClass = {
				refId: responseClass.id,
				name: responseClass.name,
				schoolRefId: responseClass.schoolId,
			};
			return classGroup;
		});
		return { schools, groups };
	},
};
