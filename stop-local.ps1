$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$logDir = Join-Path $root "artifacts\dev-logs"
$pidFile = Join-Path $logDir "local-dev.pids.json"

if (-not (Test-Path $pidFile)) {
    Write-Host "No encontre procesos registrados en $pidFile." -ForegroundColor Yellow
    return
}

$pids = Get-Content $pidFile | ConvertFrom-Json
$stopped = 0

foreach ($entry in @($pids.backend, $pids.frontend)) {
    if (-not $entry) {
        continue
    }

    $process = Get-Process -Id $entry -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $entry -Force
        Write-Host "Proceso detenido: PID $entry" -ForegroundColor Green
        $stopped++
    }
}

if ($stopped -eq 0) {
    Write-Host "No habia procesos activos para detener." -ForegroundColor Yellow
}

Remove-Item -Path $pidFile -Force
