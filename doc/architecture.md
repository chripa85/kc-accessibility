# Architecture

## Overview

The mod is a C# class library loaded by the Kingdoms and Castles mod runtime. It creates a persistent Unity host object that owns:

- screen-reader initialization and speech output
- load/save announcements
- accessibility input handling
- Harmony patches for menu, mode, and camera/startup transitions

The code is organized around runtime behavior rather than by UI screen alone.

## Main Runtime Pieces

### `ScreenReaderLoadMod`

Entry point used by the game's mod loader.

Responsibilities:

- initialize native speech support during `Preload`
- create the persistent host on `SceneLoaded`
- issue the first load announcement
- provide a single logging entry point for the mod

### `KCTolk`

Thin speech bridge around Tolk and direct NVDA fallback.

Responsibilities:

- resolve native DLLs from the mod folder
- initialize Tolk
- fall back to direct NVDA output when Tolk incorrectly resolves to SAPI while NVDA is active
- keep native interop isolated from gameplay/menu logic

### `ScreenReaderLoadBehaviour`

Persistent Unity `MonoBehaviour` used for load/save event announcements.

Responsibilities:

- subscribe to `Broadcast.OnLoadedEvent`
- announce save-load completion
- stay alive across scene changes

### `ScreenReaderAccessibilityBehaviour`

Persistent Unity `MonoBehaviour` that owns accessibility navigation behavior.

Responsibilities:

- install Harmony patches
- load mod keybindings and route input through the accessibility layer
- temporarily disable the game's native keyboard bindings for the mod session
- manage main-menu accessibility state
- manage gameplay accessibility state
- manage modal accessibility commands such as directional mesh and resource-slot prompts
- route accessibility input
- announce focus changes, values, and gameplay state
- apply preferred gameplay camera angle on gameplay startup
- keep logging stable for manual verification

This class is split into multiple partial files by feature area:

- main menu handlers
- gameplay map handlers
- gameplay discovery helpers
- gameplay pending-command handlers
- gameplay build handlers
- gameplay panel handlers
- gameplay panel accessibility helpers
- menu and settings helpers
- shared helpers
- state
- routing

## Input Model

The mod uses its own keybinding layer through `ModKeyBindings`. In gameplay, the accessibility behavior checks those bindings first and routes input into:

- map navigation
- panel navigation
- build menu navigation
- discovery/search helpers
- modal prompts such as resource slot selection and directional mesh selection
- bookmark/resource/anchor features

The accessibility layer intentionally reuses the game's real UI and gameplay systems rather than building a parallel fake interface. During an active mod session it also suppresses the game's native keyboard bindings so the accessibility bindings remain authoritative.

## Patching Model

Harmony patches are used for high-signal events such as:

- main menu state transitions
- console item hover/focus updates
- settings menu enable
- difficulty refresh
- save/load UI setup
- banner picker open/close
- gameplay mode transitions
- game start camera initialization

These patches are intentionally narrow and are used to observe or lightly steer the game rather than replace large systems.

## Logging Model

Logging is part of the verification strategy, not just debugging noise.

The mod logs:

- startup and initialization milestones
- patch installation
- accessibility announcements
- optional input-debug traces
- important gameplay navigation events

Guidelines:

- keep log messages explicit and stable
- avoid vague messages that are hard to correlate with player actions
- preserve enough signal to reconstruct a test session from `output.txt`

## Documentation Boundaries

Public repository documentation should stay focused on:

- how the mod works
- how to build and test it
- active bindings and behavior
- architecture relevant to contributors

Local reverse-engineering notes, decompiled references, and machine-specific investigation material belong under ignored `local/` content, not in tracked public source.
