param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('pump', 'level')]
    [string]$Target,

    [ValidateSet('usb', 'ota')]
    [string]$Mode = 'usb',

    [string]$Port,

    [string]$Env = 'esp32-s3'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$firmwareRoot = Join-Path $root 'infra\firmware'

$projectPath = switch ($Target) {
    'pump' { Join-Path $firmwareRoot 'pump-esp32' }
    'level' { Join-Path $firmwareRoot 'level-esp32' }
}

$platformioIni = Join-Path $projectPath 'platformio.ini'
if (-not (Test-Path $platformioIni)) {
    throw "platformio.ini not found at $platformioIni"
}

Write-Host "Building firmware: target=$Target env=$Env" -ForegroundColor Cyan
& pio run -e $Env -d $projectPath

if ($Mode -eq 'usb') {
    Write-Host "Uploading via USB..." -ForegroundColor Cyan
    if ([string]::IsNullOrWhiteSpace($Port)) {
        & pio run -e $Env -d $projectPath -t upload
    }
    else {
        & pio run -e $Env -d $projectPath -t upload --upload-port $Port
    }
}
else {
    if ([string]::IsNullOrWhiteSpace($Port)) {
        throw "OTA mode requires -Port <ip-or-hostname>."
    }

    Write-Host "Uploading via OTA..." -ForegroundColor Cyan
    & pio run -e $Env -d $projectPath -t upload --upload-port $Port --upload-protocol espota
}
