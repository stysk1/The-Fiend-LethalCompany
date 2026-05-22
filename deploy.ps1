#Requires -Version 5
<#
.SYNOPSIS
    Build The Fiend and copy the DLL + AssetBundle into a local r2modman/BepInEx profile for testing.
.EXAMPLE
    ./deploy.ps1
.EXAMPLE
    ./deploy.ps1 -ProfilePath "D:\r2\profiles\MyProfile\BepInEx\plugins\Rolevote-The_Fiend"
#>
[CmdletBinding()]
param(
    [string]$ProfilePath = "$env:APPDATA\r2modmanPlus-local\LethalCompany\profiles\FiendTest\BepInEx\plugins\Rolevote-The_Fiend",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "Restoring tools..." -ForegroundColor Cyan
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed." }

Write-Host "Building ($Configuration)..." -ForegroundColor Cyan
dotnet build "$root\TheFiend\TheFiend.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$outDir = Join-Path $root "TheFiend\bin\$Configuration\netstandard2.1"
$dll    = Join-Path $outDir "TheFiend.dll"
$bundle = Join-Path $outDir "thefiend"

if (-not (Test-Path $dll))    { throw "Build output not found: $dll" }
if (-not (Test-Path $bundle)) { throw "AssetBundle not found in output: $bundle" }

if (-not (Test-Path $ProfilePath)) {
    throw "Plugin folder not found: $ProfilePath`nPass the correct path with -ProfilePath."
}

Copy-Item $dll    $ProfilePath -Force
Copy-Item $bundle $ProfilePath -Force

Write-Host "Deployed TheFiend.dll + thefiend to:" -ForegroundColor Green
Write-Host "  $ProfilePath" -ForegroundColor Green
