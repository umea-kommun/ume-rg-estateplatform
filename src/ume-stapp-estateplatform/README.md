# Fastighetsportalen (ume-stapp-estateplatform)

## Purpose

Frontend for Umeå kommun's fastighetsportal — an internal-only Vue 3 + TypeScript
SPA for searching estates and buildings, viewing blueprints and maps, and
submitting fault reports, orders and changed space requirements.

It was extracted from `ume-rg-myplatform/src/ume-stapp-minasidor` (Mina sidor)
so UI and API changes can ship in one PR. Files carrying a
`Duplicated from ume-rg-myplatform` header are shared with Mina sidor and were
copied at commit `84b4a5dc`; that header is the diff base if the two copies ever
need reconciling.

## What it talks to

- **EstateService** (`VUE_APP_ESTATE_SERVICE`) — this repo, `src/ume-app-estateservice`.
  The only backend for estate data, work orders, favorites, feedback and the
  runtime `GET /features` flags and authenticated `GET /me` capabilities.
- **IDProxy** (`authtoken.umea.se`) — one login client, internal AD only.

## Running it

```bash
npm ci
npm run serve       # vite dev server on :8080, uses .env
```

`.env` points at the dev EstateService by default. To run against a local API,
change `VUE_APP_ESTATE_SERVICE` in `.env` — do not commit that; use `.env.local`.

```bash
npm run build       # vue-tsc --noEmit && vite build  -> wwwroot/
npm test            # vitest
npm run lint        # eslint --fix
```

## Layout

- `src/main.ts` — entry point
- `src/components/app/*` — shell (header, footer, content, error, 404)
- `src/components/estate/*` — the application itself
- `src/components/shared/*` — components duplicated from Mina sidor
- `src/router/index.ts` — routes, auth, feature, and capability gates
- `src/store/*` — root Vuex store (estate) plus the `feedback` module
- `src/plugins/auth/*` — auth manager, middleware and client config
- `src/utils/Config.ts` — runtime config, `VUE_APP_*` from `.env*`
- `src/utils/useCurrentUser.ts` — current identity and work-order capabilities

## Environment

Every `VUE_APP_*` in `.env` is read by the app. All four env files
(`.env`, `.env.dev`, `.env.test`, `.env.prod`) carry an identical key set —
keep it that way.

The auth client is configured through the `VUE_APP_AUTH_INTERNAL_*` prefix, and
`AuthConfig.ts` keys off that literal string to decide a client is internal.
Renaming the prefix without changing `AuthConfig.ts` silently produces an app
that cannot log anyone in.

## Feature flags

Routes are gated by `GET /features` on EstateService (`EstateService`,
`ErrorReport`), fetched at navigation time and fail-open. A gated route falls
back to `/`; if `/` itself is gated the 404 page is shown.

## User capabilities

Authenticated `GET /me` returns identity and the work-order types the current
user may access. Capability loading fails closed and controls which entry points
the SPA shows, including direct-route redirects. The API remains the enforcement
point: do not inspect AAD groups or duplicate `WorkOrder:RequiredGroupByType` in
the frontend.
