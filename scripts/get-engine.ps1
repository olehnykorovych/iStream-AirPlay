# Downloads the UxPlay engine (uxplay-windows by leapbtw, GPL-3.0) into .\engine and verifies its checksum.
param(
    [string]$Version = '2.0.0.1736',
    [string]$Sha256 = '9D3A51C15FC9DB857351195E7EB7BBB21700D9AE25D936A54BCF8536B62CCA18'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$url = "https://github.com/leapbtw/uxplay-windows/releases/download/$Version/uxplay-windows.zip"
$tmp = Join-Path ([IO.Path]::GetTempPath()) ("istream-engine-" + [guid]::NewGuid())
New-Item -ItemType Directory $tmp | Out-Null
try {
    $zip = Join-Path $tmp 'uxplay-windows.zip'
    Write-Host "Downloading $url"
    Invoke-WebRequest $url -OutFile $zip
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash
    if ($hash -ne $Sha256) { throw "Checksum mismatch: got $hash, expected $Sha256" }
    $dest = Join-Path $root 'engine'
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    Expand-Archive $zip $dest
    Write-Host "Engine ready in $dest"
}
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
