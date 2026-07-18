# DisplayForge

English | [日本語](./README_ja.md)

[![GitHub release](https://img.shields.io/github/v/release/dreamingdog0529/DisplayForge?include_prereleases)](https://github.com/dreamingdog0529/DisplayForge/releases/latest)
[![CI](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml/badge.svg)](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Windows multi-monitor **profile switcher**. Inspired by NirSoft MultiMonitorTool’s save/restore workflow, with **per-profile global hotkeys** and **system tray residency** as first-class features.

## Screenshots

**Main window** — profiles, hotkeys, layout editor, and monitor details

![DisplayForge main window](docs/images/main-window.png)

**System tray** — apply a profile, open the app, or exit from the context menu

![DisplayForge system tray menu](docs/images/tray-menu.png)

## Download

**[Open the latest release](https://github.com/dreamingdog0529/DisplayForge/releases/latest)**

| Item | Details |
|------|---------|
| OS | Windows 10 / 11 (x64) |
| Recommended | **`…-Setup.exe`** (installs everything you need) |
| Also available | **`….msi`** (app only; see below) |
| Example file names | `DisplayForge-0.1.1-win-x64-en-US-Setup.exe`, `…-ja-JP-Setup.exe` |
| Settings location | `%AppData%\DisplayForge\` (kept after uninstall) |

### Which file should I download? (Setup.exe vs MSI)

Releases include two kinds of installer. **They install the same app** — pick based on how you want to install:

| File | Who it’s for | What it does |
|------|----------------|--------------|
| **`…-Setup.exe`** (recommended) | Most people | Installs DisplayForge. If [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) is missing, it installs that first automatically. |
| **`….msi`** | Advanced / IT use | Installs DisplayForge only. You must already have the Desktop Runtime, or the app will not run. Handy for company deployment or silent install with `msiexec`. |

**Simple rule:** if you’re not sure, download **Setup.exe**.

- Prefer **ja-JP** or **en-US** in the file name for the *installer wizard* language (Japanese or English). The app itself supports [31 UI languages](#supported-languages) either way.
- Run the installer as administrator when Windows asks.
- If a release is not available yet, you can [build from source](docs/building.md).

## Features

- Save the current monitor layout as a profile
  - Enabled / disabled, primary monitor, resolution, refresh rate, orientation, position
- Apply, duplicate, delete, and rename profiles
- Assign a global hotkey per profile for instant switching
- System tray residency (apply from the context menu)
- UI localization in **31 languages** (follows the system language by default; override in **Settings**)
- Settings and profiles stored as JSON under `%AppData%\DisplayForge\`

## Supported languages

### App UI (31)

Defaults to the Windows display language. Change anytime under **Settings → Language**. Every installer package includes all of these UI languages.

> **Note:** UI strings (and most installer localizations) were produced with AI assistance. Wording may be imperfect or unnatural in places — feedback and corrections are welcome.

| Code | Language | Code | Language |
|------|----------|------|----------|
| `en` | English | `ja` | 日本語 |
| `zh-Hans` | 简体中文 | `zh-Hant` | 繁體中文 |
| `ko` | 한국어 | `de` | Deutsch |
| `fr` | Français | `es` | Español |
| `pt-BR` | Português (Brasil) | `pt-PT` | Português (Portugal) |
| `it` | Italiano | `nl` | Nederlands |
| `pl` | Polski | `ru` | Русский |
| `uk` | Українська | `tr` | Türkçe |
| `cs` | Čeština | `sv` | Svenska |
| `da` | Dansk | `nb` | Norsk bokmål |
| `fi` | Suomi | `hu` | Magyar |
| `ro` | Română | `el` | Ελληνικά |
| `vi` | Tiếng Việt | `th` | ไทย |
| `id` | Bahasa Indonesia | `ms` | Bahasa Melayu |
| `hi` | हिन्दी | `ar` | العربية |
| `he` | עברית | | |

### Installer (Setup / MSI wizard)

GitHub Releases currently ship **en-US** and **ja-JP** Setup.exe (and MSI) packages (installer wizard language only). The app UI language set above is the same in both packages.

## Usage

1. Start the app (the main window opens by default)
2. Optionally minimize to the tray; double-click the tray icon to open the window again
3. Create a profile with **New from current layout**
4. Click the hotkey field and press a chord such as `Ctrl+Alt+1`
5. Change the Windows display layout, then save another profile the same way
6. Switch with the hotkey or the tray menu

In a normal launch, closing the window keeps the app in the tray. Use **Exit** on the tray menu to quit. Prefer tray-only startup via **Settings → Start minimized to tray**.

## Data locations

| File | Contents |
|------|----------|
| `%AppData%\DisplayForge\profiles.json` | Profile list |
| `%AppData%\DisplayForge\settings.json` | Language, notifications, hotkey enablement, etc. |

## Developers (quick start)

Requirements: Windows 10/11 x64, [.NET 10 SDK](https://dotnet.microsoft.com/download)

```powershell
dotnet build
dotnet test
dotnet run --project src/DisplayForge
```

Build installers locally (Setup.exe + MSI):

```powershell
.\build-msi.ps1
```

Full build, silent install, CI/release notes: **[docs/building.md](docs/building.md)**  
How to contribute: **[CONTRIBUTING.md](CONTRIBUTING.md)**

### Architecture overview

```
src/DisplayForge                 WPF UI / tray / hotkeys
src/DisplayForge.Core            Display API, profiles, matching
tests/DisplayForge.Core.Tests
installer/DisplayForge.Installer WiX MSI
installer/DisplayForge.Bootstrapper WiX Bundle (Setup.exe)
```

Display changes use the Windows **CCD** API (`QueryDisplayConfig` / `SetDisplayConfig`).  
The primary monitor is treated as the virtual-desktop origin `(0,0)`.

With `dotnet run`, closing the window also exits the process (so the shell is not left blocked). To keep tray residency while developing:

```powershell
dotnet run --project src/DisplayForge -- --tray-on-close
```

To exit on close even in a normal launch, pass `--exit-on-close`.

## Known limitations

- Aimed at extended desktop (Extend). Advanced clone-only editing is not supported
- When a monitor is disconnected, that profile entry is skipped (partial apply)
- On some Windows 11 setups `SetDisplayConfig` can be flaky; if apply fails, open Windows Display Settings once and try again
- DPI scaling / HDR / window position restore are candidates for future work

## Community & project docs

Repository automation and community files follow
[container-registry/oss-project-template](https://github.com/container-registry/oss-project-template)
(adapted for .NET / Windows installers).

| Document | Purpose |
|----------|---------|
| [CONTRIBUTING.md](CONTRIBUTING.md) | Develop, test, PRs, DCO, CI/CD, releases |
| [SUPPORT.md](SUPPORT.md) | How to get help |
| [ROADMAP.md](ROADMAP.md) | Direction and how to propose work |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | Community standards |
| [SECURITY.md](SECURITY.md) | Private vulnerability reporting |
| [CODEOWNERS](CODEOWNERS) | Default code review owners |
| [CHANGELOG.md](CHANGELOG.md) | Release history |
| [LICENSE](LICENSE) | MIT license text |

## Contributors

Thanks to everyone who has contributed to DisplayForge. This list is updated automatically from git history.

<!-- readme: contributors -start -->
<!-- readme: contributors -end -->

## Third-party

- Icons are based on [Lucide](https://lucide.dev/) ([ISC License](https://lucide.dev/license)):
  - App / tray indicator: `monitor-cog` (source SVG under `src/DisplayForge/Assets/lucide/`; packaged as `Assets/app.ico`)
  - Tray menu: `monitor`, `check`, `settings`, `app-window`, `log-out` (same folder)
  - Full license text: `src/DisplayForge/Assets/LICENSES/lucide-LICENSE.txt`
