# Building DisplayForge

## Prerequisites

- Windows 10 / 11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PowerShell 7+ recommended for `build-msi.ps1` (Windows PowerShell 5.1 often works)

## Application (debug / local run)

```powershell
dotnet build
dotnet run --project src/DisplayForge
dotnet test
```

Publish without installer:

```powershell
# Framework-dependent (default for release; needs .NET 10 Desktop Runtime at runtime)
dotnet publish src/DisplayForge -c Release -r win-x64 --self-contained false -o artifacts/publish/win-x64

# Self-contained (embeds runtime; larger install folder)
dotnet publish src/DisplayForge -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64
```

## Installers (MSI + Setup.exe)

`build-msi.ps1` publishes a **framework-dependent win-x64** build by default, then builds:

1. WiX **MSI** (app only; multi-culture installer UI)
2. WiX **Bundle** `*-Setup.exe` that installs **.NET 10 Desktop Runtime** if missing, then runs the MSI

```powershell
# All cultures that have installer/DisplayForge.Installer/Loc/*.wxl
.\build-msi.ps1

# Override product version (also written into the MSI / bundle ProductVersion)
.\build-msi.ps1 -Version 1.2.0

# Subset of UI languages (faster local iteration)
.\build-msi.ps1 -Cultures "ja-JP;en-US"

# Reuse previous publish output
.\build-msi.ps1 -SkipPublish

# MSI only (no Setup.exe bootstrapper)
.\build-msi.ps1 -SkipBundle

# Self-contained app MSI (no bootstrapper; runtime embedded in install dir)
.\build-msi.ps1 -SelfContained

# Pin the embedded Desktop Runtime redistributable version
.\build-msi.ps1 -DotNetDesktopRuntimeVersion 10.0.10
```

### Output layout

| Path | Content |
|------|---------|
| `artifacts/publish/win-x64/` | Published app (gitignored) |
| `artifacts/prereqs/` | Downloaded `windowsdesktop-runtime-*-win-x64.exe` (gitignored) |
| `artifacts/msi/DisplayForge-{ver}-win-x64-{culture}-Setup.exe` | **Recommended** bootstrapper |
| `artifacts/msi/DisplayForge-{ver}-win-x64-{culture}.msi` | App MSI (requires runtime if FDD) |
| `artifacts/obj/installer/` | Intermediate WiX MSI build files |
| `artifacts/obj/bootstrapper/` | Intermediate WiX Bundle build files |

Example asset names:

- `DisplayForge-0.1.1-win-x64-en-US-Setup.exe` (runtime + MSI)
- `DisplayForge-0.1.1-win-x64-ja-JP.msi` (MSI only)

### Install / uninstall

| Item | Value |
|------|--------|
| Recommended installer | `*-Setup.exe` (installs .NET 10 Desktop Runtime if needed) |
| Install directory | `C:\Program Files\DisplayForge\` |
| Shortcuts | Start Menu / Desktop |
| Start with Windows | Install UI option (default: on); registry `HKLM\...\Run` |
| Launch after install | Exit dialog checkbox (default: on); starts DisplayForge.exe |
| Upgrade | Same UpgradeCode → major upgrade |
| User data | `%AppData%\DisplayForge\` (kept after uninstall) |
| Runtime on uninstall | Desktop Runtime is **left installed** (shared with other apps) |

```powershell
# Recommended
.\artifacts\msi\DisplayForge-0.1.1-win-x64-en-US-Setup.exe

# MSI only (needs .NET 10 Desktop Runtime already installed for FDD builds)
msiexec /i artifacts\msi\DisplayForge-0.1.1-win-x64-en-US.msi

# Quiet MSI: disable start-with-Windows
msiexec /i artifacts\msi\DisplayForge-0.1.1-win-x64-en-US.msi /qn START_WITH_WINDOWS=0

msiexec /x artifacts\msi\DisplayForge-0.1.1-win-x64-en-US.msi
```

Installer sources:

- MSI: `installer/DisplayForge.Installer/`
- Bootstrapper: `installer/DisplayForge.Bootstrapper/`

## CI and GitHub Releases

Automation is aligned with [container-registry/oss-project-template](https://github.com/container-registry/oss-project-template) (see [CHECKLIST.md](../CHECKLIST.md) and [CONTRIBUTING.md](../CONTRIBUTING.md)).

| Workflow | Trigger | Role |
|----------|---------|------|
| **CI** (`ci.yml`) | push/PR to `main` | restore, build, test on `windows-latest` |
| **PR Title** (`pr-title.yml`) | PR open/edit/sync | Conventional Commits on PR title |
| **Spell Check** (`spellcheck.yml`) | markdown/yaml changes | typos via `.typos.toml` |
| **License Check** (`license-check.yml`) | csproj / lockfile changes | NuGet license scan |
| **Dependency Review** (`dependency-review.yml`) | PR | dependency + license policy |
| **Labeler / Size** | PR | path labels + size/XS–XL |
| **Scorecard** (`scorecard.yml`) | schedule / main | OpenSSF Scorecard |
| **Release Please** (`release-please.yml`) | push to `main` | release PR; on merge → tag + GitHub Release + assets job |
| **Release Assets** (`release-assets.yml`) | `workflow_call`, tag `v*`, or manual | `build-msi.ps1`, upload Setup.exe + MSIs |

Tag version is SemVer without a process mismatch: tag `v1.2.0` → MSI `ProductVersion` `1.2.0`.

### Automated versioning (Release Please)

Config:

- `release-please-config.json` — strategy (`simple`), changelog sections, extra files (csproj / WiX)
- `.release-please-manifest.json` — last released version
- `version.txt` — simple strategy version file (kept in sync with csproj)

Maintainer flow:

1. Land Conventional Commits on `main` (`fix:`, `feat:`, `feat!:`, …) via squash merge.
2. Wait for the bot Release PR; merge it when ready to ship.
3. Setup.exe and MSIs appear on the GitHub Release for `vX.Y.Z`.

If Release Please cannot open PRs, enable **Allow GitHub Actions to create and approve pull requests** under repository Actions settings.

### Manual tag release

```powershell
git tag v1.2.0
git push origin v1.2.0
```

## Publishing checklist (maintainers)

After the repository is on GitHub:

- [x] Owner/repo links point to `dreamingdog0529/DisplayForge`
- [ ] Apply `.github/settings.yml` (description, topics, squash-only, labels) — optional `SETTINGS_TOKEN`
- [ ] Enable Dependabot alerts, secret scanning, and push protection (also in `settings.yml`)
- [ ] Optional: protect `main` with required CI status checks; install [dco2](https://github.com/apps/dco2)
- [ ] Enable **Allow GitHub Actions to create and approve pull requests** (for Release Please)
- [x] Version automation: Release Please + MSI attach via `release-please.yml` / `release-assets.yml`
- [ ] See root [CHECKLIST.md](../CHECKLIST.md) for remaining template verification steps
