# Builds the release files for a version:
#   dist\iStream-v<ver>-win-x64.zip   portable package
#   dist\iStream-Setup-<ver>.exe      installer (via build-setup.cmd)
# Compiles the app straight into the staging folder, so a running copy of iStream.exe is never touched.
param([Parameter(Mandatory = $true)][string]$Version)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path 'engine\uxplay-windows.exe')) { throw 'engine\ is missing: run scripts\get-engine.ps1 first' }

New-Item -ItemType Directory -Force build, dist | Out-Null
& $csc -nologo -platform:x64 -out:build\mkicon.exe -r:System.Drawing.dll src\IconArt.cs src\MkIcon.cs
if ($LASTEXITCODE) { throw 'mkicon build failed' }
& .\build\mkicon.exe build\iStream.ico

$stage = Join-Path $root 'dist\iStream'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory $stage | Out-Null

& $csc -nologo -platform:x64 -target:winexe -optimize+ -win32icon:build\iStream.ico -win32manifest:src\app.manifest `
    -r:System.dll -r:System.Core.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:Microsoft.CSharp.dll `
    "-out:$stage\iStream.exe" src\iStream.cs src\Controls.cs src\Strings.cs src\IconArt.cs
if ($LASTEXITCODE) { throw 'app build failed' }

Copy-Item engine (Join-Path $stage 'engine') -Recurse
Copy-Item LICENSE, THIRD_PARTY_NOTICES.md $stage
Copy-Item scripts\README.txt (Join-Path $stage 'README.txt')
# never ship machine-specific engine leftovers
Get-ChildItem $stage -Recurse -Include *.log | Remove-Item -Force

$zip = Join-Path $root "dist\iStream-v$Version-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stage -DestinationPath $zip -CompressionLevel Optimal

$env:VER = $Version
& (Join-Path $root 'build-setup.cmd')
if ($LASTEXITCODE) { throw 'installer build failed' }

Get-Item $zip, "dist\iStream-Setup-$Version.exe" | ForEach-Object {
    '{0}  {1:N1} MB  {2}' -f $_.Name, ($_.Length / 1MB), (Get-FileHash $_.FullName -Algorithm SHA256).Hash
}
