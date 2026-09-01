# Boundaries

## Always

- Run `dotnet build` and `dotnet test` before claiming a backend change is complete. Some projects treat warnings as errors; do not introduce warnings in the others.
- For frontend changes, follow `src/ume-stapp-estateplatform/AGENTS.md` and run the relevant build, test, and lint commands there.
- Match `.editorconfig` rules — explicit types over `var`, file-scoped namespaces, mandatory braces.
- Use Shouldly assertions and skip `// Arrange/Act/Assert` comments in tests.
- Forward `CancellationToken` to async calls that accept one.
- Add XML doc + Swagger annotations to new public Controller endpoints.

## Ask First

- Adding new NuGet package versions in `Directory.Packages.props` (the project uses central package management — pin a single version).
- New EF Core migrations under `Umea.se.EstateService.DataStore/Migrations/` (the deployment pipeline auto-runs these).
- Changes to `pipelines/`, `iac/`, or `.github/workflows/`.
- New Pythagoras endpoints or HTTP clients — confirm timeout/cache budgets first.
- Changes to public API contracts or response shapes (front-end consumers exist).
- Adding new Azure resources or Key Vault secret names.

## Never

- Commit secrets, API keys, or connection strings — everything sensitive resolves via `@KeyVault(...)`.
- Edit `global.json` or `NuGet.Config` unless explicitly requested (per `.github/copilot-instructions.md`).
- Bypass `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` with `<NoWarn>` unless explicitly approved.
- Add `.ConfigureAwait(false)` calls in API code (`CA2007` is intentionally suppressed).
- Modify generated EF Core migration files after they ship — write a new migration instead.
- Use `var` for declarations; explicit types are the enforced repository style.
- Force-push to `main` or skip code review.
- Reimplement API authorization in the frontend. The frontend may hide actions from `/me` capabilities, but the API remains the enforcement point.

## When Uncertain

- Propose a short plan before large changes.
- Open a draft PR for early review.
- Ask before introducing new abstractions, builder helpers, or wrapper layers — the codebase favors directness.
