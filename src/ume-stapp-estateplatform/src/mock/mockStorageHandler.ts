import MockedDefaultData from '@/mock/data';

export const loadDataFromLocalStorage = (): typeof MockedDefaultData => {
	const dataString = window.localStorage.getItem('myPageMockData');
	return dataString ? JSON.parse(dataString) : null;
};

export const saveDataToLocalStorage = (
	data: typeof MockedDefaultData
): void => {
	window.localStorage.setItem('myPageMockData', JSON.stringify(data));
};

export const resetSavedMockData = (): void => {
	window.localStorage.setItem('myPageMockData', '');
};
