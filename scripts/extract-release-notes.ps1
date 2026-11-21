<#
.SYNOPSIS
    Extracts release notes for a specific version from CHANGELOG.md.
.PARAMETER Version
    The version string (e.g. "1.3.0" or "v1.3.0"). If omitted, reads the latest version from CHANGELOG.md.
.PARAMETER ChangelogPath
    Path to CHANGELOG.md (default: CHANGELOG.md).
.PARAMETER OutputPath
    Optional path to write the extracted notes.
#>

param(
    [string]$Version,
    [string]$ChangelogPath = "CHANGELOG.md",
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ChangelogPath)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $ChangelogPath = Join-Path $scriptDir "..\CHANGELOG.md"
}

if (-not (Test-Path $ChangelogPath)) {
    throw "CHANGELOG.md not found at '$ChangelogPath'"
}

$content = Get-Content -Path $ChangelogPath -Raw

# Clean version string (remove leading 'v' if present)
if ($Version) {
    $cleanVersion = $Version.TrimStart('v', 'V')
} else {
    # Extract the topmost version from ## [X.Y.Z]
    if ($content -match '##\s+\[(?<ver>[\d\.]+(?:-[\w\.]+)?)\]') {
        $cleanVersion = $Matches['ver']
    } else {
        throw "Could not determine latest version from CHANGELOG.md"
    }
}

# Regex to find the section for this version:
# Matches from '## [X.Y.Z]' up to the next '## [' or EOF
$escapedVer = [regex]::Escape($cleanVersion)
$pattern = "(?s)##\s+\[$escapedVer\][^\r\n]*\r?\n(.*?)(?=\r?\n##\s+\[|\z)"

if ($content -match $pattern) {
    $notes = $Matches[1].Trim()
} else {
    $notes = "Release version $cleanVersion`n`nPlease see CHANGELOG.md for details."
}

# Add release metadata / summary header
$finalNotes = @"
## ChargeGate v$cleanVersion

$notes

---
### Downloads & Verification
- **Installer (.exe):** `ChargeGate-Setup-v$cleanVersion.exe` (Recommended)
- **Standalone Portable:** `ChargeGate.exe` (Single-file, self-contained)
- **Sponsor & Support:** [ko-fi.com/softwarebrotherhood](https://ko-fi.com/softwarebrotherhood)
"@

if ($OutputPath) {
    [System.IO.File]::WriteAllText($OutputPath, $finalNotes, [System.Text.Encoding]::UTF8)
    Write-Host "Release notes written to $OutputPath for version $cleanVersion"
}

Write-Output $finalNotes
