import { ConsentStatus, UserConsentStatus } from '@/models/Enums';
import i18n from '@/plugins/i18next';

export const getFormattedDate = (dateString: string): string => {
	const date = new Date(dateString);
	return date.toDateString().split(' ').slice(1).join(' ');
};

export const getReviewSignalColor = (item: any): string => {
	if (item.review === 'True') {
		return '#eaffea';
	} else if (item.review === 'False') {
		return '#ffe6ea';
	} else {
		return 'none';
	}
};

export const getConsentStatusText = (consentStatus: ConsentStatus): string => {
	switch (consentStatus) {
		case ConsentStatus.Approved:
			return i18n.global.t('component.consentStart.table.approved');
		case ConsentStatus.Denied:
			return i18n.global.t('component.consentStart.table.denied');
		case ConsentStatus.Pending:
			return i18n.global.t('component.consentStart.table.pending');
		case ConsentStatus.New:
			return i18n.global.t('component.consentStart.table.new');
		default:
			return 'ERROR';
	}
};

export const getConsentUserStatusText = (
	userStatus: UserConsentStatus
): string => {
	switch (userStatus) {
		case UserConsentStatus.NotAnswered:
			return i18n.global.t('consent.userStatus.notAnswered');
		case UserConsentStatus.Approved:
			return i18n.global.t('consent.userStatus.approved');
		case UserConsentStatus.Rejected:
			return i18n.global.t('consent.userStatus.rejected');
		default:
			return 'ERROR';
	}
};
