# Copilot Instructions

## Build, test, and lint commands

Run commands from the repository root:

```sh
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --configuration Release
dotnet format --verify-no-changes
```

Run one test method with Microsoft Testing Platform and xUnit v3:

```sh
dotnet test tests/lib/BinkyLabs.PublicApi.Promoter.Tests.csproj -- --filter-method "*PromoteAsync_WhenUnshippedContainsEntries_AppendsToShipped"
```

Generate the same style of coverage output used in CI:

```sh
dotnet test --configuration Release --coverlet --coverlet-output-format cobertura
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reporttypes:"MarkdownSummaryGithub;Cobertura" -reports:**/coverage.cobertura.*.xml -targetdir:./reports/coverage
```

## Architecture

This repository is a reusable GitHub Action that only handles the file-promotion portion of Public API export maintenance.

- `action.yml` is a composite action that installs .NET 10 and runs the CLI project from `src/tool`.
- `src/lib` contains the promotion engine. It finds matching `*.Unshipped*.txt` files, infers sibling shipped files in the same directory, applies `*REMOVED*` deletions, appends promoted lines, and resets unshipped files to `#nullable enable`.
- `src/tool` contains the thin CLI wrapper that parses action inputs and writes action outputs such as `changed`, `files-changed`, and `changed-files`.
- `tests/lib` covers the promotion engine behavior and edge cases. `tests/tool` covers CLI parsing and GitHub output writing.
- `.github/workflows/dotnet.yml` follows the same sibling-repo testing pattern: format check, Release build, Coverlet-based coverage, ReportGenerator aggregation, and a hard failure below 95% line coverage.
- `release-please-config.json`, `.release-please-manifest.json`, and `.github/workflows/release-please.yml` manage stable releases from `main` using Release Please and Conventional Commits.

## Key conventions

- Follow the `openapi-overlays-dotnet` test stack: .NET 10, Microsoft Testing Platform, xUnit v3 (`xunit.v3.mtp-v2`), `coverlet.MTP`, and ReportGenerator.
- Keep the action narrowly scoped to mutating shipped and unshipped export files. Do not move branch management, commits, pushes, or pull-request creation into this repository's runtime logic.
- Preserve cross-platform output stability in the promotion engine: write LF line endings, emit forward-slash relative paths in action outputs, and treat `#nullable enable` plus `*REMOVED*` markers as part of the file format contract.
- Use file-scoped namespaces and the existing `.editorconfig` style defaults rather than introducing alternative C# formatting patterns.
- Treat Conventional Commits as release metadata, not just commit style: Release Please uses them to decide stable version bumps and changelog entries on `main`.

## Commit message format

Always use Conventional Commits when creating commits. Follow this structure:

```txt
<type>[optional scope]: <short description>

[optional body]

[optional footer(s)]
```

The header must always include a `type` and a short description. Add a scope when it makes the affected area clearer.

### Types

- `feat`: a new feature
- `fix`: a bug fix
- `perf`: a performance improvement
- `refactor`: a code change that neither fixes a bug nor adds a feature
- `test`: adding or correcting tests
- `style`: formatting or other changes that do not affect behavior
- `docs`: documentation-only changes
- `build`: changes to build tooling or dependencies
- `ci`: changes to CI or automation configuration
- `chore`: repository maintenance or other non-feature work

### Scope

Use the scope to identify the package, project area, or repository surface affected by the change.

Typical scopes here are `action`, `promoter`, `cli`, `tests`, `docs`, or `github`.

### Examples

```txt
feat(action): add public API promotion action
fix(promoter): preserve LF output when promoting shipped exports
test(cli): cover GitHub output file generation
ci(github): enforce 95 percent coverage threshold
```

### Breaking changes

If a commit introduces a breaking change, either add `!` after the type or scope, or include a footer that starts with `BREAKING CHANGE:`:

```txt
feat(api)!: change promotion configuration format

BREAKING CHANGE: Promotion definitions now require explicit environment names.
```
