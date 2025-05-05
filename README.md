# Real Radio

> A radio plugin for Schedule I using BepInEx

**Note: Requires alternate branch on Steam.**

Currently in development.

## Existing features
- Play remote audio streams (from urls). Supports all file formats that Media Foundation supports
- A remote stream can be played through multiple AudioSources in Unity (using AudioStreamHost and AudioStreamClient, managed through the AudioStreamManager singleton)
- Placeable radio object that you can put in your properties. This uses custom placement logic which allows placing the radio object on almost any surface. Persistance (saving/loading) for these are not implemented yet. Multiplayer functionality is fully working.
- A generic radial menu singleton, currently used in vehicles (radio station selection) and placeable radio objects
- All game objects with a LandVehicle component have an associated VehicleRadioProxy networked object which controls playing audio from the vehicle. Players can change radio station by holding the reload button and selecting a station in the radial menu. Audio effects are changed depending on if the player is inside the vehicle or not. When inside the vehicle the audio is not spatialized and has no audio filters. When the player is not in the vehicle the audio is spatialized and a low pass filter is applied to simulate the audio being muffled from inside the car.
- Residential buildings where NPCs live have a chance (50% atm) to play music when the building has NPCs inside. The time when music starts and stops in a day is randomized at the end of each day. This logic only runs on the server but the playing radio station is synced to all clients
- Radio stations played by NPCs are randomized. Radio stations can be excluded from being played by NPCs

## General project goals
- Immersion is important, but if something feels better even if it's less realistic that should be preferred
- Generally speaking the mod should be highly polished to the best of your ability, as if it was part of the base game.
- Custom assets should fit the art style of the base game
