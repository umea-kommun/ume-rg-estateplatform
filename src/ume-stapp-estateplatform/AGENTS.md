# AGENTS

## Repository summary

This repository is the Mina sidor frontend application for Umea kommun.
It is a Vue 3 + TypeScript single-page app that serves both external users and internal staff.

The frontend depends on runtime configuration and backend services from other repositories.

## Primary domains

- Consent
- Kvittens
- Grade
- Password (internal)
- Estate (internal, feature-flagged)
- Feedback

## Important architecture points

- App entrypoint: `src/main.ts`
- App shell and global UX states: `src/components/app/*`
- Routes and access control: `src/router/index.ts`
- Route name enums: `src/router/routes.ts`
- Root state and modules: `src/store/store.ts`
- Auth implementation and middleware: `src/plugins/auth/*`
- Runtime config object: `src/Config.ts` and `src/utils/Config.ts`
- Feature flags (Estate): `src/utils/useFeatureFlags.ts`

## Access model rules

Route access is controlled via route `meta`:

- `requiresExternalLogin`
- `requiresInternalLogin`
- `requiresGroup`
- `requiresUnauthenticated`
- `requiresFeature`

When adding routes, always set the correct `meta` values and verify guard behavior in `useAuthMiddleware`.

## Runtime configuration rules

- Environment variables must use `VUE_APP_` prefix (see Vite config).
- Runtime config can be overridden by `window.vueAppServerConfig`.
- Do not hardcode endpoint URLs or auth values in components.

## Coding guidelines for agents

- Keep changes scoped and minimal.
- Follow existing TypeScript + Vue style and naming.
- Preserve route naming conventions from enums in `src/router/routes.ts`.
- Prefer shared base components in `src/components/base` before creating duplicate UI controls.
- Keep interfaces/enums in `src/models` aligned with API payloads.
- Add/adjust tests under nearest `__tests__` folders when behavior changes.

## Common commands

- Install: `npm install`
- Dev server: `npm run serve`
- Build: `npm run build`
- Test: `npm run test`
- Lint: `npm run lint`

## Deployment notes

- Vite build output goes to `wwwroot/`.
- Static web app runtime config is in `public/staticwebapp.config.json`.

## Pitfalls to avoid

- Using `npm run dev` (this repo uses `npm run serve`).
- Forgetting role/group gate checks for internal routes.
- Breaking auth client name checks used by route middleware.
- Ignoring feature-flag checks for Estate routes.
