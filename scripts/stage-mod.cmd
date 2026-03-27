@echo off
setlocal

set "PROJECT_DIR=%~1"
if "%PROJECT_DIR%"=="" (
  echo Expected project directory as first argument.
  exit /b 1
)

for %%I in ("%PROJECT_DIR%..\\..") do set "REPO_ROOT=%%~fI"
set "MOD_NAME=kc-accessibility"
set "STAGE_ROOT=%REPO_ROOT%\mod"
set "MOD_ROOT=%STAGE_ROOT%\%MOD_NAME%"

if not exist "%STAGE_ROOT%" mkdir "%STAGE_ROOT%"
if not exist "%MOD_ROOT%" mkdir "%MOD_ROOT%"

for %%F in ("%PROJECT_DIR%*.cs") do (
  if /I not "%%~nxF"=="AssemblyInfo.cs" copy /Y "%%~fF" "%MOD_ROOT%\%%~nxF" >nul
)
copy /Y "%PROJECT_DIR%info.json" "%MOD_ROOT%\info.json" >nul
if exist "%PROJECT_DIR%keybindings.json" copy /Y "%PROJECT_DIR%keybindings.json" "%MOD_ROOT%\keybindings.json" >nul
copy /Y "%REPO_ROOT%\lib\Tolk.dll" "%MOD_ROOT%\Tolk.dll" >nul
copy /Y "%REPO_ROOT%\lib\nvdaControllerClient64.dll" "%MOD_ROOT%\nvdaControllerClient64.dll" >nul

echo Staged mod files in "%MOD_ROOT%"
exit /b 0
