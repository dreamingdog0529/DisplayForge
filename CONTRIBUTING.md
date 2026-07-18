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

WiX Toolset packages are restored via NuGet when you build the installer project; no separate WiX install is required for MSI builds.

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

## MSI installer (local)

```powershell
.\build-msi.ps1
.\build-msi.ps1 -Version 1.2.0
.\build-msi.ps1 -Cultures "ja-JP;en-US"   # faster subset
.\build-msi.ps1 -SkipPublish              # reuse existing publish output
```

Outputs land under `artifacts/msi/` (gitignored). Naming:

`DisplayForge-{version}-win-x64-{culture}.msi`

More detail: [docs/building.md](docs/building.md).

## Project layout

```
src/DisplayForge          WPF UI, tray, hotkeys
src/DisplayForge.Core     Display API, profiles, matching
tests/DisplayForge.Core.Tests
installer/DisplayForge.Installer   WiX MSI
tools/                    Dev utilities (icons, locales)
```

## Pull requests

1. Fork (if external) or branch from `main`.
2. Keep changes focused; prefer small PRs.
3. Run `dotnet build` and `dotnet test` before opening the PR.
4. Update `CHANGELOG.md` under `[Unreleased]` when the change is user-visible.
5. Fill in the PR template checklist.

### Commit messages

Use clear, imperative summaries (e.g. `Add monitor identify overlay`, `Fix hotkey unregister on exit`).

## Release process (maintainers)

1. Move `[Unreleased]` notes into a new version section in `CHANGELOG.md`.
2. Bump `<Version>` in `src/DisplayForge/DisplayForge.csproj` (and related projects if needed).
3. Commit on `main`, then tag and push:

   ```powershell
   git tag v1.2.0
   git push origin main --tags
   ```

4. The `release` GitHub Actions workflow builds multi-culture MSIs and publishes a GitHub Release with those assets.

Semantic versioning: `MAJOR.MINOR.PATCH` (breaking / features / fixes).

## Security

Do not file public issues for vulnerabilities. See [SECURITY.md](SECURITY.md).

## Questions

Open a GitHub Discussion or Issue (question / feature request templates) after the repository is published.
