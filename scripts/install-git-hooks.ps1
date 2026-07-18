#Requires -Version 5.1
<#
.SYNOPSIS
  Point this clone at the shared Git hooks under .githooks/ (Conventional Commits on commit-msg).

.DESCRIPTION
  Sets local (not global) core.hooksPath so commit subjects are checked before the commit is created.
  Same rules as .github/workflows/conventional-commits.yml.

.EXAMPLE
  pwsh ./scripts/install-git-hooks.ps1
#>

$ErrorActionPreference = 'Stop'

$root = git rev-parse --show-toplevel 2>$null
if (-not $root) {
    Write-Error "Not inside a Git repository."
    exit 1
}

$hooksPath = Join-Path $root '.githooks'
if (-not (Test-Path -LiteralPath (Join-Path $hooksPath 'commit-msg'))) {
    Write-Error "Expected hook not found: $hooksPath\commit-msg"
    exit 1
}

Push-Location $root
try {
    git config core.hooksPath .githooks
    $current = git config --get core.hooksPath
    Write-Host "core.hooksPath = $current"
    Write-Host "Local commit-msg hook enabled (Conventional Commits)."
    Write-Host "Example:  git commit -m `"fix: tray icon not restoring window`""
}
finally {
    Pop-Location
}
