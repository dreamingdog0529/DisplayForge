# Contributing Guide For DisplayForge

Thanks for contributing. This project follows the same principles as
[container-registry/oss-project-template](https://github.com/container-registry/oss-project-template):

| Principle | Implementation |
|-----------|----------------|
| **Conventional Commits** | PR titles (and commits) use `type: description` |
| **Squash merge only** | One commit per PR on `main` |
| **PR-based workflow** | All changes via pull requests |
| **DCO sign-off** | `git commit -s` (`Signed-off-by`) |
| **Automated releases** | Release Please from commit types |
| **Local-first** | Lefthook / Task / git hooks before push |
| **GitHub-native** | Labels, settings, Scorecard, dependency review |

## Code of conduct

Participation is governed by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Getting help

See [SUPPORT.md](SUPPORT.md). Roadmap notes: [ROADMAP.md](../ROADMAP.md).

## Development environment

| Requirement | Notes |
|-------------|--------|
| OS | Windows 10 / 11 (x64) |
| SDK | [.NET 10 SDK](https://dotnet.microsoft.com/download) |
| Optional | [typos](https://github.com/crate-ci/typos), [lefthook](https://github.com/evilmartians/lefthook), [Task](https://taskfile.dev/) |
| Git hooks | `pwsh ./scripts/install-git-hooks.ps1` or `task setup` |

WiX packages restore via NuGet when building installer projects.

### Build, run, test

```powershell
dotnet build
dotnet test
dotnet run --project src/DisplayForge
```

Or with Task:

```bash
task build
task test
task check
```

Tray-style close while developing:

```powershell
dotnet run --project src/DisplayForge -- --tray-on-close
```

### Installers (local)

```powershell
.\build-msi.ps1
.\build-msi.ps1 -Version 1.2.0
.\build-msi.ps1 -Cultures "ja-JP;en-US"
.\build-msi.ps1 -SkipPublish
```

Outputs under `artifacts/msi/` (gitignored). Details: [docs/development.md](../docs/development.md).

### Project layout

```
src/DisplayForge                  WPF UI, tray, hotkeys
src/DisplayForge.Core             Display API, profiles, matching
tests/DisplayForge.Core.Tests
installer/DisplayForge.Installer  WiX MSI
installer/DisplayForge.Bootstrapper  WiX Bundle (Setup.exe + runtime)
tools/                            Dev utilities (icons, locales)
.github/                          Workflows, issue/PR templates, settings
```

## Issues, requests & ideas

Use [GitHub Issues](https://github.com/dreamingdog0529/DisplayForge/issues) with the YAML forms:

- Bug Report
- Feature Request
- Proposal (larger design changes)

Labels such as `bug`, `enhancement`, `proposal`, and `needs-triage` are applied by the forms.

## Contribution checklist

- [ ] Clean, simple, well-styled code
- [ ] Conventional Commits (`type: description`); related issues referenced by number
- [ ] DCO sign-off on every commit (`git commit -s`)
- [ ] Tests pass (`dotnet test` / `task test`)
- [ ] Minimize dependencies; prefer Apache-2.0, BSD-3, MIT, ISC, MPL
- [ ] No secrets or build artifacts (`bin/`, `obj/`, `artifacts/`, `*.msi`)

## Creating a pull request

1. Search Issues; open one if needed.
2. Fork/clone and create a focused branch.
3. Commit with **Conventional Commits** and **DCO**:

   ```bash
   git commit -s -m "fix: tray icon not restoring window"
   ```

4. Push and open a PR against `main`. Fill in the PR template.
5. Ensure CI is green (build/test, PR title, spellcheck, dependency review, etc.).

> Sync your fork before opening the PR if you forked.

### Commit message format

```
<type>[optional scope]: <description>

[optional body]

Signed-off-by: Name <email>
```

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`

| Prefix | Version impact |
|--------|----------------|
| `feat:` | minor |
| `fix:` and most others | patch |
| `feat!:` / `fix!:` / `BREAKING CHANGE:` | major |

Before `1.0.0`, `feat:` still bumps **minor** (`bump-minor-pre-major`).

**Squash merge:** the **PR title** becomes the commit on `main` — keep it Conventional (CI: `pr-title.yml`).

Rules of thumb:

- Format: `type(optional-scope): description` (description required; prefer lowercase start).
- **Bots exempt from PR title CI:** Dependabot (`chore(deps):` via `dependabot.yml`) and Release Please (`github-actions[bot]` / `release-please--*` branches; title is usually `chore(main): release X.Y.Z`).
- Local hook also accepts release subjects such as `chore(main): release 1.2.3` and `Release 1.2.3` so release commits are not blocked by mistake.
- **Merge commits are skipped** (e.g. `Merge branch 'main' into your-branch`): the local hook detects `MERGE_HEAD` / `CHERRY_PICK_HEAD` / `REVERT_HEAD`, and `validate-commit-subject.sh` also skips merge/revert subjects.

### Local hooks

```powershell
pwsh ./scripts/install-git-hooks.ps1
# or: task setup
```

- **pre-commit** (lefthook): spellcheck staged markdown/yaml
- **commit-msg**: Conventional Commits + DCO

## DCO

This project uses the [Developer Certificate of Origin](https://developercertificate.org/).
Sign off every commit with `-s`. Maintainers may enable the [dco2](https://github.com/apps/dco-2) GitHub App (see `.github/dco.yml`).

## Automation

| Area | Workflow / config |
|------|-------------------|
| Build & test | `ci.yml` |
| PR title (Conventional Commits) | `pr-title.yml` |
| PR labels (paths) | `labeler.yml` + `.github/labeler.yml` |
| PR size labels | `pr-size-labeler.yml` |
| Welcome first-timers | `welcome.yml` |
| Spell check | `spellcheck.yml` + `.typos.toml` |
| License check (NuGet) | `license-check.yml` |
| Dependency review | `dependency-review.yml` |
| OpenSSF Scorecard | `scorecard.yml` |
| Repo settings / labels | `settings.yml` + `apply-settings.yml` |
| Contributors | `contributors.yml` → `README.md` (Contributors section) |
| Version & changelog | `release-please.yml` + `release-please-config.json` |
| Release assets (MSI / Setup) | `release-assets.yml` |

### Release process (maintainers)

1. Merge PRs to `main` with Conventional Commits (squash).
2. Release Please opens/updates a release PR.
3. Merge the release PR → tag + GitHub Release.
4. `release-assets.yml` builds multi-culture installers and uploads them.

One-time: Actions read/write + allow Actions to create PRs. Optional: `SETTINGS_TOKEN` (repo PAT) for full `settings.yml` apply; install dco2 for DCO checks on PRs.

## Security

Do not file public issues for vulnerabilities. See [SECURITY.md](SECURITY.md).

## License

By contributing, you agree that your contributions are licensed under the project [MIT License](../LICENSE) and that you have the right to submit them under the DCO.
