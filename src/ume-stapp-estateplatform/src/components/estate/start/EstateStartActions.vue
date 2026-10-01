<template>
	<section v-if="visibleActions.length" class="actions">
		<v-card
			v-for="action in visibleActions"
			:key="action.route"
			class="action-card"
			:to="{ name: action.route }"
			@click="trackAction(action.key)"
		>
			<div class="action-icon" :class="action.key">
				<v-icon :icon="action.icon" :size="28" />
			</div>
			<div class="action-text">
				<h2>{{ action.title }}</h2>
				<p>{{ action.description }}</p>
			</div>
			<v-icon class="action-arrow" icon="arrow_forward" />
		</v-card>
	</section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { EstateRoutes } from '@/router/routes';
import { EstateOrderCategory } from '@/models/Enums';
import { useFeatureFlags } from '@/utils/useFeatureFlags';
import { useCurrentUser } from '@/utils/useCurrentUser';
import { appInsights } from '@/plugins/appInsights';

const { t } = useI18n();

const { isEnabled } = useFeatureFlags();
const { canCreateWorkOrderType } = useCurrentUser();
const isErrorReportEnabled = computed(() => isEnabled('ErrorReport'));
const isSpaceRequirementAllowed = computed(() =>
	canCreateWorkOrderType(EstateOrderCategory.SpaceRequirement)
);

interface StartAction {
	key: 'faultReport' | 'order' | 'spaceRequirement';
	route: EstateRoutes;
	icon: string;
	title: string;
	description: string;
}

const visibleActions = computed<StartAction[]>(() => {
	if (!isErrorReportEnabled.value) return [];
	const actions: StartAction[] = [
		{
			key: 'faultReport',
			route: EstateRoutes.FaultReport,
			icon: 'warning',
			title: t('component.estatePortal.actions.faultReport.title'),
			description: t(
				'component.estatePortal.actions.faultReport.description'
			),
		},
		{
			key: 'order',
			route: EstateRoutes.Order,
			icon: 'handyman',
			title: t('component.estatePortal.actions.order.title'),
			description: t('component.estatePortal.actions.order.description'),
		},
	];
	if (isSpaceRequirementAllowed.value) {
		actions.push({
			key: 'spaceRequirement',
			route: EstateRoutes.SpaceRequirement,
			icon: 'space_dashboard',
			title: t('component.estatePortal.actions.spaceRequirement.title'),
			description: t(
				'component.estatePortal.actions.spaceRequirement.description'
			),
		});
	}
	return actions;
});

const trackAction = (target: string) => {
	appInsights?.trackEvent({
		name: 'EstateStartActionClicked',
		properties: {
			target,
			url: window.location.href,
		},
	});
};
</script>

<style lang="scss" scoped>
.actions {
	display: flex;
	gap: 16px;
	margin-top: 32px;

	.action-card {
		flex: 1 1 0;
		min-width: 0;
		display: flex;
		align-items: flex-start;
		gap: 16px;
		padding: 24px;

		.action-icon {
			flex-shrink: 0;
			display: flex;
			align-items: center;
			justify-content: center;
			width: 52px;
			height: 52px;
			border-radius: 14px;
			background: $primary-bg;

			.v-icon {
				color: $primary;
			}

			&.faultReport {
				background: $warning-bg;

				.v-icon {
					color: $grey-darken-3;
				}
			}

			&.order {
				background: $accent-bg;

				.v-icon {
					color: $grey-darken-3;
				}
			}

			&.spaceRequirement {
				background: $success-bg;

				.v-icon {
					color: $grey-darken-3;
				}
			}
		}

		.action-text {
			flex: 1;

			h2 {
				font-size: size(18);
				color: $black;
				margin: 0 0 4px;
			}

			p {
				font-size: size(15);
				line-height: 1.45;
				color: $grey-darken-2;
				margin: 0;
			}
		}

		.action-arrow {
			color: $grey-lighten-5;
			transition:
				color 0.2s ease,
				transform 0.2s ease;
		}

		&:hover {
			.action-arrow {
				color: $primary;
				transform: translateX(2px);
			}
		}
	}
}

@media only screen and (max-width: $estate-mobile-threshold) {
	.actions {
		flex-direction: column;

		.action-card {
			flex: none;
		}
	}
}
</style>
