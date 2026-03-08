using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Grabthebirdy.SaveSouls.Main.Models;
using Grabthebirdy.SaveSouls.Main.Services;
using SharpHook;
using SharpHook.Native;

namespace Grabthebirdy.SaveSouls.Main
{
    public partial class SaveSoulsWindow : Window
    {
        private TaskPoolGlobalHook? _globalHook;
        private TrayIcon? _trayIcon;
        private GameItem? _selectedGame;
        private string _gameSaveFilename = "";
        private readonly QuickSaveLoad[] _quickSaveLoadArray;
        private readonly AppSettings _settings;

        public SaveSoulsWindow()
        {
            InitializeComponent();
            _settings = SettingsService.Load();
            _quickSaveLoadArray = BuildQuickSaveLoadArray();
            InitializeComboBox();
            ScanForLatestSave();
            InitializeKeyboardHooks();
            InitializeTrayIcon();
        }

        // -------------------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------------------

        private static QuickSaveLoad[] BuildQuickSaveLoadArray()
        {
            string baseDir = AppContext.BaseDirectory;
            return
            [
                new QuickSaveLoad { Number = 1, Folder = Path.Combine(baseDir, "QuickSaveFolder1"), SaveKey = KeyCode.VcF1, LoadKey = KeyCode.VcF5 },
                new QuickSaveLoad { Number = 2, Folder = Path.Combine(baseDir, "QuickSaveFolder2"), SaveKey = KeyCode.VcF2, LoadKey = KeyCode.VcF6 },
                new QuickSaveLoad { Number = 3, Folder = Path.Combine(baseDir, "QuickSaveFolder3"), SaveKey = KeyCode.VcF3, LoadKey = KeyCode.VcF7 },
                new QuickSaveLoad { Number = 4, Folder = Path.Combine(baseDir, "QuickSaveFolder4"), SaveKey = KeyCode.VcF4, LoadKey = KeyCode.VcF8 },
            ];
        }

        private void InitializeComboBox()
        {
            GameItem[] items =
            [
                new GameItem { Text = "Dark Souls 1",  FolderName = "NBGI",         SaveFileName = "DRAKS0005.sl2",  UseDocuments = true, SteamAppId = "570940"  },
                new GameItem { Text = "Dark Souls 2",  FolderName = "DarkSoulsII",  SaveFileName = "DARKSII0000.sl2",                     SteamAppId = "335300"  },
                new GameItem { Text = "Dark Souls 3",  FolderName = "DarkSoulsIII", SaveFileName = "DS30000.sl2",                         SteamAppId = "374320"  },
                new GameItem { Text = "Elden Ring",    FolderName = "EldenRing",    SaveFileName = "ER0000.sl2",                          SteamAppId = "1245620" },
                new GameItem { Text = "Sekiro",        FolderName = "Sekiro",       SaveFileName = "S0000.sl2",                           SteamAppId = "814380"  },
            ];

            ComboBox1.ItemsSource = items;
            ComboBox1.SelectedIndex = 2;
        }

        private void InitializeKeyboardHooks()
        {
            _globalHook = new TaskPoolGlobalHook();
            _globalHook.KeyPressed += OnKeyPressed;
            // RunAsync starts the hook on a background thread; the returned Task
            // completes only when the hook is stopped (on Dispose).
            _ = _globalHook.RunAsync();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TrayIcon { ToolTipText = "Save Souls", IsVisible = false };

             _trayIcon.Icon = new WindowIcon(LoadAsset("avares://SaveSouls/Assets/bonfire-icon.ico"));

            var menu = new NativeMenu();

            var showItem = new NativeMenuItem("Show");
            showItem.Click += (_, _) => Dispatcher.UIThread.Post(RestoreFromTray);

            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) => Dispatcher.UIThread.Post(Close);

            menu.Add(showItem);
            menu.Add(exitItem);
            _trayIcon.Menu = menu;
            _trayIcon.Clicked += (_, _) => Dispatcher.UIThread.Post(RestoreFromTray);

            TrayIcon.SetIcons(Avalonia.Application.Current!, new TrayIcons { _trayIcon });
        }

        // -------------------------------------------------------------------------
        // Tray icon / window state
        // -------------------------------------------------------------------------

        public void RestoreFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (_trayIcon != null)
                _trayIcon.IsVisible = false;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == WindowStateProperty && WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
                Hide();
                if (_trayIcon != null)
                    _trayIcon.IsVisible = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _globalHook?.Dispose();
            _trayIcon?.Dispose();
            base.OnClosed(e);
        }

        // -------------------------------------------------------------------------
        // UI event handlers
        // -------------------------------------------------------------------------

        private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ComboBox1.SelectedItem is not GameItem item)
                return;

            _selectedGame = item;
            _gameSaveFilename = item.SaveFileName;
            
            Background = new ImageBrush(LoadAsset(item.Text switch
            {
                "Dark Souls 2" => "avares://SaveSouls/Assets/dark-souls-2.jpg",
                "Dark Souls 3" => "avares://SaveSouls/Assets/dark-souls-3.jpg",
                "Elden Ring"   => "avares://SaveSouls/Assets/elden-ring.jpg",
                "Sekiro"       => "avares://SaveSouls/Assets/sekiro.jpg",
                "Dark Souls 1" or
                _              => "avares://SaveSouls/Assets/dark-souls-1.jpg"
            }))
            {
                Stretch = Stretch.None,
                AlignmentY = AlignmentY.Top
            };

            // 1. Prefer the path the user previously confirmed for this game
            if (_settings.GameSavePaths.TryGetValue(item.Text, out string? savedPath) && File.Exists(savedPath))
            {
                txtGameSavePath.Text = savedPath;
                Log($"Restored save path for {item.Text}: {savedPath}");
                return;
            }

            // 2. Fall back to auto-discovery
            string baseFolder = item.UseDocuments
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string gameFolder = Path.Combine(baseFolder, item.FolderName);
            string? savePath = DiscoverSavePath(gameFolder, item.SaveFileName);

            if (savePath != null)
            {
                txtGameSavePath.Text = savePath;
                SavePathForCurrentGame(savePath);
                Log($"Auto-found save path for {item.Text}: {savePath}");
                return;
            }

            // 3. On Linux, try Proton (Steam) paths
            foreach (string protonFolder in GetProtonGameFolders(item))
            {
                savePath = DiscoverSavePath(protonFolder, item.SaveFileName);
                if (savePath != null)
                {
                    txtGameSavePath.Text = savePath;
                    SavePathForCurrentGame(savePath);
                    Log($"Auto-found Proton save path for {item.Text}: {savePath}");
                    return;
                }
            }

            Log($"Could not auto-find save path for {item.Text}. Please pick it manually.");
        }

        private async void BtnSelectFolder_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Souls Save File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Soul files") { Patterns = ["*.sl2"] },
                    new FilePickerFileType("All files")  { Patterns = ["*"] },
                ]
            });

            if (files.Count > 0)
            {
                string path = files[0].Path.LocalPath;
                txtGameSavePath.Text = path;
                _gameSaveFilename = Path.GetFileName(path);
                SavePathForCurrentGame(path);
                Log($"Selected file: {path}");
            }
        }

        // -------------------------------------------------------------------------
        // Global keyboard hook
        // -------------------------------------------------------------------------

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            foreach (var slot in _quickSaveLoadArray)
            {
                if (e.Data.KeyCode == slot.SaveKey)
                {
                    e.SuppressEvent = true;
                    var captured = slot;
                    Dispatcher.UIThread.Post(() => QuickSave(captured));
                    return;
                }

                if (e.Data.KeyCode == slot.LoadKey)
                {
                    e.SuppressEvent = true;
                    var captured = slot;
                    Dispatcher.UIThread.Post(() => QuickLoad(captured));
                    return;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Save / Load
        // -------------------------------------------------------------------------

        private void QuickSave(QuickSaveLoad slot)
        {
            string sourcePath = txtGameSavePath.Text ?? "";
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                Log("No save path set. Please select a save file first.");
                return;
            }

            string destPath = Path.Combine(slot.Folder, _gameSaveFilename);
            try
            {
                Directory.CreateDirectory(slot.Folder);
                File.Copy(sourcePath, destPath, overwrite: true);
                Log($"Game Saved (QuickSave {slot.Number})");
                ScanForLatestSave();
            }
            catch (Exception ex)
            {
                Log($"QuickSave {slot.Number} failed: {ex.Message}");
            }
        }

        private void QuickLoad(QuickSaveLoad slot)
        {
            string destPath = txtGameSavePath.Text ?? "";
            if (string.IsNullOrWhiteSpace(destPath))
            {
                Log("No save path set. Please select a save file first.");
                return;
            }

            string sourcePath = Path.Combine(slot.Folder, _gameSaveFilename);
            if (!File.Exists(sourcePath))
            {
                Log($"QuickSave {slot.Number} slot is empty.");
                return;
            }

            BackupOriginal(destPath);

            try
            {
                File.Copy(sourcePath, destPath, overwrite: true);
                Log($"Game Loaded (QuickLoad {slot.Number})");
            }
            catch (Exception ex)
            {
                Log($"QuickLoad {slot.Number} failed: {ex.Message}");
            }
        }

        private void BackupOriginal(string sourcePath)
        {
            if (!File.Exists(sourcePath)) return;

            string backupFolder = Path.Combine(AppContext.BaseDirectory, "Backup");
            string backupPath = Path.Combine(backupFolder, _gameSaveFilename);

            try
            {
                DateTime sourceWrite = File.GetLastWriteTime(sourcePath);
                DateTime backupWrite = File.Exists(backupPath)
                    ? File.GetLastWriteTime(backupPath)
                    : DateTime.MinValue;

                if (sourceWrite != backupWrite)
                {
                    Directory.CreateDirectory(backupFolder);
                    File.Copy(sourcePath, backupPath, overwrite: true);
                }
            }
            catch { }
        }

        // -------------------------------------------------------------------------
        // Save scan
        // -------------------------------------------------------------------------

        private void ScanForLatestSave()
        {
            var sortedList = new SortedList<DateTime, string>();

            foreach (var slot in _quickSaveLoadArray)
            {
                if (!Directory.Exists(slot.Folder)) continue;

                string[] files = Directory.GetFiles(slot.Folder, "*.sl2");
                if (files.Length == 0) continue;

                DateTime latest = files.Max(f => File.GetLastWriteTimeUtc(f));

                // Ensure unique keys (timestamps could theoretically collide)
                while (sortedList.ContainsKey(latest))
                    latest = latest.AddTicks(1);

                sortedList.Add(latest, $"{slot.Number} (F{slot.Number + 4})");
            }

            lbl_LatestSave.Content = sortedList.Count > 0
                ? string.Join(" -> ", sortedList.Values.Reverse())
                : "There is no save point";
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static Bitmap LoadAsset(string uri)
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            return new Bitmap(stream);
        }

        private void SavePathForCurrentGame(string path)
        {
            if (_selectedGame == null) return;
            _settings.GameSavePaths[_selectedGame.Text] = path;
            SettingsService.Save(_settings);
        }

        /// <summary>
        /// Returns the common Steam root directories to check on Linux.
        /// </summary>
        private static IEnumerable<string> GetSteamRoots()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".steam", "steam");
            yield return Path.Combine(home, ".local", "share", "Steam");
        }

        /// <summary>
        /// Returns potential Proton game save folders for the given game on Linux.
        /// </summary>
        private static IEnumerable<string> GetProtonGameFolders(GameItem item)
        {
            if (!OperatingSystem.IsLinux() || string.IsNullOrEmpty(item.SteamAppId))
                yield break;

            string protonUserPath = Path.Combine("steamapps", "compatdata", item.SteamAppId, "pfx", "drive_c", "users", "steamuser");
            string saveSubFolder = item.UseDocuments
                ? Path.Combine("Documents", item.FolderName)
                : Path.Combine("AppData", "Roaming", item.FolderName);

            foreach (string steamRoot in GetSteamRoots())
            {
                string folder = Path.Combine(steamRoot, protonUserPath, saveSubFolder);
                yield return folder;
            }
        }

        /// <summary>
        /// Looks for the save file directly in the game folder, then one level deep
        /// (to handle Steam ID sub-folders like 0000000000000001).
        /// </summary>
        private static string? DiscoverSavePath(string gameFolder, string fileName)
        {
            if (!Directory.Exists(gameFolder))
                return null;

            string direct = Path.Combine(gameFolder, fileName);
            if (File.Exists(direct))
                return direct;

            foreach (string subDir in Directory.GetDirectories(gameFolder))
            {
                string candidate = Path.Combine(subDir, fileName);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private void Log(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                rTxtLogContent.Text += $"[{DateTime.Now}] {message}{Environment.NewLine}";
                rTxtLog.ScrollToEnd();
            });
        }
    }
}
