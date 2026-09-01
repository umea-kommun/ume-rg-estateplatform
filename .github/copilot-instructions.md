# Copilot Instructions

The full project guidance lives in [`AGENTS.md`](../AGENTS.md) and the
[`.ai/`](../.ai/) directory:

- [`.ai/code-style.md`](../.ai/code-style.md) — formatting, naming, `var` rules, notable analyzers
- [`.ai/testing.md`](../.ai/testing.md) — xUnit.net + Shouldly and Vitest conventions
- [`.ai/architecture.md`](../.ai/architecture.md) — modules and domain terms
- [`.ai/boundaries.md`](../.ai/boundaries.md) — always / ask first / never

## Copilot-specific nudges

- Make only high-confidence suggestions when reviewing code changes.
- Treat `.editorconfig` and analyzer settings as load-bearing. Some backend projects treat warnings as errors; do not introduce warnings in the others.
- Before changing `src/ume-stapp-estateplatform/`, read its nested `AGENTS.md` for frontend-specific constraints.
- Add XML doc + Swagger annotations to new public Controller endpoints (`<example>` / `<code>` where useful). `CS1591` is silenced globally, so this is a team convention, not a build failure.
- Never modify `global.json` or `NuGet.Config` unless explicitly asked.
