# CLAUDE

See `AGENTS.md` for the same content — this file mirrors it so both tools find it.

## Repository summary

Fastighetsportalen — the internal estate frontend for Umeå kommun. Vue 3 +
TypeScript + Vuetify 4 + Vite 6, built with `vue-tsc --noEmit && vite build`.

Internal-only: every route requires AD login through IDProxy. There is no
citizen-facing surface.

## Primary domains

- Estate and building search
- Estate / building / room details, documents, contacts
- Blueprint viewer (SVG) and map viewer (OpenLayers + SWEREF 99 20 15)
- Fault report, order, changed space requirements
- Favorites
- Feedback (posts to EstateService)

## Important architecture points

- App entrypoint: `src/main.ts`
- App shell and global UX states: `src/components/app/*`
- Routes, auth gating and feature gating: `src/router/index.ts`
- Route name enums: `src/router/routes.ts`
- Root store (estate) and the feedback module: `src/store/store.ts`
- Auth implementation and middleware: `src/plugins/auth/*`
- Runtime config object: `src/Config.ts` and `src/utils/Config.ts`
- Feature flags: `src/utils/useFeatureFlags.ts`

## Access model rules

- Every route sets `requiresInternalLogin`. There is one auth client, the
  internal AD one, configured via the `VUE_APP_AUTH_INTERNAL_*` prefix.
- `AuthConfig.ts` identifies the internal client by matching that literal
  prefix. The prefix is load-bearing — see README.
- Route access is additionally gated by runtime flags from `GET /features`
  (`EstateService`, `ErrorReport`). They fail open.
- No AAD group gating is in use, though the middleware still supports
  `requiresGroup`.

## Conventions

- Tabs for indentation, single quotes, Prettier via eslint-plugin-prettier.
- `@/` resolves to `src/`.
- Add env vars to all four `.env*` files or none — the key sets must match.
- Files with a `Duplicated from ume-rg-myplatform` header are shared with Mina
  sidor. Keep the header when editing; it is the diff base for reconciling the
  two copies. Do not add the header to new estate-only files.
- Locale keys live in `src/locales/{sv,en}.json` and the two key sets must stay
  identical.

## Things that fail silently

- `src/components/estate/map/layer.ts` registers the SWEREF 99 20 15 projection
  as a side effect (`proj4.defs` + `register` + `addProjection`). Break it and
  the map renders at the wrong coordinates instead of erroring.
- Map assets are small enough that Vite inlines them; adding a larger one makes
  it a real file request instead.
- `eslint . --fix` needs `.eslintignore` to skip `wwwroot/`.

## Commands

- `npm ci`
- `npm run serve`
- `npm run build`
- `npm test`
- `npm run lint`
