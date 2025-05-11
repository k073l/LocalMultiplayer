# Local Multiplayer

> MelonLoader mod for testing multiplayer functionality

## Credits
- [Skippy](https://github.com/Skippeh/Schedule1RealRadioMod/tree/main/LocalMultiplayer) - Author of the mod for BepInEx Mono

## Installation
1. Install MelonLoader
2. Extract the zip file
3. Place the dll file into the Mods directory for your branch
    - For none/beta use IL2CPP
    - For alternate/alternate beta use Mono
4. Launch the game

## Usage
For automated usage you can use a batch file to launch two instances of the game with the mod (in this example file is located in game directory).
```batch
start "" "Schedule I.exe" --host --adjust-window --left-offset 0
timeout /t 1
start "" "Schedule I.exe" --join --adjust-window --left-offset 20
```
For manual usage launch the game and follow instructions on screen.

Ensure that port `7777` is not in use by another application.
When hosting, mod will automatically pick last played save or first one in the list.
When joining, mod will automatically join hosted game when available.

### Flags
- `--host`, `-h` - Host a game
- `--join`, `-j` - Join a game
- `--adjust-window`, `-s` - Adjust the window size and position
- `--left-offset`, `-o` - Gap between the two windows
