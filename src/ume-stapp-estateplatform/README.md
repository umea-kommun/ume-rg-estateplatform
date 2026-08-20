# minasidor frontend (ume-stapp-minasidor)

## Purpose

This repository contains the frontend application for Mina sidor (my pages) at Umea kommun.

It is one part of a larger multi-repo platform and is responsible for:

- user-facing navigation and UI flows
- authentication and route access control
- integration with backend APIs for consent, kvittens, grade, estate, and password workflows
- presentation logic, state handling, localization, and telemetry/error reporting

This repo does not include backend service implementations.

## What the application does

The application serves both external users and internal staff:

- external users: start page, consent handling, kvittens, grades
- internal staff: internal dashboard and admin/consumer views for consent, kvittens, assigned/default passwords
- estate functionality: internal estate search/details/fault report/order routes, controlled by runtime feature flags

Authentication mode and available login methods are runtime-configured via environment variables.

## Solution role in the multi-repo

This frontend acts as:

- web client and orchestration layer for multiple backend domains
- access-control enforcement at UI route level
- translation and UX layer for domain workflows
- telemetry entry point (Application Insights)

Typical dependencies in other repos (not here) are API services for:

- Consent Bridge
- Archive/Grade
- Estate Service
- authentication/token services

## Tech stack

- Vue 3 + TypeScript
- Vite 6
- Vue Router 4
- Vuex 4 (+ persisted state in session storage)
- Vuetify 3
- Vitest + Vue Test Utils
- i18n localization (`src/locales`)
- Axios for HTTP
- Application Insights for telemetry

## High-level architecture

- entrypoint: `src/main.ts`
- root app shell: `src/App.vue` and `src/components/app/*`
- route definitions and guards: `src/router/index.ts`, `src/router/routes.ts`
- global and module state: `src/store/*`
- auth abstraction and middleware: `src/plugins/auth/*`
- integration utilities/config: `src/utils/*`, `src/Config.ts`

### Main feature areas

- external features: `src/components/external/*`
- internal features: `src/components/internal/*`
- shared/base components: `src/components/base/*`

### Routing and access model

Routes use `meta` flags for access checks:

- `requiresExternalLogin`
- `requiresInternalLogin`
- `requiresGroup`
- `requiresUnauthenticated`
- `requiresFeature` (runtime feature flags, currently used by estate routes)

Authentication middleware is initialized from `src/plugins/auth/index.ts` and enforces login type and group access.

## Runtime configuration

Environment variables use the `VUE_APP_` prefix and are loaded from:

- Vite env files (`.env*`)
- optional server-provided config at runtime (`window.vueAppServerConfig`)

Core configuration domains include:

- auth scopes and auth clients
- group IDs for role-based route access
- backend API base URLs
- telemetry configuration
- UI behavior values (timeouts, locale, etc.)

Do not commit sensitive values or production secrets.

## Local development

### Prerequisites

- Node.js (LTS recommended)
- npm

### Install dependencies

```bash
npm install
```

### Start development server

```bash
npm run serve
```

The app runs on port `8080` by default (see `vite.config.ts`).

### Build for production

```bash
npm run build
```

Build output is written to `wwwroot/`.

## Quality and validation

### Run unit tests

```bash
npm run test
```

### Run lint (auto-fix)

```bash
npm run lint
```

## Deployment notes

- static web assets are generated via Vite
- output folder: `wwwroot`
- Azure static web app settings are in `public/staticwebapp.config.json`

## Directory overview

```text
src/
	components/
		app/        # app shell, layout, startup/error states
		auth/       # login/callback/notifications
		base/       # reusable base UI components
		external/   # citizen-facing features
		internal/   # staff/internal features
	locales/      # translation resources
	models/       # enums/interfaces/dto contracts
	plugins/      # auth, i18n, telemetry, validation, vuetify
	router/       # route names and route config
	store/        # vuex root/modules/actions/mutations
	themes/       # SCSS theme and mixins
	utils/        # shared helper and config utilities
```

## Troubleshooting pointers

- login redirect loops: verify auth client config and route meta requirements
- missing internal routes: verify user auth client and group membership variables
- estate routes unavailable: verify feature endpoint and `requiresFeature` flags
- API failures: verify `VUE_APP_*` backend URLs for current environment

## Maintainer notes

When adding a new feature domain:

1. add route names and route records with correct access meta
2. implement feature UI in `components/external` or `components/internal`
3. add/update Vuex module if shared state is needed
4. add typed interfaces in `models`
5. add tests in nearest `__tests__` folder
6. update this README and AI instruction files (`AGENTS.md`, `CLAUDE.md`) when architecture or workflows change
