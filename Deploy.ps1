param(
    [switch]$SkipTests,
    [switch]$SkipBuild,
    [switch]$SkipLaunch
)

Write-Host "=== SteamSwitcher Deploy Script ===" -ForegroundColor Cyan
Write-Host "Starting deployment process..." -ForegroundColor Yellow

# Change to project root directory
$projectRoot = $PSScriptRoot
Set-Location $projectRoot

# Step 1: Run Tests
if (-not $SkipTests) {
    Write-Host "`n--- Running Tests ---" -ForegroundColor Green
    try {
        dotnet test --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Tests failed! Aborting deployment." -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ All tests passed!" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error running tests: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`n--- Skipping Tests ---" -ForegroundColor Yellow
}

# Step 2: Build Solution
if (-not $SkipBuild) {
    Write-Host "`n--- Building Solution ---" -ForegroundColor Green
    try {
        $buildResult = dotnet build --configuration Release --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Build failed! Aborting deployment." -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ Build successful!" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error during build: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`n--- Skipping Build ---" -ForegroundColor Yellow
}

# Step 3: Launch Application
if (-not $SkipLaunch) {
    Write-Host "`n--- Launching Application ---" -ForegroundColor Green

    # Kill any existing SteamSwitcherGUI processes
    $existingProcesses = Get-Process -Name "SteamSwitcherGUI" -ErrorAction SilentlyContinue
    if ($existingProcesses) {
        Write-Host "Terminating existing SteamSwitcherGUI processes..." -ForegroundColor Yellow
        $existingProcesses | Stop-Process -Force
        Start-Sleep -Seconds 2
    }

    # Launch the application
    $guiPath = Join-Path $projectRoot "SteamSwitcher\SteamSwitcherGUI\bin\Release\net10.0-windows\SteamSwitcherGUI.exe"
    if (Test-Path $guiPath) {
        try {
            Write-Host "Starting SteamSwitcherGUI..." -ForegroundColor Yellow
            Start-Process -FilePath $guiPath
            Write-Host "✅ Application launched successfully!" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Error launching application: $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ GUI executable not found at: $guiPath" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`n--- Skipping Launch ---" -ForegroundColor Yellow
}

Write-Host "`n🎉 Deployment completed successfully!" -ForegroundColor Cyan
