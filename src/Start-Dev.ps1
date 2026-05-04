#Requires -Version 5.1
<#
.SYNOPSIS
    Avvia in parallelo: WebServer ASP.NET, React dev server e client WinForms.
.DESCRIPTION
    Ogni processo viene aperto in una finestra PowerShell separata.
    Chiudi le finestre per arrestare i servizi.
#>

$root      = $PSScriptRoot
$backend   = Join-Path $root "\Backend"
$frontend  = Join-Path $root "\Frontend"

$webServer = Join-Path $backend "CommRouter.WebServer"
$winForms  = Join-Path $backend "CommRouter"

function Start-InNewWindow {
    param(
        [string] $Title,
        [string] $WorkingDir,
        [string] $Command
    )
    Start-Process powershell.exe -ArgumentList @(
        "-NoExit",
        "-Command",
        "& { `$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location '$WorkingDir'; $Command }"
    )
}
Write-Host "Build soluzione .NET..." -ForegroundColor Cyan
Push-Location $backenddotnet build CommRouter.slnx --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build fallita. Correggere gli errori prima di avviare." -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "Build completata." -ForegroundColor Green
Write-Host ""
Write-Host "Avvio WebServer (localhost:5000)..." -ForegroundColor Cyan
Start-InNewWindow `
    -Title      "ComRouter - WebServer" `
    -WorkingDir $webServer `
    -Command    "dotnet run --launch-profile http"

Write-Host "Attendo 3 secondi prima di avviare React..." -ForegroundColor Gray
Start-Sleep -Seconds 3

Write-Host "Avvio React dev server (localhost:5173)..." -ForegroundColor Cyan
Start-InNewWindow `
    -Title      "ComRouter - React" `
    -WorkingDir $frontend `
    -Command    "npm run dev"

Write-Host "Attendo 2 secondi prima di avviare il client WinForms..." -ForegroundColor Gray
Start-Sleep -Seconds 2

Write-Host "Avvio client WinForms..." -ForegroundColor Cyan
Start-InNewWindow `
    -Title      "ComRouter - WinForms" `
    -WorkingDir $winForms `
    -Command    "dotnet run"

Write-Host ""
Write-Host "Tutti i processi sono stati avviati:" -ForegroundColor Green
Write-Host "  WebServer  -> http://localhost:5025"  -ForegroundColor White
Write-Host "  React UI   -> http://localhost:5173"  -ForegroundColor White
Write-Host "  WinForms   -> si connette a http://localhost:5025" -ForegroundColor White
Write-Host ""
Write-Host "Chiudi le finestre PowerShell aperte per fermare i servizi." -ForegroundColor Gray
