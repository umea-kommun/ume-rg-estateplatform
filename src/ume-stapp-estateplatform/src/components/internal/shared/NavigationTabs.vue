<template>
	<div>
		<div>
			<v-tabs
				:model-value="activeTab"
				@update:model-value="() => {}"
				class="navigation-tabs"
				:class="{ hidden: !tabsFitScreen }"
				:disabled="!tabsFitScreen"
			>
				<v-tab
					v-for="tab in tabs"
					:key="tab.routeName + tab.label"
					:value="tab.routeName"
					:to="{ name: tab.routeName, params: route.params }"
					ref="tab-ref"
					:disabled="tab.disabled"
				>
					{{ tab.label }}
				</v-tab>
			</v-tabs>
			<!-- Fallback to menu if tabs doesn't fit screen -->
			<v-menu v-if="!tabsFitScreen">
				<template #activator="{ props }">
					<div class="mobile-nav">
						<div class="label" v-bind="props">
							{{
								tabs.find((tab) => tab.routeName === activeTab)
									?.label
							}}
						</div>
						<v-btn
							v-bind="props"
							variant="flat"
							size="large"
							appendIcon="keyboard_arrow_down"
						>
							{{ $t('app.nav.more') }}
						</v-btn>
					</div>
				</template>
				<v-list class="mobile-navigation-tabs pa-0">
					<v-list-item
						v-for="tab in tabs"
						:key="tab.routeName + tab.label + '-list'"
						:to="{ name: tab.routeName, params: route.params }"
						:active="activeTab === tab.routeName"
						:title="tab.label"
					/>
				</v-list>
			</v-menu>
		</div>
	</div>
</template>

<script setup lang="ts">
import { useWindowSize } from '@vueuse/core';
import { computed, onMounted, ref, useTemplateRef, watch } from 'vue';
import { useRoute } from 'vue-router';

defineProps<{
	tabs: { routeName: string; label: string; disabled?: boolean }[];
}>();

const route = useRoute();

const activeTab = computed(() => route.name?.toString() ?? '');

const tabsFitScreen = ref(true);
const tabRef = useTemplateRef('tab-ref');
const { width } = useWindowSize();

const calculateTabsWidth = () => {
	if (!tabRef.value || tabRef.value.length === 0) {
		return;
	}
	const tabParent = tabRef.value[0]?.$el.parentElement;
	const tabParentWrapper = tabParent?.parentElement;
	const tabParentWidth = tabParent?.clientWidth || 0;
	const tabParentWrapperWidth = tabParentWrapper?.clientWidth || 0;

	tabsFitScreen.value = tabParentWrapperWidth >= tabParentWidth;
};

watch(() => width.value, calculateTabsWidth);
onMounted(calculateTabsWidth);
</script>

<style lang="scss" scoped>
.navigation-tabs.v-tabs {
	--v-tabs-height: 62px;
	border-bottom: solid 1px $grey-lighten-4;

	&.hidden {
		opacity: 0;
		pointer-events: none;
		height: 0;
		overflow: hidden;
	}

	.v-tab {
		margin: 0;
		height: 100%;
		color: $grey-darken-1;
		text-transform: none;
		letter-spacing: normal;
		font-size: size(16);
		padding: 0;
		border-radius: 0;

		&:not(:last-child) {
			margin-right: 24px;
		}

		&--selected {
			font-weight: bold;
			color: $black;
			border-color: $primary;

			:deep(.v-tab__slider) {
				background-color: $primary;
			}
		}
	}
}

.mobile-nav {
	display: flex;
	align-items: center;
	border-bottom: solid 1px $grey-lighten-4;

	.v-btn {
		margin: 0 0 2px 8px;
	}
	.label {
		border-bottom: solid 2px $primary;
		font-weight: bold;
		padding: 17px 10px;
	}
}
</style>
