# Accessibility Control Scheme

This document describes the accessibility-oriented control model used by the mod and the intended direction for nearby follow-up work.

Some items are already implemented, while others remain part of the planned control model. The intent should stay stable even when implementation details change.

## Scope Rule

Unless a dedicated island-cycling command is used, navigation and discovery commands should stay within the current island.

## Movement

- `E S D F` move 1 tile
- `Shift + E S D F` move 10 tiles

Movement speech should prefer:

- content type, if present
- otherwise tile type
- then coordinates

## Keep Navigation

- `H` announce relative position to the current island keep
- double `H` move to the keep

Example:

`14 east, 6 south`

## Matching-Tile Cycling

- `W` next closest matching tile
- `Shift + W` previous matching tile

Rules:

- match on content type first, otherwise tile type
- sort by distance
- use a stable clockwise tie-breaker
- if no match exists, say `No other matching tiles`

## Bookmarks

- `Ctrl + digit` set bookmark
- `digit` jump to bookmark
- named bookmarks are planned but should remain explicit and spoken

Jump speech should include:

- bookmark name if present
- tile or content description
- coordinates

## Mesh Awareness

- `V` single press: surrounding-tile readout
- second quick press: grouped summary

The readout should:

- use a stable order
- avoid unnecessary direction words
- stay short enough for practical play

## Directional Helpers

- `Ctrl + V` then direction: directional mesh
- `Alt + direction`: directional scan
- `Ctrl + direction`: directional summary

These helpers should provide structured, compact speech and avoid silent failures.

## Resource Access

- `R` open resource selection
- `Ctrl + R` direct resource value prompt

Selecting a resource should speak the value and, where appropriate, may move focus to a relevant source tile on the current island.

## Actions

- `C` chop / cancel chopping
- `B` open build menu

## Build Menu

Inside the build menu:

- category navigation should remain distinct from item navigation
- selecting a building should lead into accessible placement handling
- `Esc` should cancel or step back predictably

Placement mode should be sticky until explicitly canceled.

## Time Control

- `+` increase speed
- `-` decrease speed

## Search and Discovery

Search-style helpers should:

- be island-scoped by default
- use stable result ordering
- cache result sets when browsing through matches
- give explicit feedback when no result exists

## Consistency Rules

- use the same word for the same thing
- keep output order stable
- keep speech short and structured
- avoid silent failures
- every action should give feedback
