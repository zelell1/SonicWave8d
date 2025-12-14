# SonicWave 8D - PowerShell Launch Script
# Spatial Audio Converter

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " SonicWave 8D - Quick Launch Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check .NET SDK
Write-Host "[1/4] Checking .NET SDK installation..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "[OK] .NET SDK $dotnetVersion found!" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] .NET SDK is not installed!" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 SDK from:" -ForegroundColor Red
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host ""

# Step 2: Check project file
if (-not (Test-Path "SonicWave8D.csproj")) {
    Write-Host "[ERROR] Project file not found!" -ForegroundColor Red
    Write-Host "Please run this script from the SonicWave8D directory." -ForegroundColor Red
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 3: Restore dependencies
Write-Host "[2/4] Restoring NuGet packages..." -ForegroundColor Yellow
try {
    dotnet restore | Out-Null
    Write-Host "[OK] Packages restored!" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to restore packages!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host ""

# Step 4: Trust development certificate
Write-Host "[3/4] Trusting HTTPS development certificate..." -ForegroundColor Yellow
try {
    dotnet dev-certs https --trust 2>&1 | Out-Null
    Write-Host "[OK] Certificate configured!" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Could not configure certificate (may require admin)" -ForegroundColor Yellow
}
Write-Host ""

# Step 5: Run the application
Write-Host "[4/4] Starting SonicWave 8D..." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Application is starting..." -ForegroundColor Cyan
Write-Host " Press Ctrl+C to stop the server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Open browser after delay
Write-Host "Opening browser in 3 seconds..." -ForegroundColor Green
Write-Host ""

Start-Sleep -Seconds 3
Start-Process "https://localhost:5001"

# Run the application
dotnet run

# If we get here, the application stopped
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " SonicWave 8D has stopped." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"
