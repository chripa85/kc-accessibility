# Repository Guidelines

## Project Structure & Module Organization
Core mod source lives in `src/kc-accessibility/` (C# .NET Framework 4.7.2 class library).  
Reference assemblies and native dependencies are in `lib/` (for example `Assembly-CSharp.dll`, `0Harmony.dll`, `Tolk.dll`).  
Generated/staged mod files are in `mod/kc-accessibility/` and should be treated as build output.  
Helper scripts are in `scripts/`:
- `stage-mod.cmd` copies built artifacts and source payload into `mod/...`
- `deploy-mod-to-game.cmd` copies the staged mod into the game install

Public documentation lives under `doc/`. Local-only research, decompiled references, and machine-specific notes belong under ignored `local/`.

## Build, Test, and Development Commands
- `msbuild src\kc-accessibility\kc-accessibility.csproj /t:Build /p:Configuration=Debug`  
  Builds the mod DLL and runs the post-build stage step.
- `cmd /c scripts\stage-mod.cmd "C:\...\src\kc-accessibility\"`  
  Manually refreshes `mod/kc-accessibility/`.
- `cmd /c scripts\deploy-mod-to-game.cmd`  
  Deploys staged files to the local game install configured by the script.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and braces on new lines (existing repository style).  
Prefer small, focused helper methods over long monolithic handlers.  
Use `PascalCase` for types/methods/properties, `camelCase` for fields/locals.  
Keep log messages explicit and stable for gameplay verification.

## Testing Guidelines
There is currently no automated test project. Validate by:
1. Building successfully.
2. Deploying via script.
3. Verifying behavior and logs in game (`output.txt` in mod folder and global `mods\log.txt`).

When changing input/navigation logic, include a short manual test note in PR/commit context.

## Commit & Pull Request Guidelines
Recent commit history favors short imperative messages (for example: `Fix quit dialog navigation`, `Refactor accessibility code`).  
Keep commits scoped to one change area (bindings, gameplay navigation, menu handling, refactor).  
PRs should include:
- what changed and why
- affected game screens/states
- verification steps performed
- relevant log excerpts or screenshots when behavior changes

## Agent-Specific Instructions
- You should not make manual changes to the `mod` folder. Whenever the stage script is executed the files in there are modified.
- You should not change files deployed as this is done by the deploy script.
