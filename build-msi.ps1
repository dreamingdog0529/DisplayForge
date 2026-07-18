#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Publish DisplayForge (framework-dependent win-x64 by default) and build MSI + Setup bootstrapper.

.PARAMETER Configuration
  Build configuration. Default: Release

.PARAMETER Runtime
  RID for publish. Default: win-x64

.PARAMETER Version
  Product version for the MSI/bundle (must be N.N.N or N.N.N.N). Default: from csproj Version, or 1.0.0

.PARAMETER SkipPublish
  Skip dotnet publish and reuse existing artifacts/publish output.

.PARAMETER SelfContained
  Publish self-contained (embeds .NET runtime in the app folder). Skips the bootstrapper
  and .NET Desktop Runtime prerequisite download. Default is framework-dependent + Setup.exe.

.PARAMETER SkipBundle
  Build MSI only (no Setup.exe). Framework-dependent MSIs still require .NET 10 Desktop Runtime.

.PARAMETER DotNetDesktopRuntimeVersion
  Version of the Windows Desktop Runtime redistributable to download and embed in Setup.exe.
  Default: 10.0.10

.PARAMETER Cultures
  Semicolon-delimited WiX culture list (e.g. "ja-JP;en-US").
  Default: all cultures that have Loc/*.wxl files.
  Use a subset for faster local builds.

.EXAMPLE
  .\build-msi.ps1
  .\build-msi.ps1 -Version 1.2.0
  .\build-msi.ps1 -Cultures "ja-JP;en-US"
  .\build-msi.ps1 -SkipPublish
  .\build-msi.ps1 -SelfContained
#>
[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64",
    [string] $Version = "",
    [switch] $SkipPublish,
    [switch] $SelfContained,
    [switch] $SkipBundle,
    [string] $DotNetDesktopRuntimeVersion = "10.0.10",
    [string] $Cultures = ""
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$AppProject = Join-Path $Root "src\DisplayForge\DisplayForge.csproj"
$InstallerProject = Join-Path $Root "installer\DisplayForge.Installer\DisplayForge.Installer.wixproj"
$BootstrapperProject = Join-Path $Root "installer\DisplayForge.Bootstrapper\DisplayForge.Bootstrapper.wixproj"
$LocDir = Join-Path $Root "installer\DisplayForge.Installer\Loc"
$PublishDir = Join-Path $Root "artifacts\publish\$Runtime"
$MsiOutDir = Join-Path $Root "artifacts\msi"
$PrereqDir = Join-Path $Root "artifacts\prereqs"
$FrameworkDependent = -not $SelfContained

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

function Get-DotNetDesktopRuntime {
    param(
        [string] $Version,
        [string] $OutDir
    )

    if ($Version -notmatch '^\d+\.\d+\.\d+$') {
        throw "DotNetDesktopRuntimeVersion '$Version' must be N.N.N (e.g. 10.0.10)."
    }

    $fileName = "windowsdesktop-runtime-$Version-win-x64.exe"
    $dest = Join-Path $OutDir $fileName
    if (Test-Path -LiteralPath $dest) {
        Write-Host "    Reusing $dest" -ForegroundColor DarkGray
        return @{ Path = $dest; FileName = $fileName }
    }

    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
    $url = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/$Version/$fileName"
    Write-Host "    Downloading $url" -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
    }
    catch {
        if (Test-Path -LiteralPath $dest) { Remove-Item -LiteralPath $dest -Force }
        throw "Failed to download .NET Desktop Runtime $Version from $url. $($_.Exception.Message)"
    }

    if (-not (Test-Path -LiteralPath $dest) -or ((Get-Item -LiteralPath $dest).Length -lt 1MB)) {
        throw "Downloaded runtime looks invalid: $dest"
    }

    return @{ Path = $dest; FileName = $fileName }
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

$buildBundle = $FrameworkDependent -and -not $SkipBundle

Write-Host "==> DisplayForge installer build" -ForegroundColor Cyan
Write-Host "    Version:       $ProductVersion"
Write-Host "    Configuration: $Configuration"
Write-Host "    Runtime:       $Runtime"
Write-Host "    SelfContained: $SelfContained"
Write-Host "    Bundle:        $buildBundle"
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
        "--self-contained", ($(if ($SelfContained) { "true" } else { "false" })),
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

$runtimePayload = $null
if ($buildBundle) {
    Write-Host "==> .NET 10 Desktop Runtime redistributable ($DotNetDesktopRuntimeVersion)" -ForegroundColor Cyan
    $runtimePayload = Get-DotNetDesktopRuntime -Version $DotNetDesktopRuntimeVersion -OutDir $PrereqDir
}

if (Test-Path -LiteralPath $MsiOutDir) {
    Get-ChildItem -LiteralPath $MsiOutDir -Include "*.msi", "*.exe", "*.wixpdb" -File -Recurse |
        Remove-Item -Force -ErrorAction SilentlyContinue
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
if ($FrameworkDependent) {
    # Enables Package.wxs <?ifdef FrameworkDependent ?> runtime Launch check (appended in wixproj).
    $buildArgs += "-p:FrameworkDependent=true"
}
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
$producedMsi = @()
foreach ($c in $cultureList) {
    $candidates = @(
        (Join-Path $MsiOutDir "$c\DisplayForge.msi"),
        (Join-Path $MsiOutDir "DisplayForge.msi")
    )
    $src = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $src) {
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
    $producedMsi += [pscustomobject]@{ Culture = $c; Path = $named; Source = $src }
}

if ($producedMsi.Count -eq 0) {
    throw "No MSI was produced under $MsiOutDir"
}

$producedSetup = @()
if ($buildBundle) {
    $bundleObjDir = Join-Path $Root "artifacts\obj\bootstrapper"
    $bundleOutDir = Join-Path $Root "artifacts\obj\bootstrapper\out"
    $runtimeDir = Split-Path -Parent $runtimePayload.Path
    if (-not $runtimeDir.EndsWith('\') -and -not $runtimeDir.EndsWith('/')) {
        $runtimeDir = "$runtimeDir\"
    }

    Write-Host "==> Building Setup bootstrapper (WiX Bundle)" -ForegroundColor Cyan
    foreach ($item in $producedMsi) {
        $msiDir = Split-Path -Parent $item.Source
        if (-not $msiDir.EndsWith('\') -and -not $msiDir.EndsWith('/')) {
            $msiDir = "$msiDir\"
        }
        $msiFileName = [System.IO.Path]::GetFileName($item.Source)

        if (Test-Path -LiteralPath $bundleOutDir) {
            Remove-Item -LiteralPath $bundleOutDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $bundleOutDir -Force | Out-Null

        $bundleArgs = @(
            "build", $BootstrapperProject,
            "-c", $Configuration,
            "-p:ProductVersion=$ProductVersion",
            "-p:MsiDir=$msiDir",
            "-p:MsiFileName=$msiFileName",
            "-p:DotNetRuntimeDir=$runtimeDir",
            "-p:DotNetRuntimeFileName=$($runtimePayload.FileName)",
            "-p:OutputPath=$bundleOutDir\",
            "-p:BaseIntermediateOutputPath=$bundleObjDir\$($item.Culture)\"
        )
        & dotnet @bundleArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Bootstrapper build failed for culture $($item.Culture) with exit code $LASTEXITCODE"
        }

        $setupSrc = Get-ChildItem -LiteralPath $bundleOutDir -Filter "DisplayForge-Setup.exe" -Recurse -File |
            Select-Object -First 1
        if (-not $setupSrc) {
            $setupSrc = Get-ChildItem -LiteralPath $bundleOutDir -Filter "*.exe" -Recurse -File |
                Where-Object { $_.Name -notlike "*wix*" } |
                Select-Object -First 1
        }
        if (-not $setupSrc) {
            throw "Setup.exe not found under $bundleOutDir for culture $($item.Culture)"
        }

        $setupNamed = Join-Path $MsiOutDir "DisplayForge-$ProductVersion-$Runtime-$($item.Culture)-Setup.exe"
        Copy-Item -LiteralPath $setupSrc.FullName -Destination $setupNamed -Force
        $producedSetup += $setupNamed
    }
}

Write-Host ""
Write-Host "MSI ready ($($producedMsi.Count)):" -ForegroundColor Green
foreach ($item in $producedMsi) {
    Write-Host "  $($item.Path)"
}
if ($producedSetup.Count -gt 0) {
    Write-Host "Setup ready ($($producedSetup.Count)):" -ForegroundColor Green
    foreach ($p in $producedSetup) {
        Write-Host "  $p"
    }
    Write-Host ""
    Write-Host "Recommended install:  `"$($producedSetup[0])`""
    Write-Host "  (installs .NET 10 Desktop Runtime if missing, then DisplayForge)"
}
else {
    Write-Host ""
    Write-Host "Install (elevated):  msiexec /i `"$($producedMsi[0].Path)`""
    Write-Host "Quiet install:       msiexec /i `"$($producedMsi[0].Path)`" /qn"
}
Write-Host "Uninstall:           msiexec /x `"$($producedMsi[0].Path)`"   (or Apps & features → DisplayForge)"
Write-Host ""
Write-Host "Tip: faster rebuild for one language:  .\build-msi.ps1 -SkipPublish -Cultures ja-JP"
