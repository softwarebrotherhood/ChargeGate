<#
.SYNOPSIS
    Builds the standalone self-contained release and packages a Windows Setup installer (.exe).
.DESCRIPTION
    1. Publishes ChargeGate in Release mode (win-x64 self-contained).
    2. Compiles a Windows Setup Installer (.exe) using Inno Setup.
    3. Outputs both the Setup Installer and Portable single-file binary into /dist.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  ChargeGate - Windows Installer Builder  " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$distDir = Join-Path $scriptDir "dist"
$publishDir = Join-Path $distDir "publish"
$installerDir = Join-Path $distDir "installer"
$portableDir = Join-Path $distDir "portable"

# Clean previous output
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableDir | Out-Null

# 1. Publish Self-Contained .NET 9 Single-File Application
Write-Host "`n[1/3] Publishing Release (win-x64, self-contained)..." -ForegroundColor Yellow
$projectPath = Join-Path $scriptDir "src\ChargeGate.UI\ChargeGate.UI.csproj"

dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit 1
}

# Copy portable binary
$portableExe = Join-Path $portableDir "ChargeGate.exe"
Copy-Item (Join-Path $publishDir "ChargeGate.UI.exe") -Destination $portableExe -Force
Write-Host "  -> Portable executable created: $portableExe" -ForegroundColor Green

# 2. Locate Inno Setup Compiler
Write-Host "`n[2/3] Locating Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow
$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)

$isccPath = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $isccPath) {
    Write-Warning "Inno Setup (ISCC.exe) was not found in standard paths."
    Write-Warning "You can install it with: winget install JRSoftware.InnoSetup"
    Write-Host "`nPortable executable is ready at: $portableExe" -ForegroundColor Cyan
    exit 0
}

Write-Host "  -> Found ISCC at: $isccPath" -ForegroundColor Green

# 3. Compile Setup Installer
Write-Host "`n[3/3] Compiling Setup Installer (.exe)..." -ForegroundColor Yellow
$issScript = Join-Path $scriptDir "installer\ChargeGate.iss"

& $isccPath $issScript

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup compilation failed!"
    exit 1
}

$setupFiles = Get-ChildItem -Path $installerDir -Filter "ChargeGate-Setup-*.exe"
if ($setupFiles) {
    Write-Host "`n==========================================" -ForegroundColor Green
    Write-Host "  SUCCESS: Installer Generated!" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
    foreach ($f in $setupFiles) {
        $sizeMb = [math]::Round($f.Length / 1MB, 2)
        Write-Host "  Installer EXE : $($f.FullName) ($sizeMb MB)" -ForegroundColor White
    }
    Write-Host "  Portable EXE  : $portableExe" -ForegroundColor White
}
