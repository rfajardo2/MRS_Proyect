$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendProject = Join-Path $root "backend\MRSDrunk.Api\MRSDrunk.Api.csproj"
$frontendDir = Join-Path $root "frontend"
$frontendServer = Join-Path $root "tools\serve-static.ps1"
$logDir = Join-Path $root "artifacts\dev-logs"
$pidFile = Join-Path $logDir "local-dev.pids.json"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Test-Port {
    param([int]$Port)
    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    return $null -ne $connection
}

if (Test-Port 5127) {
    Write-Host "El puerto 5127 ya esta en uso. Si es el backend, puedes seguir usando ese proceso." -ForegroundColor Yellow
} else {
    $backendOut = Join-Path $logDir "backend.out.log"
    $backendErr = Join-Path $logDir "backend.err.log"
    $backend = Start-Process dotnet `
        -ArgumentList "run --no-restore --project `"$backendProject`" --launch-profile http" `
        -WorkingDirectory $root `
        -RedirectStandardOutput $backendOut `
        -RedirectStandardError $backendErr `
        -WindowStyle Hidden `
        -PassThru
    Write-Host "Backend iniciado en http://localhost:5127 (PID $($backend.Id))." -ForegroundColor Green
}

if (Test-Port 5500) {
    Write-Host "El puerto 5500 ya esta en uso. Si es el frontend, puedes abrir http://localhost:5500." -ForegroundColor Yellow
} else {
    $frontendOut = Join-Path $logDir "frontend.out.log"
    $frontendErr = Join-Path $logDir "frontend.err.log"
    $frontend = Start-Process powershell `
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$frontendServer`" -Root `"$frontendDir`" -Port 5500" `
        -WorkingDirectory $frontendDir `
        -RedirectStandardOutput $frontendOut `
        -RedirectStandardError $frontendErr `
        -WindowStyle Hidden `
        -PassThru
    Write-Host "Frontend iniciado en http://localhost:5500 (PID $($frontend.Id))." -ForegroundColor Green
}

$processes = @{
    backend = if ($backend) { $backend.Id } else { $null }
    frontend = if ($frontend) { $frontend.Id } else { $null }
    startedAt = (Get-Date).ToString("s")
}
$processes | ConvertTo-Json | Set-Content -Path $pidFile -Encoding UTF8

Write-Host ""
Write-Host "Listo. Abre:" -ForegroundColor Cyan
Write-Host "  App:     http://localhost:5500"
Write-Host "  Swagger: http://localhost:5127/swagger"
Write-Host ""
Write-Host "Logs:" -ForegroundColor Cyan
Write-Host "  $logDir"
Write-Host ""
Write-Host "Para detenerlos:" -ForegroundColor Cyan
Write-Host "  .\stop-local.ps1"
