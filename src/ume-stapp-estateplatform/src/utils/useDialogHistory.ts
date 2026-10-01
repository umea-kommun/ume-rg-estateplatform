import { Ref, watch } from 'vue';
import { useEventListener } from '@vueuse/core';

const DIALOG_STATE_KEY = 'dialogHistory';

// Tags the pushed entry so sibling dialogs on the same page stay apart, and so
// an entry that outlives a page reload never matches a new instance.
const createEntryId = () =>
	`${Date.now()}-${Math.random().toString(36).slice(2)}`;

/**
 * Lets the browser back and forward buttons close and reopen a dialog. Opening
 * pushes a history entry on the current URL, leaving that entry closes the
 * dialog, returning to it reopens the dialog, and closing from the UI pops the
 * entry again.
 */
export const useDialogHistory = (open: Ref<boolean>) => {
	const entryId = createEntryId();
	const isOnEntry = () =>
		window.history.state?.[DIALOG_STATE_KEY] === entryId;

	// Whether the pushed entry is the current history entry.
	let onEntry = false;

	useEventListener(window, 'popstate', () => {
		const onEntryNow = isOnEntry();
		if (onEntryNow === onEntry) {
			return;
		}

		onEntry = onEntryNow;
		open.value = onEntryNow;
	});

	watch(open, (isOpen) => {
		if (isOpen === onEntry) {
			return;
		}

		onEntry = isOpen;
		if (isOpen) {
			window.history.pushState(
				{ ...window.history.state, [DIALOG_STATE_KEY]: entryId },
				''
			);
		} else if (isOnEntry()) {
			window.history.back();
		}
	});
};
