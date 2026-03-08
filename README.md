# SaveSouls

A save file manager for FromSoftware games. Allows you to quickly save and load game states using global hotkeys, without having to alt-tab or interrupt gameplay.

## Supported Games

- Dark Souls 1
- Dark Souls 2
- Dark Souls 3
- Elden Ring
- Sekiro: Shadows Die Twice

## Features

- **Global hotkeys** — save and load without leaving the game
- **4 quick save slots** per game
- **Auto-detects save file location** on both Windows and Linux (including Steam/Proton)
- **Remembers** your last selected game and save path between sessions
- **Minimizes to system tray**
- **Backup** — automatically backs up your save before loading a slot

## How to Use

1. Launch **SaveSouls** and select your game from the dropdown
2. The save file path should be detected automatically. If not, click **Pick Souls file** to locate it manually
3. Launch your game and load into your character
4. **Before using any hotkeys**, go into the game's menu and exit back to the game. This triggers the game's auto-save, indicated by the **bonfire or flame icon** that appears in the corner of the screen (top-left or top-right depending on the game). Wait for it to disappear before proceeding
5. You can now use **F1–F4** to quick save your current progress into a slot at any time while playing

### Loading a saved slot

Loading works differently from saving and requires going through the main menu:

1. Quit to the main menu from inside the game
2. When you see the **"Continue"** option on the main menu, press the load hotkey (**F5–F8**) for the slot you want to restore
3. Press **"Continue"** — the game will load from exactly where you saved that slot

> **Why the main menu?** The game locks its save file while you are actively playing. Loading a slot while in-game would have no effect. Replacing the save file at the main menu screen, just before hitting Continue, is the correct moment for the game to pick it up.

> **Why step 4?** FromSoftware games write your save file when you rest at a bonfire or open/close the menu. SaveSouls copies that file — if the game hasn't written it yet, the copy will be empty or outdated. Always make sure the flame icon has finished animating before saving a slot for the first time.

> **Elden Ring** — not personally tested, but it follows the same save file pattern as the other games and should work the same way.

## Hotkeys

| Key | Action |
|-----|--------|
| F1  | Quick Save slot 1 |
| F2  | Quick Save slot 2 |
| F3  | Quick Save slot 3 |
| F4  | Quick Save slot 4 |
| F5  | Load slot 1 |
| F6  | Load slot 2 |
| F7  | Load slot 3 |
| F8  | Load slot 4 |

## Download

Go to the [Releases](../../releases) page and download the appropriate binary for your platform.

| File | Platform | Requirement |
|------|----------|-------------|
| `SaveSouls-win-x64.exe` | Windows x64 | None |
| `SaveSouls-linux-x64` | Linux x64 | None |
| `SaveSouls-mac-intel` | macOS Intel | None |
| `SaveSouls-mac-arm` | macOS Apple Silicon | None |
| `SaveSouls-win-x64-requires-dotnet.exe` | Windows x64 | [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) |
| `SaveSouls-linux-x64-requires-dotnet` | Linux x64 | [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) |
| `SaveSouls-mac-intel-requires-dotnet` | macOS Intel | [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) |
| `SaveSouls-mac-arm-requires-dotnet` | macOS Apple Silicon | [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) |

### Which version should I download?

There are two variants for each platform:

**Self-contained (~120 MB)**
The larger files bundle the entire .NET 10 runtime and all dependencies inside the executable. This means you can just download and run — no installation required, no prior setup needed. If you are unsure which to pick, **this is the one to choose**.

**Framework-dependent (~21-29 MB)**
The smaller files contain only the application code. They are significantly smaller because they rely on .NET 10 already being installed on your machine. If you are a developer or already have .NET 10 installed, this version will work fine and is much faster to download. You can get .NET 10 from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0).

### Linux
After downloading, make the file executable before running:
```bash
chmod +x SaveSouls-linux-x64
./SaveSouls-linux-x64
```

### macOS
macOS will warn about an unidentified developer on first launch. To bypass:
```bash
xattr -rd com.apple.quarantine SaveSouls-mac-intel
```
Or right-click the file → **Open** → **Open** in the dialog.

## Notes

- On Linux, global hotkeys use **X11/XWayland**. Under native Wayland the hotkeys may not work depending on your compositor.
- Save files are backed up automatically to a `Backup/` folder next to the executable before any load operation.
- Settings are stored in:
  - **Windows:** `%APPDATA%\SaveSouls\settings.json`
  - **Linux:** `~/.config/SaveSouls/settings.json`

## Built With

- [Avalonia UI](https://avaloniaui.net/) — cross-platform UI framework for .NET
- [SharpHook](https://github.com/TolikPylypchuk/SharpHook) — cross-platform global keyboard hooks
- [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)

## License

MIT
