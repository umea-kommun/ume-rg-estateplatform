import { IBuildingFloor } from '@/models/Interfaces';

const floorNumber = (floor: IBuildingFloor): number | null => {
	const name = floor.name.trim();
	const parsed = Number(name);
	return name !== '' && Number.isFinite(parsed) ? parsed : null;
};

const numberedFloors = (floors: IBuildingFloor[]) =>
	floors
		.map((floor) => ({ floor, number: floorNumber(floor) }))
		.filter(
			(entry): entry is { floor: IBuildingFloor; number: number } =>
				entry.number !== null
		)
		.sort((a, b) => a.number - b.number);

/**
 * Picks the floor to select before the user has chosen one: floor 1, then the
 * lowest numbered floor, then whatever the API listed first.
 */
export const pickDefaultFloor = (
	floors: IBuildingFloor[]
): IBuildingFloor | null => {
	const numbered = numberedFloors(floors);
	const firstFloor = numbered.find((entry) => entry.number === 1);

	return firstFloor?.floor ?? numbered[0]?.floor ?? floors[0] ?? null;
};

/**
 * Orders floors top-down for a stacked selector: floors whose name is not a
 * number first in the order the API listed them, then numbered floors from
 * highest to lowest.
 */
export const stackFloors = (floors: IBuildingFloor[]): IBuildingFloor[] => {
	const unnumbered = floors.filter((floor) => floorNumber(floor) === null);
	const numbered = numberedFloors(floors)
		.reverse()
		.map((entry) => entry.floor);

	return [...unnumbered, ...numbered];
};
