@echo off
setlocal

set "REPO_ROOT=%~dp0.."
for %%I in ("%REPO_ROOT%") do set "REPO_ROOT=%%~fI"

set "PROJECT_DIR=%REPO_ROOT%\src\kc-accessibility\"
set "MOD_NAME=kc-accessibility"
set "STAGE_ROOT=%REPO_ROOT%\mod"
set "MOD_ROOT=%STAGE_ROOT%\%MOD_NAME%"
set "DIST_ROOT=%REPO_ROOT%\dist"
set "ZIP_PATH=%DIST_ROOT%\%MOD_NAME%.zip"

if not exist "%PROJECT_DIR%kc-accessibility.csproj" (
  echo Project file not found under "%PROJECT_DIR%"
  exit /b 1
)

call "%~dp0stage-mod.cmd" "%PROJECT_DIR%"
if errorlevel 1 exit /b 1

if not exist "%DIST_ROOT%" mkdir "%DIST_ROOT%"
if exist "%ZIP_PATH%" del /F /Q "%ZIP_PATH%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%MOD_ROOT%\*' -DestinationPath '%ZIP_PATH%' -Force"
if errorlevel 1 exit /b 1

echo Packaged mod zip at "%ZIP_PATH%"
exit /b 0
