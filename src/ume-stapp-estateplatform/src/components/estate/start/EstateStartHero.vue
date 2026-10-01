<template>
	<section class="hero app-content-bleed">
		<div class="hero-map hero-map--placeholder" aria-hidden="true"></div>
		<building-map
			ref="hero-map"
			class="hero-map hero-map--live"
			:class="{ 'hero-map--ready': isMapReady }"
			:points="buildingPoints"
			hide-controls
		/>
		<div class="hero-scrim" aria-hidden="true"></div>

		<div class="hero-inner app-content-container">
			<div class="hero-content">
				<h1>{{ $t('component.estatePortal.title') }}</h1>
				<p>{{ $t('component.estatePortal.description') }}</p>

				<estate-start-search />

				<v-btn
					variant="outlined"
					color="white"
					prepend-icon="map"
					class="hero-map-btn mt-4"
					@click="openMap"
				>
					{{ $t('component.estateStart.openMap') }}
				</v-btn>
			</div>
		</div>
	</section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, useTemplateRef } from 'vue';
import BuildingMap from '../map/BuildingMap.vue';
import EstateStartSearch from './EstateStartSearch.vue';
import { useEstateSearch } from '../search/useEstateSearch';
import { appInsights } from '@/plugins/appInsights';

const { buildingPoints, fetchSearchResults: fetchMapLocations } =
	useEstateSearch(ref(''), undefined, { updateQueryParams: false });
onMounted(fetchMapLocations);

const isMapReady = computed(() => buildingPoints.value.length > 0);

const heroMapRef = useTemplateRef('hero-map');
const openMap = () => {
	heroMapRef.value?.open();
	appInsights?.trackEvent({
		name: 'EstateStartMapOpened',
		properties: {
			url: window.location.href,
		},
	});
};
</script>

<style lang="scss" scoped>
.hero {
	// Full-bleed: the backdrop below spans the whole viewport width while
	// .hero-inner keeps the reading content at the standard page width.
	position: relative;
	color: $white;

	// Non-interactive visual backdrop: never steals scroll or a stray click.
	.hero-map {
		position: absolute;
		inset: 0;
		z-index: 0;
		overflow: hidden;
		pointer-events: none;
	}

	.hero-map--placeholder {
		background: $estate-blueprint-background;
	}

	.hero-map--live {
		opacity: 0;
		transition: opacity 0.4s ease;

		&.hero-map--ready {
			opacity: 1;
		}
	}

	.hero-scrim {
		position: absolute;
		inset: 0;
		z-index: 1;
		border-radius: inherit;
		pointer-events: none;
		background: linear-gradient(
			100deg,
			rgba($primary, 0.94) 0%,
			rgba($primary, 0.8) 38%,
			rgba($primary, 0.6) 70%,
			rgba($primary, 0.48) 100%
		);
	}

	.hero-inner {
		position: relative;
		z-index: 2;
		display: flex;
		flex-direction: column;
		min-height: 350px;
		padding-block: 56px;
	}

	.hero-content {
		max-width: 620px;

		h1 {
			font-size: size(34);
			line-height: 1.15;
			margin: 0 0 8px;
		}

		p {
			font-size: size(18);
			line-height: 1.5;
			margin: 0;
			color: rgba(#fff, 0.92);
		}
	}

	.hero-map-btn {
		color: $white;
		text-transform: none;
	}
}

@media only screen and (max-width: $estate-mobile-threshold) {
	.hero {
		.hero-inner {
			min-height: 340px;
			padding-block: 40px;
		}

		.hero-scrim {
			background: linear-gradient(
				180deg,
				rgba($primary, 0.9) 0%,
				rgba($primary, 0.78) 100%
			);
		}

		.hero-content h1 {
			font-size: size(27);
		}
	}
}
</style>
