import { describe, expect, test } from 'vitest';
import { pickDefaultFloor, stackFloors } from '../defaultFloor';
import { IBuildingFloor } from '@/models/Interfaces';

const floors = (...names: string[]): IBuildingFloor[] =>
	names.map((name, index) => ({
		id: index + 1,
		name,
		popularName: null,
	}));

describe('pickDefaultFloor', () => {
	test('prefers floor 1 however the api ordered the floors', () => {
		expect(pickDefaultFloor(floors('3', '1', '2'))?.name).toBe('1');
	});

	test('falls back to the lowest floor, comparing names as numbers', () => {
		expect(pickDefaultFloor(floors('10', '2', '7'))?.name).toBe('2');
	});

	test('takes a basement level below the numbered floors', () => {
		expect(pickDefaultFloor(floors('3', '-1', '2'))?.name).toBe('-1');
	});

	test('ignores names that are not numbers when one floor is numbered', () => {
		expect(pickDefaultFloor(floors('BV', '2'))?.name).toBe('2');
	});

	test('falls back to the first floor when no name is a number', () => {
		expect(pickDefaultFloor(floors('BV', 'Vind'))?.name).toBe('BV');
	});

	test('returns null for a building without floors', () => {
		expect(pickDefaultFloor([])).toBeNull();
	});
});

const names = (stacked: IBuildingFloor[]) => stacked.map((floor) => floor.name);

describe('stackFloors', () => {
	test('puts the lowest floor at the bottom, comparing names as numbers', () => {
		expect(names(stackFloors(floors('1', '10', '-1', '2', '0')))).toEqual([
			'10',
			'2',
			'1',
			'0',
			'-1',
		]);
	});

	test('keeps floors that are not numbers on top in api order', () => {
		expect(names(stackFloors(floors('2', 'Vind', '1', 'BV')))).toEqual([
			'Vind',
			'BV',
			'2',
			'1',
		]);
	});
});
