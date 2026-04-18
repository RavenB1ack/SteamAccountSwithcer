$ErrorActionPreference = 'Stop'

$solutionPath = Join-Path $PSScriptRoot 'project.sln'
$exePath = Join-Path $PSScriptRoot 'SteamSwitcher\SteamSwitcherGUI\bin\Debug\net10.0-windows\SteamSwitcherGUI.exe'
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

Write-Host 'Killing existing SteamSwitcherGUI.exe if running...' -ForegroundColor Cyan
Start-Process -FilePath taskkill -ArgumentList '/F','/IM','SteamSwitcherGUI.exe' -NoNewWindow -Wait -ErrorAction SilentlyContinue

Write-Host 'Building solution...' -ForegroundColor Cyan
& $dotnet build $solutionPath

if (-not (Test-Path $exePath)) {
    Write-Error "Executable not found: $exePath"
    exit 1
}

Write-Host 'Launching SteamSwitcher GUI...' -ForegroundColor Cyan
Start-Process -FilePath $exePath
