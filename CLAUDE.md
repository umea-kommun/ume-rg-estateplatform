# CLAUDE.md

See `AGENTS.md` for universal project instructions (stack, commands, conventions).
Below are Claude Code-specific additions.

## Project Context

This platform is in active production for Umeå Kommun. The root instructions
cover the backend and repository-wide concerns. Before frontend work, also read
`src/ume-stapp-estateplatform/CLAUDE.md`; its auth, environment, localization,
and shared-file rules are specific to that subtree. Treat `.editorconfig` and
the analyzer settings as load-bearing. Some backend projects enable
`TreatWarningsAsErrors`; all projects must still build cleanly. The codebase
prefers directness over abstraction; favor small targeted edits over refactors.

## Working Style

- Verify backend changes with `dotnet build` and `dotnet test` before reporting done.
- Verify frontend changes from `src/ume-stapp-estateplatform` with the relevant
  `npm run build`, `npm test`, and `npm run lint` commands.
- For search-related changes, also re-read `SEARCH.MD` and update it if you alter scoring constants, field weights, `QueryOptions` defaults, or indexed `PythagorasDocument` fields.
- For DataStore changes, generate a new EF Core migration rather than editing existing ones (the deployment pipeline runs them).
- When adding NuGet packages, edit `Directory.Packages.props` (central package management) — don't pin versions in individual `.csproj` files.

## Session Preferences

- Propose a short plan before large changes or any work that crosses project boundaries.
- Don't add `.ConfigureAwait(false)` calls; `CA2007` is intentionally suppressed in API code.
- Don't introduce wrapper/builder helpers for one-off cases — see `.ai/boundaries.md`.

## Related Configs

- `.github/copilot-instructions.md` — Copilot guidance, kept aligned with these files
- `.ai/` — progressive disclosure files referenced from `AGENTS.md`
