[CmdletBinding()]
param(
    [string]$EnvFile = "infra\registry.local.env",
    [string]$Tag,
    [switch]$Push
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

if (-not (Test-Path $EnvFile)) {
    throw "Env file '$EnvFile' was not found. Copy 'infra\registry.local.env.example' to 'infra\registry.local.env' and fill in local values."
}

function Import-EnvFile {
    param([string]$Path)

    $values = @{}

    foreach ($line in Get-Content -Path $Path) {
        $trimmed = $line.Trim()

        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
            continue
        }

        $parts = $trimmed -split "=", 2

        if ($parts.Count -ne 2) {
            continue
        }

        $values[$parts[0]] = $parts[1]
    }

    return $values
}

$registryConfig = Import-EnvFile -Path $EnvFile

if ($Tag) {
    $env:CONTROLLER_TAG = $Tag
}

$registryHost = if ($registryConfig.ContainsKey("REGISTRY_HOST") -and $registryConfig["REGISTRY_HOST"]) {
    $registryConfig["REGISTRY_HOST"]
}
else {
    "registry.monge.place"
}

docker compose -f infra\docker-compose.yml --env-file $EnvFile --profile app build app
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$imageName = if ($env:CONTROLLER_IMAGE) {
    $env:CONTROLLER_IMAGE
}
elseif ($registryConfig.ContainsKey("CONTROLLER_IMAGE") -and $registryConfig["CONTROLLER_IMAGE"]) {
    $registryConfig["CONTROLLER_IMAGE"]
}
else {
    "$registryHost/watering-controller"
}

$resolvedTag = if ($env:CONTROLLER_TAG) { $env:CONTROLLER_TAG } else { "latest" }
$fullImageName = "${imageName}:$resolvedTag"

Write-Host "Built $fullImageName"

if (-not $Push) {
    return
}

if (-not $registryConfig.ContainsKey("REGISTRY_USERNAME") -or [string]::IsNullOrWhiteSpace($registryConfig["REGISTRY_USERNAME"])) {
    throw "REGISTRY_USERNAME is required in '$EnvFile' when using -Push."
}

if (-not $registryConfig.ContainsKey("REGISTRY_PASSWORD") -or [string]::IsNullOrWhiteSpace($registryConfig["REGISTRY_PASSWORD"])) {
    throw "REGISTRY_PASSWORD is required in '$EnvFile' when using -Push."
}

$registryConfig["REGISTRY_PASSWORD"] | docker login $registryHost --username $registryConfig["REGISTRY_USERNAME"] --password-stdin
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

docker compose -f infra\docker-compose.yml --env-file $EnvFile --profile app push app
exit $LASTEXITCODE
