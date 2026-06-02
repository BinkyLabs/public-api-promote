# Copilot Instructions

## Repository state

This repository currently contains only `LICENSE` as a tracked project file. Do not assume an application stack, package manager, test runner, or deployment target until those files are added.

## Architecture

There is no implementation code, service layout, or module structure in the current repository state. Treat the repo as an empty scaffold rather than inferring a web app, library, or API architecture.

## Key conventions

Base recommendations on files that actually exist in the repository. If future work adds source code, build tooling, tests, or other assistant instruction files, incorporate those concrete conventions instead of inventing defaults.

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

Because this repository is currently empty, scope is optional until clearer boundaries exist. When helpful, use a scope that reflects the area being introduced or changed, such as `repo`, `github`, `docs`, or the name of a new project or package being added.

### Examples

```txt
chore(repo): add initial repository scaffolding
docs(readme): document project goals
ci(github): add release workflow
feat(api): add first promotion pipeline
```

### Breaking changes

If a commit introduces a breaking change, either add `!` after the type or scope, or include a footer that starts with `BREAKING CHANGE:`:

```txt
feat(api)!: change promotion configuration format

BREAKING CHANGE: Promotion definitions now require explicit environment names.
```
