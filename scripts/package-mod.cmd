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
set "PACKAGE_ROOT=%DIST_ROOT%\package"
set "PACKAGE_MOD_ROOT=%PACKAGE_ROOT%\%MOD_NAME%"

if not exist "%PROJECT_DIR%kc-accessibility.csproj" (
  echo Project file not found under "%PROJECT_DIR%"
  exit /b 1
)

call "%~dp0stage-mod.cmd" "%PROJECT_DIR%"
if errorlevel 1 exit /b 1

if not exist "%DIST_ROOT%" mkdir "%DIST_ROOT%"
if exist "%ZIP_PATH%" del /F /Q "%ZIP_PATH%"
if exist "%PACKAGE_ROOT%" rmdir /S /Q "%PACKAGE_ROOT%"
mkdir "%PACKAGE_MOD_ROOT%"
if errorlevel 1 exit /b 1

for %%F in ("%MOD_ROOT%\*") do (
  if /I not "%%~aF"=="d" copy /Y "%%~fF" "%PACKAGE_MOD_ROOT%\%%~nxF" >nul
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "Push-Location '%PACKAGE_ROOT%'; Compress-Archive -Path '%MOD_NAME%' -DestinationPath '%ZIP_PATH%' -Force; Pop-Location"
if errorlevel 1 exit /b 1

rmdir /S /Q "%PACKAGE_ROOT%"

echo Packaged mod zip at "%ZIP_PATH%"
exit /b 0
