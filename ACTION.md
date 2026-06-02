# GitHub Action - public-api-promote

This GitHub Action promotes entries from `*.Unshipped*.txt` public API export files into sibling `*.Shipped*.txt` files inside your repository.

It only performs the file mutation step. Branch creation, commits, pushes, and pull request management remain in your surrounding workflow.

## Quick Start

```yaml
- uses: BinkyLabs/public-api-promote@v1
```

## Features

- Promotes public API entries from unshipped exports into shipped exports
- Supports `*REMOVED*` markers to delete entries from shipped files
- Scans the full repository by default, or a narrower subset when a glob is provided
- Exposes outputs so surrounding workflows can decide whether to commit or open pull requests
- Runs as a composite action with the repository's .NET implementation

## Usage

### Basic Example

```yaml
- name: Promote public API exports
  id: promote
  uses: BinkyLabs/public-api-promote@v1
```

### Limit Promotion to a Subtree

```yaml
- name: Promote public API exports
  id: promote
  uses: BinkyLabs/public-api-promote@v1
  with:
    unshipped-glob: 'src/**/*.Unshipped.txt'
```

### Run from a Specific Repository Root

```yaml
- name: Promote public API exports
  id: promote
  uses: BinkyLabs/public-api-promote@v1
  with:
    repository-root: './artifacts'
    unshipped-glob: '**/*.Unshipped.txt'
```

## Inputs

| Input | Description | Required | Default |
|-------|-------------|----------|---------|
| `repository-root` | Repository root to scan. Relative paths are resolved from the workflow workspace. | No | `.` |
| `unshipped-glob` | Optional glob used to locate unshipped export files. Matching shipped files are resolved in the same directory by replacing `.Unshipped` with `.Shipped` in the file name. | No | full repository scan |

## Outputs

| Output | Description |
|-------|-------------|
| `changed` | `true` when any shipped or unshipped file was modified |
| `files-changed` | Number of files modified |
| `entries-promoted` | Total number of lines appended to shipped exports |
| `entries-removed` | Total number of lines removed from shipped exports |
| `changed-files` | Newline-delimited relative file paths that changed |

## Complete Workflow Example

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
      - name: Checkout repository
        uses: actions/checkout@v6
        with:
          fetch-depth: 0

      - name: Promote public API exports
        id: promote
        uses: BinkyLabs/public-api-promote@v1
        with:
          unshipped-glob: 'src/**/*.Unshipped.txt'

      - name: Show changed files
        if: steps.promote.outputs.changed == 'true'
        shell: bash
        run: |
          echo "Changed files:"
          echo "${{ steps.promote.outputs.changed-files }}"

      - name: Commit promoted exports
        if: steps.promote.outputs.changed == 'true'
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add -- '*Shipped.txt' '*Unshipped.txt'
          git commit -m "chore: promote shipped APIs"
```

## Troubleshooting

### No Files Changed

If the action reports `changed=false`, either no matching `*.Unshipped*.txt` files were found or the matching files only contained `#nullable enable`.

### Shipped File Missing

If an unshipped file contains `*REMOVED*` entries, the matching shipped file must already exist in the same directory.

### Glob Does Not Match

`unshipped-glob` is evaluated relative to `repository-root`. If your workflow checks out into the default workspace, a value like `src/**/*.Unshipped.txt` matches files under the repository root.

## Notes

- The action writes LF line endings so output remains stable across runners
- Relative paths in outputs use forward slashes
- This repository uses stable `v<major>` and `v<major>.<minor>` floating tags for action consumption

## Related Resources

- [Project Repository](https://github.com/BinkyLabs/public-api-promote)
- [README](./README.md)
- [Contributing Guide](./CONTRIBUTING.md)
