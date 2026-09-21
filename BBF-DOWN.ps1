#Requires -Version 5.1
<#
.SYNOPSIS
  Para API, Worker e Next.js locais. Nao para o Postgres Docker (bbf-db / volume).
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$Root = $PSScriptRoot
$RunDir = Join-Path $Root ".bbf-run"

function Stop-ListenPort([int]$Port) {
    $conns = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    foreach ($c in $conns) {
        $procId = [int]$c.OwningProcess
        if ($procId -gt 4) {
            Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        }
    }
}

function Stop-PidFile([string]$Name) {
    $file = Join-Path $RunDir $Name
    if (-not (Test-Path -LiteralPath $file)) { return }
    $raw = (Get-Content -LiteralPath $file -ErrorAction SilentlyContinue | Select-Object -First 1)
    $procId = 0
    if ([int]::TryParse($raw, [ref]$procId) -and $procId -gt 4) {
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue
}

Write-Host "A parar app local (API / Worker / Next). Docker db fica."

foreach ($name in @("bbf-api", "bbf-worker", "bbf-web")) {
    docker stop $name 2>$null | Out-Null
}

Push-Location $Root
try {
    docker compose stop api worker web 2>$null | Out-Null
} finally {
    Pop-Location
}

Stop-PidFile "web.pid"
Stop-PidFile "worker.pid"
Stop-PidFile "api.pid"

Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -and (
            $_.CommandLine -match "BbfFirmasPoderes\.(Api|Worker)" -or
            ($_.CommandLine -match "next" -and $_.CommandLine -match "dev")
        )
    } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

Stop-ListenPort 8080
Stop-ListenPort 8081
Stop-ListenPort 3000

Write-Host "App local parada. bbf-db (5432) nao foi tocado."
