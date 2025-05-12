/* eslint-disable @typescript-eslint/no-explicit-any */
import { ComponentPublicInstance } from 'vue';
import store from '../store/store';
import { appInsights } from '@/plugins/appInsights';
import Config from './Config';
import i18n from '@/plugins/i18next';
import { MutationType } from '@/models/Enums';
import { IErrorPage, IErrorToDisplay } from '@/models/Interfaces';
const { t } = i18n.global;

export interface ComposedError {
	message: string;
	stack?: string;
	location: string;
	vueComponentName?: string;
	vueInfo?: string;
	isAuthenticated: boolean;
	idp?: string;
}

interface OnError {
	err: any;
	log?: boolean;
	hidden?: boolean;
	instance?: ComponentPublicInstance | null;
	info?: string | null;
	message?: string | null;
	errorPage?: IErrorPage | null;
}

const isMocked = (Config.VUE_APP_MOCK_DATA || '').trim() === 'yes';
export default class ErrorService {
	/**
	 * Called when an unhandled error occurs or called manually using ErrorService.onError
	 */
	static onError({
		err,
		log = true,
		hidden = false,
		instance,
		info,
		message,
		errorPage,
	}: OnError): void | boolean {
		console.error('Error:', err);

		const generatedUserMessage = this.getUserMessage(err);

		let userMessage = message;
		if (!userMessage) {
			userMessage = generatedUserMessage.title;
		}

		if (errorPage?.visible) {
			if (!errorPage.title && !errorPage.titleKey) {
				// No title or titleKey provided, set title to default
				errorPage.title = generatedUserMessage.title;
			}
			if (!errorPage.message && !errorPage.messageKey) {
				errorPage.message = generatedUserMessage.message;
			}
		}

		// Update the state to display our error (if no full screen error is already shown)
		if (!store.state.error?.errorPage?.visible && !hidden) {
			store.commit(MutationType.SetError, {
				error: err,
				userMessage,
				errorPage,
			});
		}

		if (!isMocked && log) {
			// Compose and send error to Insights
			const error = this.composeError({ err, instance, info });
			this.sendError(error);

			// Send error directly to app insights
			if (appInsights) {
				appInsights.trackException({ error: err }, error);
			}
		}
	}

	/**
	 * Try to get the name of the component that threw the error
	 */
	private static getComponentName(instance: any): string | null {
		if (!instance) {
			return null;
		}
		if (instance.$parent === null) {
			return 'App';
		}
		if (instance?.$options.name) {
			return instance.$options.name;
		}
		return 'Anonymous';
	}

	/**
	 * Returns an object that contains information we want to send to the logger
	 */
	private static composeError({ err, instance, info }: any): ComposedError {
		const error: ComposedError = {
			message: err?.message ?? err,
			location: window.location.href,
			isAuthenticated: false,
		};
		if (err.stack) {
			error.stack = err.stack;
		}
		if (store?.state?.user?.isAuthenticated) {
			error.isAuthenticated = true;
			error.idp = store.state.user.idp;
		}

		const componentName = this.getComponentName(instance);
		if (componentName) {
			error.vueComponentName = componentName;
		}
		if (info) {
			error.vueInfo = info;
		}

		return error;
	}

	private static sendError(error: any): void {
		store.dispatch('sendError', { error });
	}

	/**
	 * Returns what message to display to the user depending on error type
	 */
	private static getUserMessage(err: any): IErrorToDisplay {
		const errorToDisplay: IErrorToDisplay = {
			title: t('app.error.general'),
		};

		const response = err.response;
		if (err.code === 'ERR_NETWORK') {
			errorToDisplay.title = t('app.error.network');
		} else if (response?.status) {
			switch (response.status) {
				case 400:
					if (
						response.data?.errorCode ==
						'Invalid user socialSecurityNumber'
					) {
						errorToDisplay.title = t(
							'app.error.400.invalidUserSSN.title'
						);
						errorToDisplay.message = t(
							'app.error.400.invalidUserSSN.message'
						);
					}
					break;
				// eslint-disable-next-line no-fallthrough
				case 401:
					errorToDisplay.title = t('app.error.401');
					break;
				case 403:
					errorToDisplay.title = t('app.error.403');
					break;
				case 404:
					errorToDisplay.title = t('app.error.404');
					break;
				case 413:
					errorToDisplay.title = t('app.error.tooLargeTotalFileSize');
					break;
			}
		}
		return errorToDisplay;
	}
}
