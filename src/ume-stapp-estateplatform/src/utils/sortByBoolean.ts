// Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/utils/sortByBoolean.ts @ 84b4a5dc
export const sortByBoolean = <T>(
	items: T[],
	getSortValue: (item: T) => boolean | undefined
): T[] => {
	return [...items].sort(
		(left, right) =>
			Number(getSortValue(right)) - Number(getSortValue(left))
	);
};
