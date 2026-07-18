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

Publish self-contained (without MSI):

```powershell
dotnet publish src/DisplayForge -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64
```

## MSI installer

`build-msi.ps1` publishes a self-contained **win-x64** build, then builds WiX v7 multi-culture MSIs.

```powershell
# All cultures that have installer/DisplayForge.Installer/Loc/*.wxl
.\build-msi.ps1

# Override product version (also written into the MSI ProductVersion)
.\build-msi.ps1 -Version 1.2.0

# Subset of UI languages (faster local iteration)
.\build-msi.ps1 -Cultures "ja-JP;en-US"

# Reuse previous publish output
.\build-msi.ps1 -SkipPublish

# Framework-dependent publish (requires .NET 10 Desktop Runtime on targets)
.\build-msi.ps1 -FrameworkDependent
```

### Output layout

| Path | Content |
|------|---------|
| `artifacts/publish/win-x64/` | Published app (gitignored) |
| `artifacts/msi/DisplayForge-{ver}-win-x64-{culture}.msi` | Named installers |
| `artifacts/obj/installer/` | Intermediate WiX build files |

Example asset name: `DisplayForge-0.1.0-win-x64-ja-JP.msi`.

### Install / uninstall

| Item | Value |
|------|--------|
| Install directory | `C:\Program Files\DisplayForge\` |
| Shortcuts | Start Menu / Desktop |
| Start with Windows | Install UI option (default: on); registry `HKLM\...\Run` |
| Upgrade | Same UpgradeCode → major upgrade |
| User data | `%AppData%\DisplayForge\` (kept after uninstall) |

```powershell
msiexec /i artifacts\msi\DisplayForge-0.1.0-win-x64-en-US.msi

# Quiet: disable start-with-Windows
msiexec /i artifacts\msi\DisplayForge-0.1.0-win-x64-en-US.msi /qn START_WITH_WINDOWS=0

# Quiet: enable start-with-Windows (default)
msiexec /i artifacts\msi\DisplayForge-0.1.0-win-x64-en-US.msi /qn START_WITH_WINDOWS=1

msiexec /x artifacts\msi\DisplayForge-0.1.0-win-x64-en-US.msi
```

Installer sources: `installer/DisplayForge.Installer/`.

## CI and GitHub Releases

| Workflow | Trigger | Role |
|----------|---------|------|
| **CI** (`.github/workflows/ci.yml`) | push/PR to `main` | restore, build, test on `windows-latest` |
| **Release Please** (`.github/workflows/release-please.yml`) | push to `main` | open/update Release PR; on merge → tag + GitHub Release + call MSI build |
| **Release** (`.github/workflows/release.yml`) | tag `v*`, `workflow_call`, or manual dispatch | `build-msi.ps1`, upload MSIs to the GitHub Release |

Tag version is SemVer without a process mismatch: tag `v1.2.0` → MSI `ProductVersion` `1.2.0`.

### Automated versioning (Release Please)

Config:

- `release-please-config.json` — strategy (`simple`), extra files (csproj / WiX)
- `.release-please-manifest.json` — last released version
- `version.txt` — simple strategy version file (kept in sync with csproj)

Maintainer flow:

1. Land Conventional Commits on `main` (`fix:`, `feat:`, `feat!:`, …).
2. Wait for the bot Release PR; merge it when ready to ship.
3. MSIs appear on the GitHub Release for `vX.Y.Z`.

If Release Please cannot open PRs, enable **Allow GitHub Actions to create and approve pull requests** under repository Actions settings.

### Manual tag release

```powershell
git tag v1.2.0
git push origin v1.2.0
```

## Publishing checklist (maintainers)

After the repository is on GitHub:

- [x] Owner/repo links point to `dreamingdog0529/DisplayForge`
- [ ] Set repository description and topics (`windows`, `wpf`, `multi-monitor`, `dotnet`, `hotkeys`, …)
- [ ] Enable Dependabot alerts, secret scanning, and push protection
- [ ] Optional: protect `main` with required CI status checks
- [ ] Enable **Allow GitHub Actions to create and approve pull requests** (for Release Please)
- [x] Version automation: Release Please + MSI attach via `release-please.yml` / `release.yml`
