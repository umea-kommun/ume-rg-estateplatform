# GitHub Copilot Instructions

Use this repository as a frontend-only service in a multi-repo platform.

## Scope

- Implement UI and frontend orchestration only.
- Do not invent backend logic that belongs to other repositories.
- Use runtime configuration values (`VUE_APP_*`) instead of hardcoded endpoints.

## Architecture anchors

- Routing/access control: `src/router/index.ts`
- Auth middleware/provider: `src/plugins/auth/index.ts`
- State and persisted slices: `src/store/store.ts`
- Runtime config loading: `src/main.ts`, `src/Config.ts`
- Feature flags: `src/utils/useFeatureFlags.ts`

## Routing and auth

When adding/updating routes:

- set route `meta` correctly (`requiresExternalLogin`, `requiresInternalLogin`, `requiresGroup`, `requiresFeature`)
- verify redirect behavior in auth middleware
- use route names from enums in `src/router/routes.ts`

## Code style and maintenance

- Keep changes small and focused.
- Follow existing Vue 3 + TypeScript patterns.
- Prefer existing base components in `src/components/base`.
- Keep interfaces/enums in `src/models` updated with payload changes.
- Add or adjust tests in nearest `__tests__` folder when behavior changes.

## Dev commands

- `npm install`
- `npm run serve`
- `npm run build`
- `npm run test`
- `npm run lint`
