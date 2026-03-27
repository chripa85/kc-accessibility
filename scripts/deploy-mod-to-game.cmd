@echo off
setlocal

set "REPO_ROOT=%~dp0.."
for %%I in ("%REPO_ROOT%") do set "REPO_ROOT=%%~fI"

set "MOD_NAME=kc-accessibility"
set "SOURCE_MOD_DIR=%REPO_ROOT%\mod\%MOD_NAME%"
set "GAME_ROOT=C:\Games\Kingdoms and Castles"
set "TARGET_MOD_DIR=%GAME_ROOT%\KingdomsAndCastles_Data\mods\%MOD_NAME%"

if not exist "%SOURCE_MOD_DIR%" (
  echo Source mod folder not found: "%SOURCE_MOD_DIR%"
  exit /b 1
)

if not exist "%TARGET_MOD_DIR%" mkdir "%TARGET_MOD_DIR%"

for %%F in ("%SOURCE_MOD_DIR%\*") do (
  if /I not "%%~aF"=="d" copy /Y "%%~fF" "%TARGET_MOD_DIR%\%%~nxF" >nul
)

if exist "%TARGET_MOD_DIR%\output.txt" del /F /Q "%TARGET_MOD_DIR%\output.txt"

echo Deployed staged mod files to "%TARGET_MOD_DIR%"
echo Cleared "%TARGET_MOD_DIR%\output.txt"
exit /b 0
