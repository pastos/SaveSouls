# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Run in development
dotnet run --project Grabthebirdy.SaveSouls.Main/Grabthebirdy.SaveSouls.Main.csproj

# Build
dotnet build

# Publish self-contained single file (example: linux-x64)
dotnet publish Grabthebirdy.SaveSouls.Main/Grabthebirdy.SaveSouls.Main.csproj /p:PublishProfile=linux-x64 -o ./out

# Available publish profiles: win-x64, linux-x64, osx-x64, osx-arm64
# Framework-dependent variants: win-x64-fd, linux-x64-fd, osx-x64-fd, osx-arm64-fd
```

There are no automated tests in this project.

## Architecture

Single-project Avalonia UI desktop app targeting `net10.0`. The assembly name is `SaveSouls` (not the namespace — important for `avares://` asset URIs).

### Key files

- `SaveSoulsWindow.axaml` / `SaveSoulsWindow.axaml.cs` — the only window; all UI logic lives here (no MVVM, code-behind only)
- `Models/GameItem.cs` — represents a game in the dropdown (name, save folder, filename, Steam App ID)
- `Models/QuickSaveLoad.cs` — represents one quick-save slot (folder path, SharpHook key codes)
- `Models/AppSettings.cs` — persisted settings (last selected game, per-game save paths)
- `Services/SettingsService.cs` — loads/saves `settings.json` from `AppData/SaveSouls/` (cross-platform)

### How the core flow works

1. **Startup** — settings loaded, ComboBox populated, last selected game restored, keyboard hook started, tray icon initialized
2. **Game selection** — `ComboBox_SelectionChanged` tries to find the save file in order: saved path → native OS path → Proton path (Linux only). Found path is persisted immediately
3. **QuickSave (F1–F4)** — copies the `.sl2` file from `txtGameSavePath` to `QuickSaveFolderN/` next to the executable
4. **QuickLoad (F5–F8)** — backs up current save to `Backup/`, then copies the slot file back to `txtGameSavePath`. **Must be triggered from the game's main menu screen**, not during gameplay
5. **Keyboard hook** — runs on a `TaskPoolGlobalHook` background thread (SharpHook); dispatches save/load to UI thread via `Dispatcher.UIThread.Post`
6. **Tray** — minimize hides the window and sets `ShowInTaskbar=false`; tray click or menu "Show" restores it

### Assets

Assets live in `Assets/` and are embedded via `<AvaloniaResource Include="Assets/**" />` plus explicit entries in the csproj. Asset URIs must use the assembly name:
```
avares://SaveSouls/Assets/filename.ext
```
Use `AssetLoader.Open(new Uri(...))` in C# code — the `/Assets/path` shorthand only works in AXAML.

### Proton (Linux Steam) path detection

`GetProtonGameFolders()` in `SaveSoulsWindow.axaml.cs` checks:
- `~/.steam/steam/steamapps/compatdata/<AppId>/pfx/drive_c/users/steamuser/AppData/Roaming/<FolderName>/`
- `~/.local/share/Steam/steamapps/compatdata/<AppId>/pfx/drive_c/users/steamuser/AppData/Roaming/<FolderName>/`

Only runs on Linux (`OperatingSystem.IsLinux()`). Steam App IDs are defined in `InitializeComboBox`.

### Releases

GitHub Actions (`.github/workflows/release.yml`) triggers on version tags (`v*`) and publishes 8 binaries — self-contained and framework-dependent for each platform — attached to the GitHub Release.
