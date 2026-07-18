# DisplayForge

[English](./README.md) | [日本語](./README_ja.md)

[![GitHub release](https://img.shields.io/github/v/release/dreamingdog0529/DisplayForge?include_prereleases)](https://github.com/dreamingdog0529/DisplayForge/releases/latest)
[![CI](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml/badge.svg)](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Windows multi-monitor **profile switcher**. Inspired by NirSoft MultiMonitorTool’s save/restore workflow, with **per-profile global hotkeys** and **system tray residency** as first-class features.

## Download

**[Open the latest release](https://github.com/dreamingdog0529/DisplayForge/releases/latest)**

| Item | Details |
|------|---------|
| OS | Windows 10 / 11 (x64) |
| Package | MSI installer (self-contained; .NET runtime included) |
| Example file names | `DisplayForge-1.0.0-win-x64-en-US.msi`, `…-ja-JP.msi`, etc. (per UI culture) |
| Settings location | `%AppData%\DisplayForge\` (kept after uninstall) |

Pick the MSI that matches your preferred installer language and run it elevated. If a release is not available yet, you can [build from source](docs/building.md).

## Features

- Save the current monitor layout as a profile
  - Enabled / disabled, primary monitor, resolution, refresh rate, orientation, position
- Apply, duplicate, delete, and rename profiles
- Assign a global hotkey per profile for instant switching
- System tray residency (apply from the context menu)
- UI localization (30+ languages including English, Japanese, Chinese, Korean, and many European languages; follows the system language by default)
- Settings and profiles stored as JSON under `%AppData%\DisplayForge\`

## Usage

1. Start the app (by default it lives in the tray)
2. Double-click the tray icon to open the main window
3. Create a profile with **New from current layout**
4. Click the hotkey field and press a chord such as `Ctrl+Alt+1`
5. Change the Windows display layout, then save another profile the same way
6. Switch with the hotkey or the tray menu

In a normal launch, closing the window keeps the app in the tray. Use **Exit** on the tray menu to quit.

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

Build an MSI locally:

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

## License

[MIT License](LICENSE)

### Third-party

- The app icon is based on the [Lucide](https://lucide.dev/) `monitor-cog` icon ([ISC License](https://lucide.dev/license)).
  - Source SVG: `src/DisplayForge/Assets/monitor-cog.svg`
  - Full license text: `src/DisplayForge/Assets/LICENSES/lucide-LICENSE.txt`
