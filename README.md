# public-api-promote

`public-api-promote` is a reusable GitHub Action that promotes entries from `*.Unshipped*.txt` public API export files into sibling `*.Shipped*.txt` files.

These files are commonly produced by the [.NET Public API analyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers), which help library authors track intentional public surface changes.

It only performs the file mutation step. Branch creation, commits, pushes, and pull request management stay in the caller's workflow.

## How promotion works

For each matching unshipped export file, the action:

1. Removes `#nullable enable`
2. Treats lines prefixed with `*REMOVED*` as removals from the shipped file
3. Appends any remaining lines to the shipped file in the same directory
4. Resets the unshipped file back to `#nullable enable`

If the `unshipped-glob` input is omitted, the action scans the full repository for `*.Unshipped*.txt`.

## Real-world example

This action is meant to support workflows like [BinkyLabs/openapi-overlays-dotnet#329](https://github.com/BinkyLabs/openapi-overlays-dotnet/pull/329), which automatically promoted unshipped public API entries into the shipped export files and prepared the resulting changes for review.

## Usage

```yaml
name: Promote shipped APIs

on:
  push:
    branches:
      - main
  workflow_dispatch:

jobs:
  promote-apis:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
        with:
          fetch-depth: 0

      - name: Promote public API exports
        id: promote
        uses: BinkyLabs/public-api-promote@main
        with:
          unshipped-glob: 'src/**/*.Unshipped.txt'

      - name: Commit promoted exports
        if: steps.promote.outputs.changed == 'true'
        run: |
          git add -- '*Shipped.txt' '*Unshipped.txt'
          git commit -m "chore: promote shipped APIs"

      # Push and PR creation remain part of the surrounding workflow.
```

## Inputs

| Input | Required | Default | Description |
| --- | --- | --- | --- |
| `repository-root` | No | `.` | Repository root to scan. Relative paths are resolved from the workflow workspace. |
| `unshipped-glob` | No | `**/*.Unshipped*.txt` | Glob pattern used to locate unshipped export files. Matching shipped files are resolved in the same directory by replacing `.Unshipped` with `.Shipped` in the file name. |

## Outputs

| Output | Description |
| --- | --- |
| `changed` | `true` when any shipped or unshipped file was modified. |
| `files-changed` | Number of files modified. |
| `entries-promoted` | Number of unshipped lines appended to shipped exports. |
| `entries-removed` | Number of shipped lines removed because of `*REMOVED*` markers. |
| `changed-files` | Newline-delimited relative file paths that changed. |

## Local development

```sh
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Run one test method:

```sh
dotnet test tests/lib/BinkyLabs.PublicApi.Promoter.Tests.csproj -- --filter-method "*PromoteAsync_WhenUnshippedContainsEntries_AppendsToShipped"
```

Generate a coverage report locally:

```sh
dotnet test --configuration Release --coverlet --coverlet-output-format cobertura
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reporttypes:"MarkdownSummaryGithub;Cobertura" -reports:**/coverage.cobertura.*.xml -targetdir:./reports/coverage
```
