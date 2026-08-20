# CLAUDE

## Context

This is the frontend repository for Mina sidor (mypage.umea.se) in a multi-repo platform.
This repo is responsible for UI, routing, auth-driven access control, state handling, localization, and frontend-to-API integration.

## What to optimize for

- Safe, minimal changes
- Correct route access behavior
- Typed models and maintainable Vue components
- No hardcoded environment-specific values

## Project shape

- `src/components/external/*`: citizen-facing flows
- `src/components/internal/*`: staff/internal flows
- `src/components/base/*`: shared reusable UI primitives
- `src/router/*`: route enums and route definitions
- `src/store/*`: root and module Vuex stores
- `src/plugins/auth/*`: auth provider wiring and middleware
- `src/utils/*`: config and helper utilities

## Critical behavior checks

- Route `meta` drives auth/group/feature gating.
- Internal access depends on auth client name and configured group IDs.
- Estate routes depend on runtime feature flags.
- Session persistence is selective via Vuex persist reducer.

## Configuration rules

- Use `VUE_APP_` environment variables.
- Assume runtime values can be injected from `window.vueAppServerConfig`.
- Keep secrets and production-only values out of committed code.

## Command reference

- `npm install`
- `npm run serve`
- `npm run build`
- `npm run test`
- `npm run lint`

## Editing guidance

- Follow existing style and naming conventions.
- Keep changes localized; avoid unrelated refactors.
- Update tests and docs when behavior or architecture changes.
- If route or auth behavior changes, review `src/router/index.ts` and `src/plugins/auth/index.ts` together.
