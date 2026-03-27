# Manual Accessibility Regression Checklist

Use this checklist after building and deploying the mod.

## Environment
- Build: `Debug`
- Mod deployed with `scripts\deploy-mod-to-game.cmd`
- Logs:
  - `KingdomsAndCastles_Data\mods\kc-accessibility\output.txt`
  - `KingdomsAndCastles_Data\mods\log.txt`

## Main Menu Flow
- Start game and confirm initial speech.
- Navigate `Menu` entries up/down and activate each primary action.
- Open `Choose Mode` and verify left/right/up/down/back/submit behavior.
- Open `Choose Difficulty` and verify difficulty cycling + accept/back.
- Open `Name and Banner` and verify text-field editing, submit, and back.
- Open `New Map` and verify component adjustments and back.
- Open `Settings` and verify tab switching and component adjustments.
- Open `Pause` and verify navigation + cancel to back-to-game.
- Open `Quit/Exit Confirm` and verify focus, selection, cancel behavior.

## Gameplay Navigation
- Enter gameplay and enable keyboard navigation.
- Verify map cursor movement (normal and fast-move modifier).
- Verify tile element cycling and announcements.
- Verify primary/secondary actions at cursor.
- Verify entering/exiting build menu via accessibility bindings.

## Build Menu Accessibility
- Verify category switching left/right and numeric shortcuts.
- Verify item list navigation and activation.
- Verify detail mode navigation and cancel return.
- Verify locked-item reason and cost announcements.
- Verify placement-mode transition announcements after build action.

## Gameplay Panels
- Advisor panel: open, navigate items, submit, close.
- Person panel: navigate details, track actions, close.
- Worker panel: summary/actions/worker list/trash/close.
- Construct panel: summary/pause-find-demolish actions.
- Decree panel: navigate, reorder, toggle, exit back to island info.
- Island info + foreign island info: navigate, adjust tax, toggle overlays, close.

## Log Checks
- Confirm no repeated announcement spam from one key press.
- Confirm no unexpected exceptions in mod log output.
- Confirm no leftover debug noise beyond standard accessibility logs.
