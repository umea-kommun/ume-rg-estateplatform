<template>
	<app-content
		:size="contentSize"
		:isLoading="isBusyLoadingFromServer"
		class="consent-consumer-list"
		:pageTitle="$t('component.internal.consentConsumerList.title')"
	>
		<consumer-tester />
		<base-back-button />

		<div class="filter">
			<div class="d-flex"></div>
		</div>
		<div v-if="!isBusyLoadingFromServer">
			<v-row>
				<v-col class="pa-0">
					<h1 class="my-3">
						{{ $t('component.internal.consentConsumerList.title') }}
					</h1>
				</v-col>
			</v-row>
			<consent-consumer-group-filter
				:schools="schools"
				:groups="groups"
				v-model:selectedSchoolId="selectedSchoolId"
				v-model:selectedGroupId="selectedGroupId"
				v-model:searchValue="searchValue"
			/>
			<v-alert
				v-if="!consumerTemplates.length"
				icon="warning"
				class="mt-4"
			>
				{{ $t('component.internal.consentConsumerList.noResults') }}
			</v-alert>
			<v-alert
				v-else-if="!filteredTemplates.length"
				icon="warning"
				class="mt-4"
			>
				{{
					$t('component.internal.consentConsumerList.noFilterResults')
				}}
			</v-alert>
			<!-- Display list of templates-->
			<consent-consumer-list-table
				v-else
				:filtered-templates="filteredTemplates"
				:selected-group-id="selectedGroupId"
				@select-group-to-open="
					(templateId) => (templateIdToOpen = templateId)
				"
			/>
		</div>
		<consumer-open-template-dialog
			:templateIdToOpen="templateIdToOpen"
			@update:templateIdToOpen="
				(templateId) => (templateIdToOpen = templateId)
			"
			:schools="schools"
			:groups="groups"
			:filter-selected-school-id="selectedSchoolId ?? undefined"
		/>
	</app-content>
</template>
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useStore } from 'vuex';
import AppContent from '@/components/app/AppContent.vue';
import BaseBackButton from '@/components/base/BaseBackButton.vue';
import { useRoute } from 'vue-router';
import {
	IConsentConsumerTemplateGroup,
	IConsumerGroup,
	IFilteredConsumerTemplate,
	IRootState,
} from '@/models/Interfaces';
import {
	AppContentSize,
	DispatchType,
	TemplateConnectionType,
} from '@/models/Enums';
import { Helper } from '@/utils/helper';
import ConsumerTester from '@/components/internal/shared/ConsumerTester.vue';
import ConsentConsumerListTable from './ConsentConsumerListTable.vue';
import ConsumerOpenTemplateDialog from './ConsumerOpenTemplateDialog.vue';
import ConsentConsumerGroupFilter from './ConsentConsumerGroupFilter.vue';

const store = useStore<IRootState>();
const route = useRoute();
const isBusyLoadingFromServer = ref<boolean>(true);

const contentSize = ref<AppContentSize>(
	route.meta
		? (route.meta.contentSize as AppContentSize)
		: AppContentSize.Default
);

const selectedSchoolId = ref<string | null>(null);
const selectedGroupId = ref<string | null>(null);
const searchValue = ref('');

const templateIdToOpen = ref<string | null>(null);

const consumerTemplates = computed(() => store.state.consumer.templates ?? []);

const schools = computed(() => {
	const schools =
		store.state.consumer.groups?.filter(
			(group) => group.type === TemplateConnectionType.Unit
		) ?? [];
	return schools;
});

const typeOrder = {
	[TemplateConnectionType.Department]: 1,
	[TemplateConnectionType.Class]: 2,
	[TemplateConnectionType.EducationGroup]: 3,
	[TemplateConnectionType.Unit]: 4,
	[TemplateConnectionType.Skolform]: 5,
};

const groups = computed(() => {
	const groups = Helper.deepCopy(store.state.consumer.groups ?? []);
	return groups.sort((a, b) => {
		// Sort by the custom type order
		if (typeOrder[a.type] !== typeOrder[b.type]) {
			return typeOrder[a.type] - typeOrder[b.type];
		}
		// If types are the same, sort alphabetically by name
		return a.name.localeCompare(b.name);
	});
});

/**
 * Convert the groups (school forms/schools/classes) the template is assigned to to the classes the user has access to
 * @returns A list of classes (not schools or school forms)
 */
const getTemplateGroups = (
	templateGroups: IConsumerGroup[],
	consumerGroups: IConsumerGroup[]
): IConsentConsumerTemplateGroup[] => {
	const classes: IConsentConsumerTemplateGroup[] = [];
	templateGroups.forEach((group) => {
		const foundGroup = consumerGroups.find((g) => g.refId === group.refId);
		if (foundGroup) {
			switch (foundGroup.type) {
				// Template is assigned to a class
				case TemplateConnectionType.Department:
				case TemplateConnectionType.Class:
					classes.push({
						refId: group.refId,
						title: group.name,
						type: group.type,
						parentIds: group.parents?.map((p) => p.id) ?? [],
					} as IConsentConsumerTemplateGroup);
					break;

				// Template is assigned to a unit
				case TemplateConnectionType.Unit: {
					// Find all classes in school
					const classGroups = consumerGroups.filter(
						(g) => g.parents?.find((p) => p.id === foundGroup.refId)
					);
					classGroups.forEach((element) => {
						classes.push({
							refId: element.refId,
							title: element.name,
							type: element.type,
							parentIds: element.parents?.map((p) => p.id) ?? [],
						} as IConsentConsumerTemplateGroup);
					});

					break;
				}

				// Template is assigned to a school form
				case TemplateConnectionType.Skolform: {
					// Find all schools in school form
					const unitGroups = consumerGroups.filter(
						(g) => g.parents?.find((p) => p.id === foundGroup.refId)
					);
					unitGroups.forEach((element) => {
						// Find all classes in school
						const classGroups = consumerGroups.filter(
							(g) =>
								g.parents?.find((p) => p.id === element.refId)
						);
						classGroups.forEach((classGroup) => {
							classes.push({
								refId: classGroup.refId,
								title: classGroup.name,
								type: classGroup.type,
								parentIds:
									classGroup.parents?.map((p) => p.id) ?? [],
							});
						});
					});
					break;
				}
			}
		}
	});

	const uniqueGroups = classes.filter(
		(value, index, self) =>
			index === self.findIndex((t) => t.refId === value.refId)
	);
	return uniqueGroups;
};

const templatesWithGroups = computed(() => {
	return consumerTemplates.value.map((consumerTemplate) => {
		const templateGroups = getTemplateGroups(
			consumerTemplate.groups,
			groups.value
		).sort((a, b) => {
			if (a.refId === selectedGroupId.value) {
				return -1;
			} else if (b.refId === selectedGroupId.value) {
				return 1;
			}
			return a.title.localeCompare(b.title);
		});

		return {
			guid: consumerTemplate.guid,
			title: consumerTemplate.title,
			groups: templateGroups,
			period: {
				start: consumerTemplate.publishedDate,
				end: consumerTemplate.expireDate,
			},
		};
	});
});

const filteredTemplates = computed<IFilteredConsumerTemplate[]>(() => {
	let items: IFilteredConsumerTemplate[] = templatesWithGroups.value;

	if (selectedGroupId.value) {
		items = items.filter((template) => {
			let templateHasGroup = false;
			template.groups.forEach((templateGroup) => {
				if (
					selectedGroupId.value === templateGroup.refId ||
					(templateGroup.parentIds?.length &&
						templateGroup.parentIds.includes(
							selectedGroupId.value ?? ''
						))
				) {
					templateHasGroup = true;
				}
			});
			return templateHasGroup;
		});
	} else if (selectedSchoolId.value) {
		items = items.filter((template) => {
			let templateHasGroup = false;
			template.groups.forEach((templateGroup) => {
				if (
					selectedSchoolId.value === templateGroup.refId ||
					(templateGroup.parentIds?.length &&
						templateGroup.parentIds.includes(
							selectedSchoolId.value ?? ''
						))
				) {
					templateHasGroup = true;
				}
			});
			return templateHasGroup;
		});
	}
	if (selectedSchoolId.value) {
		items = items.map((template) => {
			return {
				...template,
				groups: template.groups.filter((group) => {
					if (
						(group.type === TemplateConnectionType.Class ||
							group.type === TemplateConnectionType.Department ||
							group.type ===
								TemplateConnectionType.EducationGroup) &&
						!group.parentIds?.find(
							(p) => p === selectedSchoolId.value
						)
					) {
						return false;
					}
					return true;
				}),
			};
		});
	}

	if (searchValue.value) {
		items = items.filter((template) => {
			return (
				template.title
					.toLowerCase()
					.indexOf(searchValue.value.toLowerCase()) > -1
			);
		});
	}

	return items;
});

onMounted(async () => {
	isBusyLoadingFromServer.value = true;
	// Don't load again if it is already in the store.
	if (!groups.value.length || !consumerTemplates.value.length) {
		await store.dispatch(DispatchType.GetConsentConsumerList);
	}
	if (schools.value.length === 1) {
		selectedSchoolId.value = schools.value[0].refId;
	}
	isBusyLoadingFromServer.value = false;
});
</script>
<style scoped lang="scss">
.consent-consumer-list {
	.v-btn:not(.back-btn) {
		text-transform: none;
		letter-spacing: normal;
	}

	&.app-content {
		:deep(.v-container) {
			padding-top: calc($site-content-vertical-padding - 20px);
		}
	}
}
</style>
