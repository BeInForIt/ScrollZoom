# Scroll Zoom

## Game

Barotrauma

## Description

Client-side camera mod: scroll-wheel zoom, freecam, optional freeze of freecam position, and lock-to-character. Patches `Camera.MoveCamera` via Harmony. Optional `scrollzoom_config.txt` controls zoom range and sensitivity.

## Features

- **Scroll zoom**: Mouse wheel adjusts zoom within configurable min/max. Toggle with NumPad5.
- **Freecam**: F toggles freecam; camera can follow the cursor at configurable speed.
- **Freeze freecam**: NumPad4 toggles frozen freecam position (only while freecam is on).
- **Lock to character**: NumPad6 toggles locking the camera to the character.
- **Config file**: `scrollzoom_config.txt` with keys `MaxZoomOut`, `MaxZoomIn`, `ZoomStepPerNotch`, `FreecamChaseSpeed` (created with defaults if missing). Resolved under the mod folder in `LocalMods`, assembly directory, or game directory.

## Installation

Copy the mod folder into Barotrauma's local mods directory. Enable the mod in the game's Content Packages menu.

## Compatibility

- Mod version: 1.0.0
- Game version: 1.0.0.0 (declared in `filelist.xml`)
- Uses Harmony; may conflict with other camera mods.

## Dependencies

- Barotrauma
- HarmonyLib
- Microsoft.Xna.Framework

## Technical Notes

- Client-only: `CSharp/Client/` assembly.
