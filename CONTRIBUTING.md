# Contributing to DisplayForge

Thanks for your interest in contributing. This document covers how to develop, test, and propose changes.

## Code of conduct

Participation is governed by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Development environment

| Requirement | Notes |
|-------------|--------|
| OS | Windows 10 / 11 (x64) |
| SDK | [.NET 10 SDK](https://dotnet.microsoft.com/download) |
| IDE (optional) | Visual Studio 2022+ or VS Code / Cursor with C# support |
| Git hooks (optional) | `pwsh ./scripts/install-git-hooks.ps1` — Conventional Commits on `commit-msg` |

WiX Toolset packages are restored via NuGet when you build the installer projects; no separate WiX install is required.

## Build, run, test

```powershell
# From repository root
dotnet build
dotnet test
dotnet run --project src/DisplayForge
```

Tray-style close behavior while developing:

```powershell
dotnet run --project src/DisplayForge -- --tray-on-close
```

## Installers (local)

```powershell
.\build-msi.ps1
.\build-msi.ps1 -Version 1.2.0
.\build-msi.ps1 -Cultures "ja-JP;en-US"   # faster subset
.\build-msi.ps1 -SkipPublish              # reuse existing publish output
```

Outputs land under `artifacts/msi/` (gitignored). Naming:

- `DisplayForge-{version}-win-x64-{culture}-Setup.exe` (recommended; installs .NET 10 Desktop Runtime if missing)
- `DisplayForge-{version}-win-x64-{culture}.msi` (app only)

More detail: [docs/building.md](docs/building.md).

## Project layout

```
src/DisplayForge                  WPF UI, tray, hotkeys
src/DisplayForge.Core             Display API, profiles, matching
tests/DisplayForge.Core.Tests
installer/DisplayForge.Installer  WiX MSI
installer/DisplayForge.Bootstrapper  WiX Bundle (Setup.exe + runtime)
tools/                            Dev utilities (icons, locales)
```

## Pull requests

1. Fork (if external) or branch from `main`.
2. Keep changes focused; prefer small PRs.
3. Run `dotnet build` and `dotnet test` before opening the PR.
4. Update `CHANGELOG.md` under `[Unreleased]` when the change is user-visible.
5. Fill in the PR template checklist.

### Commit messages (required)

Use [Conventional Commits](https://www.conventionalcommits.org/). CI rejects PRs whose **title** or **commit subjects** do not match (workflow: `.github/workflows/conventional-commits.yml`). Release Please uses the same prefixes to choose the next SemVer automatically.

| Prefix | Effect | Example |
|--------|--------|---------|
| `fix:` | patch | `fix: tray icon not restoring window` |
| `feat:` | minor | `feat: add monitor identify overlay` |
| `feat!:` / `fix!:` / `BREAKING CHANGE:` | major | `feat!: drop Windows 10 1809 support` |
| `docs:`, `chore:`, `ci:`, `refactor:`, `test:`, `build:`, `perf:`, `style:`, `revert:` | no version bump (unless breaking) | `ci: require conventional PR titles` |

Rules of thumb:

- Format: `type(optional-scope): description` (description required; prefer lowercase start).
- **Squash merge:** the **PR title** becomes the commit on `main` — keep it Conventional.
- **Bots exempt from this CI check:** Dependabot (`chore(deps):` via `dependabot.yml`) and Release Please (`github-actions[bot]` / `release-please--*` branches; title is usually `chore(main): release X.Y.Z`).
- Local hook also accepts release subjects such as `chore(main): release 1.2.3` and `Release 1.2.3` so release commits are not blocked by mistake.

#### Local commit-msg hook (recommended)

CI and the local hook share the same rules (`scripts/validate-commit-subject.sh`). Enable once per clone so invalid subjects fail **before** the commit is created:

```powershell
pwsh ./scripts/install-git-hooks.ps1
# equivalent: git config core.hooksPath .githooks
```

After that, a bad subject is rejected immediately:

```text
Commit message is not Conventional Commits: fixed tray icon
Use Conventional Commits, for example:
  fix: tray icon not restoring window
  ...
```

To bypass in an emergency only: `git commit --no-verify` (CI will still reject the PR).
## Release process (maintainers)

Releases are automated with **[Release Please](https://github.com/googleapis/release-please)** (GitHub Actions).

1. Merge feature/fix PRs to `main` using Conventional Commits.
2. Release Please opens or updates a **Release PR** (`chore(main): release X.Y.Z`) that:
   - bumps `version.txt`, csproj `<Version>`, WiX default `ProductVersion`
   - updates `CHANGELOG.md` and `.release-please-manifest.json`
3. When you are ready to ship, **merge the Release PR**.
4. Release Please creates tag `vX.Y.Z` and a GitHub Release; the same workflow then builds multi-culture MSIs and attaches them.

### Repository settings (one-time)

- **Settings → Actions → General → Workflow permissions**: allow read/write, and enable **Allow GitHub Actions to create and approve pull requests**.

### Manual / emergency release

Still works without Release Please:

```powershell
# After bumping version files yourself
git tag v1.2.0
git push origin v1.2.0
```

Tag push runs `.github/workflows/release.yml` (MSI + GitHub Release assets).

Semantic versioning: `MAJOR.MINOR.PATCH` (breaking / features / fixes). Before `1.0.0`, `feat:` bumps **minor** (`bump-minor-pre-major`).

## Security

Do not file public issues for vulnerabilities. See [SECURITY.md](SECURITY.md).

## Questions

Open a GitHub Discussion or Issue (question / feature request templates) after the repository is published.
