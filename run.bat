@echo off
title SonicWave 8D - Spatial Audio Converter
color 0B

echo ========================================
echo  SonicWave 8D - Quick Launch Script
echo ========================================
echo.

REM Check if .NET SDK is installed
echo [1/4] Checking .NET SDK installation...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK is not installed!
    echo Please install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo [OK] .NET SDK found!
echo.

REM Check if project file exists
if not exist "SonicWave8D.csproj" (
    echo [ERROR] Project file not found!
    echo Please run this script from the SonicWave8D directory.
    echo.
    pause
    exit /b 1
)

REM Restore dependencies
echo [2/4] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] Failed to restore packages!
    pause
    exit /b 1
)
echo [OK] Packages restored!
echo.

REM Trust development certificate
echo [3/4] Trusting HTTPS development certificate...
dotnet dev-certs https --trust >nul 2>&1
echo [OK] Certificate configured!
echo.

REM Run the application
echo [4/4] Starting SonicWave 8D...
echo.
echo ========================================
echo  Application is starting...
echo  Press Ctrl+C to stop the server
echo ========================================
echo.
echo Opening browser in 3 seconds...
echo.

REM Wait 3 seconds then open browser
timeout /t 3 /nobreak >nul
start https://localhost:5001

REM Run the application
dotnet run

REM If we get here, the application stopped
echo.
echo ========================================
echo  SonicWave 8D has stopped.
echo ========================================
echo.
pause
