# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.2](https://github.com/dreamingdog0529/DisplayForge/compare/v0.2.1...v0.2.2) (2026-07-18)


### Miscellaneous

* align repository scaffolding with oss-project-template ([#28](https://github.com/dreamingdog0529/DisplayForge/issues/28)) ([e61431b](https://github.com/dreamingdog0529/DisplayForge/commit/e61431b8b63ee2ff8c0b293da2b7944a178e816a))


### CI/CD

* fix first-interaction v3 input names in welcome ([#30](https://github.com/dreamingdog0529/DisplayForge/issues/30)) ([cf66da1](https://github.com/dreamingdog0529/DisplayForge/commit/cf66da14931efe4899fa0821e06d923a86472dfa))

## [0.2.1](https://github.com/dreamingdog0529/DisplayForge/compare/v0.2.0...v0.2.1) (2026-07-18)


### Documentation

* clarify Setup.exe vs MSI and AI translation note ([#24](https://github.com/dreamingdog0529/DisplayForge/issues/24)) ([efe31e2](https://github.com/dreamingdog0529/DisplayForge/commit/efe31e2fdda6b7714472423541b7a1a52b47c4c4))
* explain Setup.exe vs MSI for end users in README ([#23](https://github.com/dreamingdog0529/DisplayForge/issues/23)) ([36ca19e](https://github.com/dreamingdog0529/DisplayForge/commit/36ca19e1a03a552d07c6a3987d16ac36de2bcc80))


### CI/CD

* add local commit-msg hook for Conventional Commits ([#22](https://github.com/dreamingdog0529/DisplayForge/issues/22)) ([c0e1fe6](https://github.com/dreamingdog0529/DisplayForge/commit/c0e1fe608273440bac703d8f99c74cc23316f0f0))
* align repo with oss-project-template automation ([#26](https://github.com/dreamingdog0529/DisplayForge/issues/26)) ([c24b59b](https://github.com/dreamingdog0529/DisplayForge/commit/c24b59b861a810cc20991dfa5b87075d56fb9f5d))
* skip merge commits in Conventional Commits checks ([#25](https://github.com/dreamingdog0529/DisplayForge/issues/25)) ([86840df](https://github.com/dreamingdog0529/DisplayForge/commit/86840dfd22cb5c8556335d2b8cdd5be9c7781bdf))

## [0.2.0](https://github.com/dreamingdog0529/DisplayForge/compare/v0.1.2...v0.2.0) (2026-07-18)


### Features

* launch app after install and document window-on-start default ([#21](https://github.com/dreamingdog0529/DisplayForge/issues/21)) ([015d3e4](https://github.com/dreamingdog0529/DisplayForge/commit/015d3e437d503a3a26ba98a554a4b2411dd7ca98))
* ship Setup.exe bootstrapper that installs .NET 10 Desktop Runtime ([#19](https://github.com/dreamingdog0529/DisplayForge/issues/19)) ([d8576ab](https://github.com/dreamingdog0529/DisplayForge/commit/d8576ab7a2eae4e2e1b0d2e0f82149c7f3727cf6))

## [0.1.2](https://github.com/dreamingdog0529/DisplayForge/compare/v0.1.1...v0.1.2) (2026-07-18)


### Bug Fixes

* update system tray menu icons ([#17](https://github.com/dreamingdog0529/DisplayForge/issues/17)) ([03dc0b8](https://github.com/dreamingdog0529/DisplayForge/commit/03dc0b839a6af92d70c77e10d10d0fd37e272923))

## [0.1.1](https://github.com/dreamingdog0529/DisplayForge/compare/v0.1.0...v0.1.1) (2026-07-18)


### Bug Fixes

* build en-US and ja-JP MSIs for GitHub Releases ([#12](https://github.com/dreamingdog0529/DisplayForge/issues/12)) ([ed84894](https://github.com/dreamingdog0529/DisplayForge/commit/ed848946762029bcb03ad2d730bfb3d3b1893be1))

## [0.1.0] - 2026-07-18

### Added

- Multi-monitor profile save / apply / rename / duplicate / delete
- Per-profile global hotkeys
- System tray residency and context menu apply
- UI localization (30+ languages)
- Self-contained win-x64 MSI installer (WiX), multi-culture builds
- Settings and profiles stored under `%AppData%\DisplayForge\`
- GitHub community health files, CI, and release automation for public distribution
- Bilingual README (English / Japanese)
- Release Please automation: Conventional Commits drive SemVer, CHANGELOG, tags, and MSI attachment on GitHub Releases
- CI enforcement of Conventional Commits for PR titles and commit messages

[0.1.2]: https://github.com/dreamingdog0529/DisplayForge/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/dreamingdog0529/DisplayForge/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/dreamingdog0529/DisplayForge/releases/tag/v0.1.0
