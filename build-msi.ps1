#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Publish DisplayForge (self-contained win-x64) and build multi-language MSI installers.

.PARAMETER Configuration
  Build configuration. Default: Release

.PARAMETER Runtime
  RID for publish. Default: win-x64

.PARAMETER Version
  Product version for the MSI (must be N.N.N or N.N.N.N). Default: from csproj Version, or 1.0.0

.PARAMETER SkipPublish
  Skip dotnet publish and reuse existing artifacts/publish output.

.PARAMETER FrameworkDependent
  Publish framework-dependent (requires .NET 10 Desktop Runtime on target machines).
  Default is self-contained.

.PARAMETER Cultures
  Semicolon-delimited WiX culture list (e.g. "ja-JP;en-US").
  Default: all cultures that have Loc/*.wxl files.
  Use a subset for faster local builds.

.EXAMPLE
  .\build-msi.ps1
  .\build-msi.ps1 -Version 1.2.0
  .\build-msi.ps1 -Cultures "ja-JP;en-US"
  .\build-msi.ps1 -SkipPublish
#>
[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64",
    [string] $Version = "",
    [switch] $SkipPublish,
    [switch] $FrameworkDependent,
    [string] $Cultures = ""
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$AppProject = Join-Path $Root "src\DisplayForge\DisplayForge.csproj"
$InstallerProject = Join-Path $Root "installer\DisplayForge.Installer\DisplayForge.Installer.wixproj"
$LocDir = Join-Path $Root "installer\DisplayForge.Installer\Loc"
$PublishDir = Join-Path $Root "artifacts\publish\$Runtime"
$MsiOutDir = Join-Path $Root "artifacts\msi"

function Get-AppVersion {
    if ($Version) { return $Version }
    [xml] $proj = Get-Content -LiteralPath $AppProject
    $v = $proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if ($v) { return $v.Trim() }
    return "1.0.0"
}

function Get-AvailableCultures {
    Get-ChildItem -LiteralPath $LocDir -Filter "*.wxl" -File |
        ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) } |
        Sort-Object
}

$ProductVersion = Get-AppVersion
# WiX / MSI ProductVersion is up to 4 parts of 16-bit integers.
if ($ProductVersion -notmatch '^\d+(\.\d+){1,3}$') {
    throw "Version '$ProductVersion' is not a valid MSI ProductVersion (use N.N.N or N.N.N.N)."
}

$available = @(Get-AvailableCultures)
if ($available.Count -eq 0) {
    throw "No Loc/*.wxl files found under $LocDir. Run gen-wxl.ps1 first."
}

if ([string]::IsNullOrWhiteSpace($Cultures)) {
    $cultureList = $available
}
else {
    $cultureList = @(
        $Cultures -split '[;,\s]+' |
            Where-Object { $_ } |
            ForEach-Object { $_.Trim() }
    )
    foreach ($c in $cultureList) {
        if ($c -notin $available) {
            throw "Unknown culture '$c'. Available: $($available -join ', ')"
        }
    }
}

# WiX Cultures property: semicolon-delimited culture groups
$culturesProperty = ($cultureList -join ';')

Write-Host "==> DisplayForge MSI build" -ForegroundColor Cyan
Write-Host "    Version:       $ProductVersion"
Write-Host "    Configuration: $Configuration"
Write-Host "    Runtime:       $Runtime"
Write-Host "    SelfContained: $(-not $FrameworkDependent)"
Write-Host "    Cultures:      $($cultureList -join ', ') ($($cultureList.Count))"
Write-Host "    PublishDir:    $PublishDir"

if (-not $SkipPublish) {
    if (Test-Path -LiteralPath $PublishDir) {
        Remove-Item -LiteralPath $PublishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

    $publishArgs = @(
        "publish", $AppProject,
        "-c", $Configuration,
        "-r", $Runtime,
        "-o", $PublishDir,
        "--self-contained", ($(if ($FrameworkDependent) { "false" } else { "true" })),
        "-p:PublishSingleFile=false",
        "-p:DebugType=none",
        "-p:DebugSymbols=false",
        "-p:Version=$ProductVersion"
    )

    Write-Host "==> dotnet publish" -ForegroundColor Cyan
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
}
else {
    if (-not (Test-Path -LiteralPath (Join-Path $PublishDir "DisplayForge.exe"))) {
        throw "SkipPublish set but DisplayForge.exe not found under $PublishDir"
    }
    Write-Host "==> Skipping publish (reusing $PublishDir)" -ForegroundColor Yellow
}

if (Test-Path -LiteralPath $MsiOutDir) {
    # Remove previous MSIs only; keep directory
    Get-ChildItem -LiteralPath $MsiOutDir -Filter "*.msi" -File -Recurse | Remove-Item -Force
}
New-Item -ItemType Directory -Path $MsiOutDir -Force | Out-Null

$objDir = Join-Path $Root "artifacts\obj\installer"
Write-Host "==> Building MSI (WiX, multi-culture)" -ForegroundColor Cyan
$buildArgs = @(
    "build", $InstallerProject,
    "-c", $Configuration,
    "-p:PublishDir=$PublishDir\",
    "-p:ProductVersion=$ProductVersion",
    "-p:OutputPath=$MsiOutDir\",
    "-p:BaseIntermediateOutputPath=$objDir\"
)
# When building a subset, pass Cultures. MSBuild splits on ';', so encode as %3B.
# When building all Loc/*.wxl cultures, omit Cultures and let WiX discover them.
$buildingAll = ($cultureList.Count -eq $available.Count) -and
    (@(Compare-Object $cultureList $available).Count -eq 0)
if (-not $buildingAll) {
    $buildArgs += "-p:Cultures=$(($cultureList -join '%3B'))"
}
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "MSI build failed with exit code $LASTEXITCODE" }

# Collect MSIs: WiX writes culture subfolders when multiple cultures are built
$produced = @()
foreach ($c in $cultureList) {
    $candidates = @(
        (Join-Path $MsiOutDir "$c\DisplayForge.msi"),
        (Join-Path $MsiOutDir "DisplayForge.msi")
    )
    $src = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $src) {
        # Fallback: search
        $src = Get-ChildItem -LiteralPath $MsiOutDir -Filter "DisplayForge.msi" -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Directory.Name -eq $c -or $cultureList.Count -eq 1 } |
            Select-Object -ExpandProperty FullName -First 1
    }
    if (-not $src) {
        Write-Warning "MSI for culture $c not found under $MsiOutDir"
        continue
    }
    $named = Join-Path $MsiOutDir "DisplayForge-$ProductVersion-$Runtime-$c.msi"
    Copy-Item -LiteralPath $src -Destination $named -Force
    $produced += $named
}

if ($produced.Count -eq 0) {
    throw "No MSI was produced under $MsiOutDir"
}

Write-Host ""
Write-Host "MSI ready ($($produced.Count)):" -ForegroundColor Green
foreach ($p in $produced) {
    Write-Host "  $p"
}
Write-Host ""
Write-Host "Install (elevated):  msiexec /i `"$($produced[0])`""
Write-Host "Quiet install:       msiexec /i `"$($produced[0])`" /qn"
Write-Host "Uninstall:           msiexec /x `"$($produced[0])`""
Write-Host ""
Write-Host "Tip: faster rebuild for one language:  .\build-msi.ps1 -SkipPublish -Cultures ja-JP"
