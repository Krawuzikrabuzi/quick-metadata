# install.ps1
$ErrorActionPreference = "Stop"

# Self-elevate
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
    Start-Process powershell.exe -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

# Variablen  <-- das fehlte
$installDir = "C:\Program Files\QuickMetadata"
$exePath    = "$installDir\QuickMetadata.exe"
$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition

# create install dir
Write-Host "Installiere nach $installDir ..." -ForegroundColor Cyan
if (Test-Path $installDir) {
    Remove-Item "$installDir\*" -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $installDir | Out-Null
}

# copy files
Get-ChildItem -Path $scriptDir | Where-Object { $_.Name -ne "install.ps1" } | ForEach-Object {
    Copy-Item $_.FullName -Destination $installDir -Recurse -Force
}
Write-Host "Dateien kopiert." -ForegroundColor Green

# create registry entries
Write-Host "Schreibe Registry-Eintraege..." -ForegroundColor Cyan
$jpgKey = "HKCU:\SOFTWARE\Classes\SystemFileAssociations\.jpg\shell\QuickMetadata"
$dirKey = "HKCU:\SOFTWARE\Classes\Directory\shell\QuickMetadata"

New-Item -Path "$jpgKey\command" -Force | Out-Null
Set-ItemProperty -Path $jpgKey           -Name "(default)"        -Value "QuickMetadaten"
Set-ItemProperty -Path $jpgKey           -Name "MultiSelectModel" -Value "Player"
Set-ItemProperty -Path "$jpgKey\command" -Name "(default)"        -Value "`"$exePath`" `"%1`""

New-Item -Path "$dirKey\command" -Force | Out-Null
Set-ItemProperty -Path $dirKey           -Name "(default)"        -Value "QuickMetadaten - all JPGs"
Set-ItemProperty -Path "$dirKey\command" -Name "(default)"        -Value "`"$exePath`" /folder `"%1`""

Write-Host "Registry-Eintraege gesetzt." -ForegroundColor Green

# restart explorer
Write-Host "Starte Explorer neu..." -ForegroundColor Cyan
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Start-Process explorer

Write-Host ""
Write-Host "Installation abgeschlossen!" -ForegroundColor Green
pause