# Template Adoption Checklist (DisplayForge)

Based on [container-registry/oss-project-template](https://github.com/container-registry/oss-project-template) CHECKLIST.md, adapted for this repository.

## Done in-repo

- [x] Copy `.github/` workflows and configs (adapted for .NET)
- [x] Community files under `.github/`: `CODE_OF_CONDUCT.md`, `SUPPORT.md`, `CONTRIBUTING.md`, `SECURITY.md`; plus root `ROADMAP.md`, `CHANGELOG.md` (contributors list lives in `README.md`)
- [x] `.typos.toml`, `lefthook.yml`, `Taskfile.yml`
- [x] `release-please-config.json` (`simple` + csproj/WiX version bumps)
- [x] `release-assets.yml` builds MSI + Setup.exe (instead of Go binaries)
- [x] `license-check.yml` for NuGet (instead of go-licenses)
- [x] Placeholders replaced (`dreamingdog0529` / `DisplayForge`)

## Maintainer one-time (GitHub UI / secrets)

- [ ] Settings → Actions → Workflow permissions: read/write; allow Actions to create PRs
- [ ] Optional: create classic PAT with `repo` scope → secret `SETTINGS_TOKEN` (full `settings.yml` apply)
- [ ] Optional: install [dco2](https://github.com/apps/dco2) for DCO on PRs
- [ ] Merge a settings change so labels from `.github/settings.yml` are created
- [ ] Confirm squash-only merge is applied (or set manually to match `settings.yml`)

## Verification

- [ ] Open a test issue — forms render
- [ ] Open a test PR — auto-labels (path + size), PR title lint
- [ ] Actions: CI, spellcheck, dependency-review run
- [ ] Push `feat:` to main path — Release Please PR updates
- [ ] Merge Release PR — tag, release, installer assets upload

## Local development

```powershell
# hooks (lefthook if installed, else .githooks)
pwsh ./scripts/install-git-hooks.ps1

# or
task setup
task check
```

After everything works, this file may be deleted.
