@echo off
title PulseWatch Application Launcher
color 0A

echo.

:: Build Backend
echo [1/3] Building Backend...
cd pulsewatch.api
dotnet build
if %errorlevel% neq 0 (
    color 0C
    echo Build failed! Check errors above.
    pause
    exit /b 1
)
echo Build successful!
cd ..
echo.

:: Run Backend
echo [2/3] Starting Backend Server...
cd pulsewatch.api
start "PulseWatch API" cmd /k "dotnet run"
cd ..
timeout /t 3 /nobreak >nul
echo Backend started!
echo.

:: Run Frontend
echo [3/3] Starting Frontend Server...
cd pulsewatch.client
start "PulseWatch Client" cmd /k "npm run dev"
cd ..
echo Frontend started!
echo.

echo.
echo Servers are running in separate windows.
echo Close the windows to stop the servers.
echo.
pause