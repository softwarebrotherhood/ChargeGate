<#
.SYNOPSIS
    Prepares and automates a new release for ChargeGate.
.DESCRIPTION
    1. Identifies the target release version from CHANGELOG.md (or parameter).
    2. Extracts the adapted release notes for that specific version.
    3. Builds the local installer (.exe) and standalone portable binary.
    4. Generates dist/RELEASE_NOTES_v<version>.md.
    5. Optionally creates and pushes the git tag (v<version>) to trigger automated GitHub Release publishing.
.PARAMETER Version
    The version to release (e.g., "1.3.0" or "v1.3.0"). If omitted, reads the latest version from CHANGELOG.md.
.PARAMETER PushTag
    Switch to automatically push the git tag to origin.
.PARAMETER SkipBuild
    Skip running build-installer.ps1.
#>

[CmdletBinding()]
param(
    [string]$Version,
    [switch]$PushTag,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Get-Item (Join-Path $scriptDir "..")).FullName
Set-Location $rootDir

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  ChargeGate - Automated Release Assistant   " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# 1. Determine Version
$changelogPath = Join-Path $rootDir "CHANGELOG.md"
if (-not (Test-Path $changelogPath)) {
    throw "CHANGELOG.md not found at '$changelogPath'"
}

$changelogContent = Get-Content -Path $changelogPath -Raw

if ($Version) {
    $cleanVersion = $Version.TrimStart('v', 'V')
} else {
    if ($changelogContent -match '##\s+\[(?<ver>[\d\.]+(?:-[\w\.]+)?)\]') {
        $cleanVersion = $Matches['ver']
    } else {
        throw "Could not determine release version from CHANGELOG.md"
    }
}

$tagName = "v$cleanVersion"
Write-Host "`n[1/4] Target Release Version: $cleanVersion (Tag: $tagName)" -ForegroundColor Green

# 2. Extract & Adapt Release Notes
Write-Host "[2/4] Extracting adapted release notes from CHANGELOG.md..." -ForegroundColor Yellow
$notesScript = Join-Path $scriptDir "extract-release-notes.ps1"
$distDir = Join-Path $rootDir "dist"
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir -Force | Out-Null }
$releaseNotesFile = Join-Path $distDir "RELEASE_NOTES_$tagName.md"

$releaseNotes = & $notesScript -Version $cleanVersion -OutputPath $releaseNotesFile

Write-Host "  -> Release notes generated: $releaseNotesFile" -ForegroundColor Green
Write-Host "`n----------------- RELEASE NOTES PREVIEW -----------------" -ForegroundColor Gray
Write-Host $releaseNotes -ForegroundColor White
Write-Host "--------------------------------------------------------`n" -ForegroundColor Gray

# 3. Build Binaries and Installer
if (-not $SkipBuild) {
    Write-Host "[3/4] Building Windows Installer (.exe) and Standalone Portable..." -ForegroundColor Yellow
    $buildScript = Join-Path $rootDir "build-installer.ps1"
    & powershell -ExecutionPolicy Bypass -File $buildScript
    if ($LASTEXITCODE -ne 0) {
        throw "Build installer script failed."
    }
} else {
    Write-Host "[3/4] Skipping build as requested." -ForegroundColor Gray
}

# 4. Git Tagging & Release Trigger
Write-Host "`n[4/4] Git Tagging for Release: $tagName" -ForegroundColor Yellow

$existingTag = git tag -l $tagName
if ($existingTag) {
    Write-Host "  -> Git tag '$tagName' already exists locally." -ForegroundColor Yellow
} else {
    git tag -a $tagName -m "Release $tagName"
    Write-Host "  -> Created local git tag '$tagName'." -ForegroundColor Green
}

if ($PushTag) {
    Write-Host "`nPushing tag '$tagName' to origin..." -ForegroundColor Cyan
    git push origin $tagName
    Write-Host "Tag '$tagName' pushed! GitHub Actions will now build and publish the release." -ForegroundColor Green
} else {
    Write-Host "`nNext Step: To publish the release automatically on GitHub, push the tag:" -ForegroundColor Cyan
    Write-Host "  git push origin $tagName" -ForegroundColor White
}
