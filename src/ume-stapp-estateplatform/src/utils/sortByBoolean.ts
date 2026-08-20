export const sortByBoolean = <T>(
	items: T[],
	getSortValue: (item: T) => boolean | undefined
): T[] => {
	return [...items].sort(
		(left, right) =>
			Number(getSortValue(right)) - Number(getSortValue(left))
	);
};
