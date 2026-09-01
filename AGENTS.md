# AGENTS.md

Estate platform monorepo for Umeå Kommun. It contains the EstateService API and
Fastighetsportalen, an internal Vue SPA. The frontend and API are deployed
separately but are commonly changed together in one PR.

## Stack

- C# (latest LTS) on **.NET 10** / ASP.NET Core
- **EF Core 10** (SQL Server in deployed envs, SQLite locally / in integration tests)
- **FusionCache** (memory + Azure Blob L2) for image and read-heavy caching
- **Microsoft.Extensions.Http.Resilience** (Polly) for outbound Pythagoras calls
- **JWT bearer** auth against Umeå's internal token service
- **Azure OpenAI** via `Microsoft.Extensions.AI` for WorkOrder category classification (uses the centralized AI Foundry resource in `ume-rg-general`, not a repo-owned OpenAI resource)
- **xUnit.net v2** + **Shouldly** for backend tests; **Swashbuckle** for OpenAPI
- **Bicep** + **Azure DevOps** pipelines for IaC and CI/CD
- **Vue 3** + **TypeScript**, **Vuetify 4**, **Vite 6**, and **Vitest** for the frontend

Backend package versions live in `Directory.Packages.props`. Frontend packages
live in `src/ume-stapp-estateplatform/package.json`. The solution file is
`src/ume-app-estateservice/Umea.se.EstateService.slnx`.

Frontend-specific instructions live in
`src/ume-stapp-estateplatform/AGENTS.md`. Read that file before changing
anything under that directory; it documents auth, environment, localization,
and duplicated-component constraints.

## Commands

Build: `dotnet build`
Test all: `dotnet test`
Test single project: `dotnet test src/ume-app-estateservice/Umea.se.EstateService.Test/`
Test single test: `dotnet test --filter "FullyQualifiedName~{ClassName}.{MethodName}"`
Run API: `dotnet run --project src/ume-app-estateservice/Umea.se.EstateService.API`
Clean: `dotnet clean`

Frontend (run from `src/ume-stapp-estateplatform`):
Install: `npm ci`
Run: `npm run serve`
Build: `npm run build`
Test: `npm test`
Lint/fix: `npm run lint`

The API needs Key Vault access (or local user-secrets) for Pythagoras, OpenAI, blob storage, and Application Insights — see the README "Configuration" section.

## Repo Layout

| Path                                                | Purpose                                               |
| --------------------------------------------------- | ----------------------------------------------------- |
| `src/ume-app-estateservice/Umea.se.EstateService.*` | API, Logic, ServiceAccess, DataStore, Shared, Test    |
| `src/ume-app-estateservice/Umea.se.Toolkit.Images/` | Shared image processing/caching helpers               |
| `src/ume-app-estateservice/docs/`                   | API docs (e.g. `work-orders-api.md`)                  |
| `src/ume-stapp-estateplatform/`                     | Vue SPA; has its own `AGENTS.md` and `CLAUDE.md`      |
| `iac/`                                              | Bicep IaC                                             |
| `pipelines/`                                        | Azure DevOps pipeline definitions                     |
| `SEARCH.MD`                                         | In-memory search engine internals (scoring, indexing) |

## Conventions

- Code style and naming: see `.ai/code-style.md`
- Testing patterns: see `.ai/testing.md`
- Architecture overview: see `.ai/architecture.md`
- Boundaries (always / ask first / never): see `.ai/boundaries.md`
- GitHub Copilot guidance also lives in `.github/copilot-instructions.md` (kept in sync with the above)
