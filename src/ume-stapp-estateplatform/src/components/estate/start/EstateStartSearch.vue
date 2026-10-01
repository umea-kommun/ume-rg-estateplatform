<template>
	<div class="hero-search">
		<v-text-field
			v-model="search"
			:placeholder="$t('component.estateSearch.searchPlaceholder')"
			color="primary"
			prepend-inner-icon="search"
			clearable
			hide-details
			variant="solo"
			autocomplete="off"
			role="combobox"
			:aria-expanded="showMenu"
			:aria-controls="listboxId"
			:aria-activedescendant="activeDescendantId"
			aria-autocomplete="list"
			@focus="onFocus"
			@blur="onBlur"
			@keydown.down.prevent="onArrowDown"
			@keydown.up.prevent="onArrowUp"
			@keydown.enter="onEnterKey"
			@keydown.esc="onEscape"
		>
			<template #append-inner>
				<v-btn
					variant="flat"
					color="primary"
					class="ma-2 search-btn"
					@click="goToSearch"
				>
					{{ $t('component.estateSearch.searchButton') }}
				</v-btn>
			</template>
		</v-text-field>

		<!--
			A plain absolutely-positioned block rather than a teleported overlay.
			v-autocomplete's overlay could not be positioned reliably under the
			iOS Safari keyboard (it covered the input), and it reset the typed
			text on blur. Owning the list here fixes both: the field's v-model is
			ours so the text persists, and the list is anchored directly below the
			input (see .hero-search-menu) so it can't land on top of what the user
			is typing.
			mousedown.prevent keeps focus in the input so clicks register before
			the blur-driven close.
		-->
		<div
			v-if="showMenu"
			id="hero-search-listbox"
			class="hero-search-menu"
			:role="suggestions.length ? 'listbox' : undefined"
			@mousedown.prevent
		>
			<v-list>
				<template v-if="suggestions.length">
					<v-list-item
						v-for="(item, index) in suggestions"
						:id="`hero-suggestion-${index}`"
						:key="item.key"
						role="option"
						:aria-selected="index === activeIndex"
						:class="[
							'suggestion-item',
							`type-${item.type}`,
							{
								'suggestion-item--active':
									index === activeIndex,
							},
						]"
						:to="item.to"
						:prepend-icon="item.icon"
						:title="item.name"
						:subtitle="item.meta"
						@mouseenter="activeIndex = index"
					/>

					<v-divider />
					<v-list-item
						id="hero-suggestion-all"
						role="option"
						:aria-selected="activeIndex === showAllIndex"
						class="suggestion-all"
						:class="{
							'suggestion-all--active':
								activeIndex === showAllIndex,
						}"
						:to="searchAllRoute"
						prepend-icon="search"
						append-icon="arrow_forward"
						:title="
							$t('component.estateStart.showAllResultsFor', {
								query,
							})
						"
						@mouseenter="activeIndex = showAllIndex"
					/>
				</template>

				<div v-else class="suggestion-status" aria-live="polite">
					<template v-if="isSearching">
						<v-progress-circular
							indeterminate
							color="primary"
							:size="20"
							:width="2"
						/>
						<span>{{ $t('component.estateStart.searching') }}</span>
					</template>
					<template v-else>
						{{ $t('component.estateSearch.noResults') }}
					</template>
				</div>
			</v-list>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRouter, RouteLocationRaw } from 'vue-router';
import { watchDebounced } from '@vueuse/core';
import { useI18n } from 'vue-i18n';
import { useEstateSearch } from '../search/useEstateSearch';
import { EstateRoutes } from '@/router/routes';
import { EstateType } from '@/models/Enums';
import { IEstateSearchResultEntry } from '@/models/Interfaces';

const router = useRouter();
const { t } = useI18n();

const PREVIEW_LIMIT = 4;

const search = ref('');
const query = computed(() => search.value?.trim() ?? '');

// Inline preview only. The query params and map locations belong to the
// full search page, so opt out of both here.
const { fetchSearchResults, searchResults } = useEstateSearch(
	search,
	undefined,
	{
		updateQueryParams: false,
		getBuildingLocations: false,
	}
);

// --- Suggestion mapping ------------------------------------------------------

interface Suggestion {
	key: string;
	name: string;
	icon: string;
	meta: string;
	type: EstateType;
	to: RouteLocationRaw;
}

const typeIcon = (type: EstateType): string => {
	switch (type) {
		case EstateType.Estate:
			return 'home_work';
		case EstateType.Building:
			return 'apartment';
		case EstateType.Room:
			return 'meeting_room';
	}
	return 'place';
};

const navigationFor = (entry: IEstateSearchResultEntry): RouteLocationRaw => {
	switch (entry.type) {
		case EstateType.Estate:
			return {
				name: EstateRoutes.EstateDetails,
				params: { estateId: entry.id },
			};
		case EstateType.Building:
			return {
				name: EstateRoutes.BuildingDetails,
				params: { buildingId: entry.id },
			};
		case EstateType.Room:
			return {
				name: EstateRoutes.BuildingDetails,
				params: {
					buildingId: entry.ancestors?.find(
						(a) => a.type === EstateType.Building
					)?.id,
				},
				query: { roomId: entry.id },
			};
	}
	return { name: EstateRoutes.Search };
};

// A single muted line: the type, then the most useful locator for that type.
const metaFor = (entry: IEstateSearchResultEntry): string => {
	const typeLabel = t(`estateCommon.type.${entry.type}`);
	let detail: string | null | undefined;
	switch (entry.type) {
		case EstateType.Estate:
			detail = entry.name || entry.municipalityArea;
			break;
		case EstateType.Building:
			detail =
				entry.address?.street?.trim() ||
				entry.ancestors?.[0]?.popularName ||
				entry.ancestors?.[0]?.name;
			break;
		case EstateType.Room:
			detail =
				entry.ancestors?.[1]?.popularName || entry.ancestors?.[1]?.name;
			break;
	}
	return detail ? `${typeLabel} · ${detail}` : typeLabel;
};

const suggestions = computed<Suggestion[]>(() =>
	(searchResults.value ?? []).slice(0, PREVIEW_LIMIT).map((entry) => ({
		key: entry.type + entry.id,
		name: entry.popularName || entry.name,
		icon: typeIcon(entry.type),
		meta: metaFor(entry),
		type: entry.type,
		to: navigationFor(entry),
	}))
);

// --- Menu open/close state ---------------------------------------------------

const hasFocus = ref(false);
// Set by Escape so the menu stays closed until the query changes again.
const dismissed = ref(false);
// Index of the keyboard-highlighted suggestion, or -1 for "none" (Enter then
// means "search for what I typed" rather than "open this suggestion").
const activeIndex = ref(-1);

const showMenu = computed(
	() => hasFocus.value && !!query.value && !dismissed.value
);

const onFocus = () => {
	hasFocus.value = true;
	// Re-focusing after an Escape dismissal should surface the menu again.
	dismissed.value = false;
};
const onBlur = () => {
	hasFocus.value = false;
	// Drop the highlight so a later re-focus starts from "nothing selected".
	activeIndex.value = -1;
};

// --- Search fetching ---------------------------------------------------------

// Drives the dropdown's loading state. Flipped on the moment the user types so
// the debounce window before the fetch fires reads as "searching" rather than a
// premature "no results", and off only once the fetch settles.
const isSearching = ref(false);
watch(
	() => search.value,
	(value) => {
		isSearching.value = !!value?.trim();
		// A fresh query supersedes an Escape dismissal and any highlight.
		dismissed.value = false;
		activeIndex.value = -1;
	}
);
watchDebounced(
	() => search.value,
	async () => {
		await fetchSearchResults();
		isSearching.value = false;
	},
	{ debounce: 200, maxWait: 400 }
);

// --- Actions -----------------------------------------------------------------

// The full search page. Bound as the show-all row's :to (so it's a real link -
// middle/cmd-click opens a new tab) and reused by the search button and the
// keyboard "search for what I typed" path.
const searchAllRoute = computed<RouteLocationRaw>(() => ({
	name: EstateRoutes.Search,
	query: query.value ? { search: query.value } : undefined,
}));

const goToSearch = () => {
	router.push(searchAllRoute.value);
};

// Keyboard-only: the suggestion rows are :to links, so a mouse click (and
// modifier-clicks for new tabs) navigate natively. Enter keeps focus in the
// input, so it routes here instead.
const onSelect = (item: Suggestion) => {
	if (item?.to) {
		router.push(item.to);
	}
};

// --- Keyboard navigation -----------------------------------------------------

// The "show all results" footer is a navigable target too, sitting one past
// the last suggestion (index === suggestions.length), so arrowing down reaches
// it before wrapping back to the top.
const showAllIndex = computed(() => suggestions.value.length);

// Wires the input to the open listbox and its highlighted option for screen
// readers (the aria-* attributes on the field above).
const listboxId = computed(() =>
	showMenu.value && suggestions.value.length
		? 'hero-search-listbox'
		: undefined
);
const activeDescendantId = computed(() => {
	if (activeIndex.value < 0) {
		return undefined;
	}
	return activeIndex.value === showAllIndex.value
		? 'hero-suggestion-all'
		: `hero-suggestion-${activeIndex.value}`;
});

const onArrowDown = () => {
	if (!query.value) {
		return;
	}
	dismissed.value = false;
	if (!suggestions.value.length) {
		return;
	}
	activeIndex.value =
		activeIndex.value < showAllIndex.value ? activeIndex.value + 1 : 0;
};

const onArrowUp = () => {
	if (!query.value) {
		return;
	}
	dismissed.value = false;
	if (!suggestions.value.length) {
		return;
	}
	// From "none" (-1) or the top, wrap to the show-all footer.
	activeIndex.value =
		activeIndex.value > 0 ? activeIndex.value - 1 : showAllIndex.value;
};

// Enter on a highlighted suggestion opens it; on the show-all footer (or with
// nothing highlighted) it means "search for what I typed".
const onEnterKey = () => {
	const active = suggestions.value[activeIndex.value];
	if (active) {
		onSelect(active);
		return;
	}
	goToSearch();
};

const onEscape = () => {
	dismissed.value = true;
	activeIndex.value = -1;
};
</script>

<style lang="scss" scoped>
.hero-search {
	position: relative;
	margin-top: 28px;

	:deep(.v-field) {
		box-shadow: 0 6px 20px -6px rgba(0, 0, 0, 0.35);
		cursor: text;
	}

	:deep(input) {
		cursor: text;
	}

	:deep(.v-field--appended) {
		padding-right: 0;
	}
}

// Floating (overlays the hero content below) on every width. It is a plain
// absolutely-positioned child of .hero-search, not a teleported overlay, so it
// is always laid out directly below the input: it can't reflow the hero and
// can't land on top of the input the way v-autocomplete's overlay did.
.hero-search-menu {
	position: absolute;
	top: 100%;
	left: 0;
	right: 0;
	z-index: 20;
	margin-top: 8px;
	border-radius: $border-radius;
	overflow: hidden;
	background: rgb(var(--v-theme-surface));
	box-shadow: 0 12px 32px -8px rgba(0, 0, 0, 0.3);

	// No bottom padding: the "show all" footer sits flush to the rounded
	// bottom edge instead of leaving a white strip under it.
	:deep(.v-list) {
		padding: 6px 0 0;
		max-height: 420px;
		overflow-y: auto;
	}

	:deep(.suggestion-item),
	:deep(.suggestion-all) {
		.v-list-item__spacer {
			width: 12px;
		}
	}

	:deep(.suggestion-item) {
		// Roomier rows than the default compact list item.
		padding-block: 11px;
		cursor: pointer;

		.v-list-item__prepend .v-icon {
			color: $grey-darken-2;
			opacity: 1;
		}

		&.type-building .v-list-item__prepend .v-icon {
			color: $primary;
		}

		// The default title line-height (24px) leaves more empty space above
		// the text than below it; tightening it evens out the row.
		.v-list-item-title {
			font-weight: 600;
			font-size: size(16);
			line-height: 1.25;
			text-transform: capitalize;
		}

		.v-list-item-subtitle {
			font-size: size(14);
			line-height: 1.3;
			color: $grey-darken-1;
			opacity: 1;
		}
	}

	// Keyboard/pointer highlight - the overlay used to give us this for free.
	:deep(.suggestion-item--active) {
		background: rgba($primary, 0.14);
	}

	:deep(.suggestion-status) {
		display: flex;
		align-items: center;
		justify-content: flex-start;
		gap: 10px;
		min-height: 56px;
		padding: 12px 16px;
		color: $grey-darken-2;
		font-size: size(15);
	}

	:deep(.suggestion-all) {
		padding-block: 16px;
		border-top: solid 1px $grey-lighten-3;
		background: rgba($primary, 0.06);
		color: $primary;
		cursor: pointer;

		&.suggestion-all--active {
			background: rgba($primary, 0.16);
		}

		.v-list-item-title {
			font-weight: 600;
		}

		.v-icon {
			color: $primary;
			opacity: 1;
		}
	}
}
</style>
