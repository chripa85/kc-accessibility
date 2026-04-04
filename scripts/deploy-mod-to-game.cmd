@echo off
setlocal

set "REPO_ROOT=%~dp0.."
for %%I in ("%REPO_ROOT%") do set "REPO_ROOT=%%~fI"

set "MOD_NAME=kc-accessibility"
set "SOURCE_MOD_DIR=%REPO_ROOT%\mod\%MOD_NAME%"
set "GAME_ROOT=C:\Games\Kingdoms and Castles"
set "GAME_STEAM_ROOT=C:\Program Files (x86)\Steam\steamapps\common\Kingdoms and Castles"
set "TARGET_MOD_DIR=%GAME_ROOT%\KingdomsAndCastles_Data\mods\%MOD_NAME%"
set "TARGET_2_MOD_DIR=%GAME_STEAM_ROOT%\KingdomsAndCastles_Data\mods\%MOD_NAME%"

if not exist "%SOURCE_MOD_DIR%" (
  echo Source mod folder not found: "%SOURCE_MOD_DIR%"
  exit /b 1
)

set "DEPLOY_SUCCESS=0"

call :DeployTarget "%GAME_ROOT%" "%TARGET_MOD_DIR%"
if not errorlevel 1 set "DEPLOY_SUCCESS=1"

call :DeployTarget "%GAME_STEAM_ROOT%" "%TARGET_2_MOD_DIR%"
if not errorlevel 1 set "DEPLOY_SUCCESS=1"

if "%DEPLOY_SUCCESS%"=="1" (
  exit /b 0
)

echo Deployment failed: no deployment targets were available or copied successfully.
exit /b 1

:DeployTarget
set "TARGET_ROOT=%~1"
set "TARGET_DIR=%~2"

if not exist "%TARGET_ROOT%" (
  echo Deployment skipped: target root not found: "%TARGET_ROOT%"
  exit /b 1
)

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
if errorlevel 1 (
  echo Deployment failed: could not create target mod folder: "%TARGET_DIR%"
  exit /b 1
)

for %%F in ("%TARGET_DIR%\*") do (
  if /I not "%%~aF"=="d" del /F /Q "%%~fF" >nul
)

for %%F in ("%SOURCE_MOD_DIR%\*") do (
  if /I not "%%~aF"=="d" copy /Y "%%~fF" "%TARGET_DIR%\%%~nxF" >nul
)

if exist "%TARGET_DIR%\output.txt" del /F /Q "%TARGET_DIR%\output.txt"

echo Mirrored staged mod files to "%TARGET_DIR%"
echo Cleared "%TARGET_DIR%\output.txt"
exit /b 0
