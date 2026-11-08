# Builds iStream's release files. Used locally and by GitHub Actions (.github/workflows/release.yml).
#
#   .\scripts\pack.ps1                     everything, unsigned: dist\iStream-v<ver>-win-x64.zip + dist\iStream-Setup-<ver>.exe
#   .\scripts\pack.ps1 -Stage app          compile out\app\iStream.exe and out\app\uninstall.exe (to be code-signed)
#   .\scripts\pack.ps1 -Stage package      package out\app (signed or not) into the ZIP and the installer
#
# The version comes from AssemblyVersion in src\iStream.cs unless -Version is given.
# Nothing here touches a running copy of iStream.exe.
param(
    [ValidateSet('all', 'app', 'package')][string]$Stage = 'all',
    [string]$Version,
    [string]$AppDir
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "C# compiler not found: $csc" }

if (-not $Version) {
    $m = Select-String -Path src\iStream.cs -Pattern 'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)' | Select-Object -First 1
    if (-not $m) { throw 'AssemblyVersion not found in src\iStream.cs' }
    $Version = '{0}.{1}.{2}' -f $m.Matches[0].Groups[1].Value, $m.Matches[0].Groups[2].Value, $m.Matches[0].Groups[3].Value
}
if (-not $AppDir) { $AppDir = Join-Path $root 'out\app' }
$AppDir = [IO.Path]::GetFullPath($AppDir)
Write-Host "iStream $Version - stage: $Stage"

function Invoke-Csc([string[]]$Arguments) {
    & $csc @Arguments | Where-Object { $_ -notmatch 'warning CS0649' }
    if ($LASTEXITCODE) { throw "csc failed: $($Arguments -join ' ')" }
}

$common = @('-nologo', '-platform:x64', '-target:winexe', '-optimize+', '-win32icon:build\iStream.ico', '-win32manifest:src\app.manifest',
            '-r:System.dll', '-r:System.Core.dll', '-r:System.Drawing.dll', '-r:System.Windows.Forms.dll', '-r:Microsoft.CSharp.dll')

if ($Stage -in 'all', 'app') {
    New-Item -ItemType Directory -Force build, $AppDir | Out-Null
    Invoke-Csc @('-nologo', '-platform:x64', '-out:build\mkicon.exe', '-r:System.Drawing.dll', 'src\IconArt.cs', 'src\MkIcon.cs')
    & .\build\mkicon.exe build\iStream.ico
    Invoke-Csc ($common + @('-r:System.Web.Extensions.dll', "-out:$AppDir\iStream.exe",
                            'src\iStream.cs', 'src\Controls.cs', 'src\Strings.cs', 'src\IconArt.cs', 'src\Updater.cs'))
    # the uninstaller is the setup code without a payload
    Invoke-Csc ($common + @('-r:System.IO.Compression.dll', "-out:$AppDir\uninstall.exe", 'src\Setup.cs', 'src\Controls.cs', 'src\IconArt.cs'))
    Write-Host "App built in $AppDir"
}

if ($Stage -in 'all', 'package') {
    foreach ($f in 'iStream.exe', 'uninstall.exe') { if (-not (Test-Path (Join-Path $AppDir $f))) { throw "Missing $AppDir\$f (run -Stage app first)" } }
    if (-not (Test-Path 'engine\uxplay-windows.exe')) { throw 'engine\ is missing: run scripts\get-engine.ps1 first' }
    if (-not (Test-Path 'build\iStream.ico')) { throw 'build\iStream.ico is missing: run -Stage app first' }
    New-Item -ItemType Directory -Force dist | Out-Null

    $stageDir = Join-Path $root 'dist\iStream'
    if (Test-Path $stageDir) { Remove-Item -LiteralPath $stageDir -Recurse -Force }
    New-Item -ItemType Directory $stageDir | Out-Null
    Copy-Item (Join-Path $AppDir 'iStream.exe') $stageDir
    Copy-Item engine (Join-Path $stageDir 'engine') -Recurse
    Copy-Item LICENSE, THIRD_PARTY_NOTICES.md $stageDir
    Copy-Item scripts\README.txt (Join-Path $stageDir 'README.txt')
    Get-ChildItem $stageDir -Recurse -Include *.log | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

    $zip = Join-Path $root "dist\iStream-v$Version-win-x64.zip"
    if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }
    Compress-Archive -Path $stageDir -DestinationPath $zip -CompressionLevel Optimal

    $setup = Join-Path $root "dist\iStream-Setup-$Version.exe"
    Invoke-Csc ($common + @('-r:System.IO.Compression.dll', "-resource:$zip,payload.zip", "-resource:$AppDir\uninstall.exe,uninstall.exe",
                            "-out:$setup", 'src\Setup.cs', 'src\Controls.cs', 'src\IconArt.cs'))

    Get-Item $zip, $setup | ForEach-Object {
        '{0}  {1:N1} MB  {2}' -f $_.Name, ($_.Length / 1MB), (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    }
}
