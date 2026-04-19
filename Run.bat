@echo off
echo === SteamSwitcher Quick Launch ===
echo Building and launching SteamSwitcher...
cd /d "%~dp0"
dotnet build --configuration Release --verbosity minimal
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)
echo Build successful!
start "" "SteamSwitcher\SteamSwitcherGUI\bin\Release\net10.0-windows\SteamSwitcherGUI.exe"
echo Application launched!