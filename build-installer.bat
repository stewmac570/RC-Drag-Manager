@echo off
setlocal

cd /d "%~dp0"

echo.
echo [build-installer] Starting installer build...
echo [build-installer] Repo root: %CD%
echo [build-installer] Calling PowerShell script: Installer\build-installer.ps1
echo.

powershell -ExecutionPolicy Bypass -File "Installer\build-installer.ps1"
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if not "%EXIT_CODE%"=="0" (
    echo [build-installer] Build failed with exit code %EXIT_CODE%.
    pause
    exit /b %EXIT_CODE%
)

echo [build-installer] Build finished successfully.
echo [build-installer] Installer output should be in: Installer\output\
pause
exit /b 0
