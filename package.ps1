#Requires -Version 5
<#
.SYNOPSIS
    Build The Fiend and assemble a Thunderstore-format zip in ./dist for sharing or upload.
.DESCRIPTION
    Produces dist/<name>-<version>.zip (name + version read from thunderstore/manifest.json),
    laid out for Thunderstore / r2modman: manifest.json, icon.png, README.md, CHANGELOG.md,
    TheFiend.dll, and the thefiend AssetBundle at the zip root.
.EXAMPLE
    ./package.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$manifestPath = Join-Path $root "thunderstore\manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$name    = $manifest.name
$version = $manifest.version_number

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

$dist    = Join-Path $root "dist"
$staging = Join-Path $dist "_staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

# Thunderstore package contents (flat at the zip root).
Copy-Item $manifestPath                          (Join-Path $staging "manifest.json") -Force
Copy-Item (Join-Path $root "thunderstore\icon.png")   (Join-Path $staging "icon.png")   -Force
Copy-Item (Join-Path $root "thunderstore\README.md")  (Join-Path $staging "README.md")  -Force
Copy-Item (Join-Path $root "CHANGELOG.md")            (Join-Path $staging "CHANGELOG.md") -Force
Copy-Item $dll    (Join-Path $staging "TheFiend.dll") -Force
Copy-Item $bundle (Join-Path $staging "thefiend")     -Force

$zip = Join-Path $dist "$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -Force
Remove-Item $staging -Recurse -Force

Write-Host "Packaged: $zip" -ForegroundColor Green
