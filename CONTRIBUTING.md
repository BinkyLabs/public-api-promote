# Contributing to public-api-promote

Thanks for your interest in contributing to `public-api-promote`! We welcome contributions from everyone. These guidelines should help you get started and keep changes aligned with the rest of the repository.

## Getting started

To work on this repository locally, install:

- [.NET SDK 10.0](https://get.dot.net/)

## Recommended tools

- [Visual Studio Code](https://code.visualstudio.com/)
- [reportgenerator](https://www.nuget.org/packages/dotnet-reportgenerator-globaltool), if you want to generate local coverage reports in the same format used by CI

## Building the project

```sh
dotnet restore
dotnet build --no-restore --configuration Release
```

## Running the tests

```sh
dotnet test --configuration Release
```

Run one test method:

```sh
dotnet test tests/lib/BinkyLabs.PublicApi.Promoter.Tests.csproj -- --filter-method "*PromoteAsync_WhenUnshippedContainsEntries_AppendsToShipped"
```

Generate a local coverage report:

```sh
dotnet test --configuration Release --coverlet --coverlet-output-format cobertura
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reporttypes:"MarkdownSummaryGithub;Cobertura" -reports:**/coverage.cobertura.*.xml -targetdir:./reports/coverage
```

Check formatting before you open a pull request:

```sh
dotnet format --verify-no-changes
```

## Contributing code

1. Fork the repository and clone it locally. `gh repo fork BinkyLabs/public-api-promote --clone`
2. Create a branch for your change. `git checkout -b my-change`
3. Make your changes.
4. Add or update tests when behavior changes.
5. Update documentation when inputs, outputs, usage, or repository workflows change.
6. Run the build, test, and format commands from this document.
7. Push your branch and open a pull request. `gh pr create`

## Repository-specific guidance

This repository is intentionally narrow in scope:

- Keep the runtime logic focused on promoting entries from `*.Unshipped*.txt` files into sibling `*.Shipped*.txt` files.
- Do not move commit, push, branch-management, or pull-request automation into the action itself.
- Preserve cross-platform behavior in the promotion engine: LF line endings, forward-slash relative paths in outputs, and the existing `#nullable enable` / `*REMOVED*` file-format contract.
- Prefer adding logic to `src/lib` and keeping `src/tool` as a thin CLI wrapper around the library.

## Commit message format

To support automated releases, pull requests should use the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) format.

Each commit message consists of a header, an optional body, and an optional footer. The header must contain a type and a short description. An optional scope can be used to identify the affected area.

```txt
<type>[optional scope]: <short description>

<optional body>

<optional footer(s)>
```

Recommended commit types:

- `feat` for new features
- `fix` for bug fixes
- `perf` for performance improvements
- `refactor` for code changes that neither add a feature nor fix a bug
- `test` for test updates
- `style` for formatting-only changes
- `docs` for documentation updates
- `build` for build tooling or dependency changes
- `ci` for CI or automation changes
- `chore` for repository maintenance work

Typical scopes in this repository include `action`, `promoter`, `cli`, `tests`, `docs`, and `github`.

If a commit introduces a breaking change, either add `!` after the type or scope, or include a footer that starts with `BREAKING CHANGE:`.

## Reporting bugs

If you find a bug, please open an issue with a clear description, reproduction steps, and any relevant logs or example input files.

## License

By contributing to this project, you agree that your contributions will be licensed under the MIT license.
