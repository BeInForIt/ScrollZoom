# Scroll Zoom

## Game

Barotrauma

## Description

Client-side camera mod that adds scroll-wheel zoom and freecam. Patches `Camera.MoveCamera` via Harmony to control zoom level, freecam mode, and camera lock.

## Features

- **Scroll zoom**: Mouse wheel adjusts camera zoom (0.35x–2.25x). Toggle with NumPad5.
- **Freecam**: Press F to detach camera from character; camera follows mouse. Toggle with F.
- **Lock to character**: NumPad6 locks camera to character position.

## Installation

Copy the mod folder into Barotrauma's local mods directory. Enable the mod in the game's Content Packages menu.

## Compatibility

- Mod version: 1.0.0
- Game version: 1.0.0.0 (declared in `filelist.xml`)
- Uses Harmony for patching; may conflict with other camera mods.

## Dependencies

- Barotrauma
- HarmonyLib (via game/LuaCs)
- Microsoft.Xna.Framework

## Technical Notes

- Client-only: `CSharp/Client/` assembly.
