using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode
{

    public partial class ExceptionOverlay : UserControl
    {
        private ICollectionView? programListView;


        private const string SettingsFolderPath = @"C:\ProgramData\ContentSafetyGuard";
        private static readonly string BlockedProgramsPath = System.IO.Path.Combine(SettingsFolderPath, "blocked-programs.json");

        public ObservableCollection<ExceptionLogic> ExceptionPrograms { get; } = new ObservableCollection<ExceptionLogic>(); 

        public ExceptionOverlay()
        {
            InitializeComponent();

            // Populate the observable UI collection after XAML controls have been initialized.
            foreach (ExceptionLogic program in LoadInstalledPrograms())
            {
                ExceptionPrograms.Add(program);
            }

            LoadSavedBlockedPrograms();

            Programs.ItemsSource = ExceptionPrograms;
            programListView = CollectionViewSource.GetDefaultView(ExceptionPrograms);
            programListView.Filter = FilterProgram;
        }

        private bool FilterProgram(object item)
        {
            if (item is not ExceptionLogic program)
            {
                return false;
            }

            string searchText = SearchProgramsTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            return program.ProgramName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void SearchProgramsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            programListView?.Refresh();
        }

        // Json Datei lesen speichern und Laden
        public void SaveBlockedPrograms(List<string> blockedPrograms)
        {
            Directory.CreateDirectory(SettingsFolderPath);

            string json = JsonSerializer.Serialize(blockedPrograms, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(BlockedProgramsPath, json);
        }


        public void LoadSavedBlockedPrograms()
        {
            if (!File.Exists(BlockedProgramsPath))
            {
                return;
            }

            string json = File.ReadAllText(BlockedProgramsPath);
            List<string>? savedPaths = JsonSerializer.Deserialize<List<string>>(json);

            if (savedPaths == null)
            {
                return;
            }

            HashSet<string> blockedPaths = new HashSet<string>(
                savedPaths,
                StringComparer.OrdinalIgnoreCase);

            // Schritt 1: Programme markieren, die bereits in der Liste existieren.
            foreach (ExceptionLogic program in ExceptionPrograms)
            {
                program.IsBlocked = blockedPaths.Contains(program.ExePath);
            }

            // Schritt 2: Gespeicherte Pfade hinzufügen, die nicht automatisch gefunden wurden.
            foreach (string savedPath in blockedPaths)
            {
                bool alreadyExists = ExceptionPrograms.Any(program =>
                    string.Equals(program.ExePath, savedPath, StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                {
                    continue;
                }

                if (!File.Exists(savedPath))
                {
                    continue;
                }

                ExceptionPrograms.Add(new ExceptionLogic
                {
                    ProgramName = System.IO.Path.GetFileNameWithoutExtension(savedPath),
                    IconPath = savedPath,
                    ExePath = savedPath,
                    IsBlocked = true
                });
            }
        }



        private ObservableCollection<ExceptionLogic> LoadInstalledPrograms()
        {
            ObservableCollection<ExceptionLogic> programs = new ObservableCollection<ExceptionLogic>();
            HashSet<string> addedProgramNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            LoadProgramsFromStartMenu(programs, addedProgramNames, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu));
            LoadProgramsFromStartMenu(programs, addedProgramNames, Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));

            LoadProgramsFromRegistry(programs, addedProgramNames, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            LoadProgramsFromRegistry(programs, addedProgramNames, Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            LoadProgramsFromRegistry(programs, addedProgramNames, Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

            return programs;
        }


        private bool ShouldHideProgram(string displayName)
        {
            string name = displayName.ToLowerInvariant();

            return name.Contains("driver")
                || name.Contains("treiber")
                || name.Contains("microsoft edge")
                || name.Contains("redistributable")
                || name.Contains("runtime")
                || name.Contains("sdk")
                || name.Contains("development kit")
                || name.Contains("software development kit")
                || name.Contains("update")
                || name.Contains("hotfix")
                || name.Contains("security update")
                || name.Contains("service pack")
                || name.Contains("package")
                || name.Contains("pack")
                || name.Contains("tools")
                || name.Contains("components")
                || name.Contains("component")
                || name.Contains("systemsoftware")
                || name.Contains("system software")
                || name.Contains("system-clr")
                || name.Contains("clr-typen")
                || name.Contains("microsoft visual c++")
                || name.Contains(".net runtime")
                || name.Contains(".net sdk")
                || name.Contains("windows driver")
                || name.Contains("windows software development kit")
                || name.Contains("physx");
        }


        private void LoadProgramsFromRegistry(
            ObservableCollection<ExceptionLogic> programs,
            HashSet<string> addedProgramNames,
            RegistryKey rootKey, string path){
            using RegistryKey? uninstallKey = rootKey.OpenSubKey(path);

            if (uninstallKey == null)
            {
                return;
            }

            foreach (string subKeyName in uninstallKey.GetSubKeyNames())
            {
                using RegistryKey? appKey = uninstallKey.OpenSubKey(subKeyName);

                if (appKey == null)
                {
                    continue;
                }

                string? displayName = appKey.GetValue("DisplayName") as string;

           

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                // System components and release/update entries are not normal launchable applications.
                object? systemComponent = appKey.GetValue("SystemComponent");

                if (systemComponent is int systemComponentValue && systemComponentValue == 1)
                {
                    continue;
                }

                string? releaseType = appKey.GetValue("ReleaseType") as string;

                if (!string.IsNullOrWhiteSpace(releaseType))
                {
                    continue;
                }

                if (ShouldHideProgram(displayName))
                {
                    continue;
                }
                string? displayIcon = appKey.GetValue("DisplayIcon") as string;
                string executablePath = GetExecutablePathFromRegistry(appKey, displayIcon);

                if (!EndsWithExecutable(executablePath))
                {
                    continue;
                }

                // HashSet.Add returns false when the display name was already loaded from another hive.
                if (!addedProgramNames.Add(displayName))
                {
                    continue;
                }

                programs.Add(new ExceptionLogic
                {
                    IconPath = displayIcon ?? "",
                    ProgramName = displayName,
                    ExePath = executablePath,
                    IsBlocked = false
                });
            }
        }

        private void LoadProgramsFromStartMenu(
                    ObservableCollection<ExceptionLogic> programs,
                    HashSet<string> addedProgramNames, string startMenuPath){

            if (!Directory.Exists(startMenuPath))
            {
                return;
            }

            EnumerationOptions shortcutSearchOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            foreach (string shortcutPath in Directory.EnumerateFiles(startMenuPath, "*.lnk", shortcutSearchOptions))
            {
                string programName = Path.GetFileNameWithoutExtension(shortcutPath);
                string? shortcutTargetPath = TryGetShortcutTargetPath(shortcutPath);

                if (string.IsNullOrWhiteSpace(programName))
                {
                    continue;
                }

                if (ShouldHideProgram(programName))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(shortcutTargetPath) ||
                    !shortcutTargetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!addedProgramNames.Add(programName))
                {
                    continue;
                }

                programs.Add(new ExceptionLogic
                {
                    ProgramName = programName,
                    IconPath = shortcutTargetPath,
                    ExePath = shortcutTargetPath, 
                    IsBlocked = false
                });

            }
        }

        private string GetExecutablePathFromRegistry(RegistryKey appKey, string? displayIcon)
        {
            if (EndsWithExecutable(displayIcon))
            {
                return displayIcon ?? "";
            }

            string? installLocation = appKey.GetValue("InstallLocation") as string;

            if (string.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
            {
                return "";
            }

            string? executablePath = Directory
                .GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path =>
                    !Path.GetFileName(path).Contains("unins", StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetFileName(path).Contains("setup", StringComparison.OrdinalIgnoreCase));

            return executablePath ?? "";
        }

        private bool EndsWithExecutable(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string cleanedPath = Environment.ExpandEnvironmentVariables(path.Trim());

            if (cleanedPath.StartsWith("\""))
            {
                int closingQuoteIndex = cleanedPath.IndexOf('"', 1);

                if (closingQuoteIndex > 1)
                {
                    cleanedPath = cleanedPath.Substring(1, closingQuoteIndex - 1);
                }
            }
            else
            {
                cleanedPath = cleanedPath.Split(',')[0].Trim();
            }

            return cleanedPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }

        private string? TryGetShortcutTargetPath(string shortcutPath)
        {
            object? shellObject = null;
            object? shortcutObject = null;

            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");

                if (shellType == null)
                {
                    return null;
                }

                shellObject = Activator.CreateInstance(shellType);

                if (shellObject == null)
                {
                    return null;
                }

                dynamic shell = shellObject;
                shortcutObject = shell.CreateShortcut(shortcutPath);
                dynamic shortcut = shortcutObject;

                return shortcut.TargetPath as string;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (shortcutObject != null && Marshal.IsComObject(shortcutObject))
                {
                    Marshal.FinalReleaseComObject(shortcutObject);
                }

                if (shellObject != null && Marshal.IsComObject(shellObject))
                {
                    Marshal.FinalReleaseComObject(shellObject);
                }
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select executable",
                Filter = "Executable files (*.exe)|*.exe",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string exePath = dialog.FileName;

            ExceptionLogic? existingProgram = ExceptionPrograms.FirstOrDefault(program =>
                string.Equals(program.ExePath, exePath, StringComparison.OrdinalIgnoreCase));

            if (existingProgram != null)
            {
                existingProgram.IsBlocked = true;
                Programs.ScrollIntoView(existingProgram);
                return;
            }

            ExceptionLogic customProgram = new ExceptionLogic
            {
                ProgramName = System.IO.Path.GetFileNameWithoutExtension(exePath),
                IconPath = exePath,
                ExePath = exePath,
                IsBlocked = true
            };

            ExceptionPrograms.Add(customProgram);
            Programs.ScrollIntoView(customProgram);
        }
        // "Das x Symbol in ExpectionOverlay" 
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
            OverlayClosed?.Invoke(this, EventArgs.Empty);
        }

        // "Das Done Symbol in ExpectionOverlay"
        public List<string> GetBlockedProgramPaths()
        {
            return ExceptionPrograms
                .Where(program => program.IsBlocked)
                .Where(program => !string.IsNullOrWhiteSpace(program.ExePath))
                .Select(program => program.ExePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        public event EventHandler? BlockedProgramsSaved;
        public event EventHandler? OverlayClosed;

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            SaveBlockedPrograms(GetBlockedProgramPaths());

            BlockedProgramsSaved?.Invoke(this, EventArgs.Empty);

            Visibility = Visibility.Collapsed;
            OverlayClosed?.Invoke(this, EventArgs.Empty);
        }

    }
}
