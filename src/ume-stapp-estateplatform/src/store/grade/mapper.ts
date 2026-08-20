import { IGrade } from '@/models/grade/Interfaces';

export default {
	mapResponseToGrades: (responseData: IGrade[]): IGrade[] => {
		const grades: IGrade[] = responseData.map((i) => ({
			documentId: i.documentId,
			documentName: i.documentName,
			schoolName: i.schoolName,
		}));

		return grades;
	},
};
