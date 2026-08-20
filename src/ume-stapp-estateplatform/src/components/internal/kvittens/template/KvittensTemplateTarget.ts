import { IKvittensTemplate } from '@/models/kvittens/Interfaces';
import { useI18n } from 'vue-i18n';

export const useKvittensTemplateTarget = () => {
	const { t } = useI18n();

	// [1,2,3,5,6,7] => ["1-3", "5-7"], non-numeric values (e.g. "F") are kept as-is
	const formatYearRanges = (years: string[]): string => {
		const nonNumeric = years.filter((y) => isNaN(Number(y))).sort();
		const numeric = years
			.filter((y) => !isNaN(Number(y)))
			.map(Number)
			.sort((a, b) => a - b);

		const runs: number[][] = [];
		for (const year of numeric) {
			const currentRun = runs[runs.length - 1];
			if (currentRun && year === currentRun[currentRun.length - 1] + 1) {
				currentRun.push(year);
			} else {
				runs.push([year]);
			}
		}

		const formattedNumeric = runs.map((run) =>
			run.length >= 3
				? `${run[0]}-${run[run.length - 1]}`
				: run.join(', ')
		);

		return [...nonNumeric, ...formattedNumeric].join(', ');
	};

	const addDisplayTargetsToTemplate = (template: IKvittensTemplate) => {
		const groupedTargets = template.targets.reduce(
			(acc, target) => {
				const form = target.schoolForm;
				(acc[form] ??= []).push(target.schoolYear);
				return acc;
			},
			{} as Record<string, string[]>
		);
		const displayTargets = Object.entries(groupedTargets).map(
			([schoolForm, schoolYears]) => ({
				schoolForm,
				schoolFormLabel: t('schoolForm.' + schoolForm),
				schoolYears,
				schoolYearsLabel: formatYearRanges(schoolYears),
			})
		);
		return {
			...template,
			displayTargets,
		};
	};

	return { addDisplayTargetsToTemplate };
};
