# Kingdoms and Castles Accessibility Mod

Accessibility mod for *Kingdoms and Castles* focused on screen-reader support, spoken menu navigation, and keyboard-driven gameplay navigation.

## What It Does

The mod adds spoken feedback for:

- game load and save-load events
- main menu and settings navigation
- gameplay map navigation
- building placement and build menu navigation
- gameplay panel access such as island info, worker/job views, and related UI
- island summaries, keep-relative navigation, resource announcements, and other accessibility-oriented helpers

Speech output is provided through Tolk, with direct NVDA fallback handling when needed.

## Requirements

- Windows version of *Kingdoms and Castles*
- a working screen reader such as NVDA
- the game's mod loading system enabled through the normal `mods` folder

## Install

Build the project or run the stage script, then copy the staged `mod/kc-accessibility/` folder into the game's `mods` directory so the final structure is:

```text
KingdomsAndCastles_Data/
  mods/
    kc-accessibility/
      info.json
      ScreenReaderAccessibilityBehaviour.cs
      ScreenReaderLoadMod.cs
      keybindings.json
      Tolk.dll
      nvdaControllerClient64.dll
      ...
```

This mod is packaged as source files plus metadata and native speech DLLs, not as a single mod DLL. The native speech DLLs must remain beside the mod files.

## Build

Build the project from the repository root:

```powershell
msbuild src\kc-accessibility\kc-accessibility.csproj /t:Build /p:Configuration=Debug
```

The post-build step stages the mod into `mod/kc-accessibility/`.

## Deploy

Deploy the staged mod with:

```powershell
cmd /c scripts\deploy-mod-to-game.cmd
```

## Repository Layout

- `src/kc-accessibility/` mod source
- `lib/` required game and native dependencies
- `scripts/` stage and deploy helpers
- `doc/` public documentation
- `local/` ignored local-only research and notes

## Key Documentation

- [Controls Spec](doc/accessibility-control-scheme-spec.md)
- [Architecture](doc/architecture.md)
- [Manual Regression Checklist](doc/manual-accessibility-regression-checklist.md)

## Logging

The mod writes explicit runtime logs intended to support manual accessibility verification. When testing changes, check:

- the mod-specific `output.txt`
- the game's general mods log

Keep logging stable and meaningful. Accessibility behavior in this project is verified primarily through build success plus in-game testing and log review.

## Development Notes

- `mod/` is generated output and should not be edited manually
- deployed game files should be updated via the deploy script, not by hand
- local-only investigation material belongs under `local/`, not in tracked source
