#Requires -Version 5.1
<#
.SYNOPSIS
  Sobe API, Worker e Next.js na maquina. Postgres permanece no Docker (bbf-db).
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$RunDir = Join-Path $Root ".bbf-run"
$ComposeDev = Join-Path $Root "compose.dev.yaml"
$EnvFile = Join-Path $Root ".env"

function Import-DotEnv([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    Get-Content -LiteralPath $Path -Encoding UTF8 | ForEach-Object {
        $line = $_.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) { return }
        $eq = $line.IndexOf("=")
        if ($eq -lt 1) { return }
        $name = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim().Trim('"').Trim("'")
        if ($name.Length -gt 0) {
            Set-Item -LiteralPath "Env:$name" -Value $value
        }
    }
}

function Stop-ListenPort([int]$Port) {
    $conns = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    foreach ($c in $conns) {
        $procId = [int]$c.OwningProcess
        if ($procId -gt 4) {
            Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        }
    }
}

function Stop-DockerApp {
    foreach ($name in @("bbf-api", "bbf-worker", "bbf-web")) {
        docker stop $name 2>$null | Out-Null
    }
    Push-Location $Root
    try {
        docker compose stop api worker web 2>$null | Out-Null
    } finally {
        Pop-Location
    }
}

function Wait-Http([string]$Url, [int]$Seconds) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { return }
        } catch { Start-Sleep -Seconds 1 }
    }
    throw "Timeout a espera de $Url"
}

Stop-DockerApp
Stop-ListenPort 8080
Stop-ListenPort 8081
Stop-ListenPort 3000
Start-Sleep -Seconds 1

if (-not (Test-Path -LiteralPath $ComposeDev)) {
    throw "Falta $ComposeDev"
}

Write-Host "Postgres Docker (bbf-db)..."
Push-Location $Root
try {
    docker compose -f compose.dev.yaml up -d
} finally {
    Pop-Location
}

$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    docker exec bbf-db pg_isready -U bbf -d bbf_firmas 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 1
}
if (-not $ready) { throw "bbf-db nao ficou ready. Suba o engine Docker e tente de novo." }

Import-DotEnv $EnvFile
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
$env:Database__MigrateOnStartup = "true"
$env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=bbf_firmas;Username=bbf;Password=bbf"
$env:Documents__StorageRoot = Join-Path $Root "data\docs"
if (-not $env:Kas__TimeoutSeconds) { $env:Kas__TimeoutSeconds = "600" }
if (-not $env:Kas__MultipartFileField) { $env:Kas__MultipartFileField = "document_url" }
if (-not $env:Kas__RunUrl) {
    $env:Kas__RunUrl = "https://kaas-core-dev.up.railway.app/kas/triggers/journeys/testes-firmas-e-poderes/run"
}
$env:NEXT_PUBLIC_API_URL = "http://localhost:8080"
$env:ASPNETCORE_URLS = ""

New-Item -ItemType Directory -Force -Path $RunDir | Out-Null
New-Item -ItemType Directory -Force -Path $env:Documents__StorageRoot | Out-Null

Write-Host "dotnet build..."
dotnet build (Join-Path $Root "backend\src\BbfFirmasPoderes.Api") --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Falha build API" }
dotnet build (Join-Path $Root "backend\src\BbfFirmasPoderes.Worker") --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Falha build Worker" }

$api = Start-Process -FilePath "dotnet" -WorkingDirectory $Root -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $RunDir "api.out.log") `
    -RedirectStandardError (Join-Path $RunDir "api.err.log") `
    -ArgumentList @(
        "run", "--no-build",
        "--project", "backend\src\BbfFirmasPoderes.Api",
        "--urls", "http://localhost:8080"
    )
$api.Id | Set-Content -LiteralPath (Join-Path $RunDir "api.pid") -Encoding ASCII

$worker = Start-Process -FilePath "dotnet" -WorkingDirectory $Root -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $RunDir "worker.out.log") `
    -RedirectStandardError (Join-Path $RunDir "worker.err.log") `
    -ArgumentList @(
        "run", "--no-build",
        "--project", "backend\src\BbfFirmasPoderes.Worker",
        "--urls", "http://localhost:8081"
    )
$worker.Id | Set-Content -LiteralPath (Join-Path $RunDir "worker.pid") -Encoding ASCII

$npm = if (Test-Path "$env:ProgramFiles\nodejs\npm.cmd") { "$env:ProgramFiles\nodejs\npm.cmd" } else { "npm.cmd" }
$web = Start-Process -FilePath $npm -WorkingDirectory $Root -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $RunDir "web.out.log") `
    -RedirectStandardError (Join-Path $RunDir "web.err.log") `
    -ArgumentList @("run", "dev", "--", "-p", "3000")
$web.Id | Set-Content -LiteralPath (Join-Path $RunDir "web.pid") -Encoding ASCII

Write-Host "A espera de health..."
Wait-Http "http://localhost:8080/health/live" 60
Wait-Http "http://localhost:8081/health/live" 60
Wait-Http "http://localhost:3000" 90

Write-Host "BBF local no ar (Postgres continua no Docker bbf-db)."
Write-Host "  UI    http://localhost:3000"
Write-Host "  API   http://localhost:8080"
Write-Host "  Worker http://localhost:8081"
Write-Host "Logs: $RunDir"
