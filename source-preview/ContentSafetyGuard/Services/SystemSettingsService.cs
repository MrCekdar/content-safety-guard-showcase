using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Principal;
using System.Text.Json;

namespace ContentSafetyGuard.Services
{
    public class SystemSettingsService
    {
        private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunRegistryName = "ContentSafetyGuard";

        private readonly string settingsFolderPath;
        private readonly string settingsFilePath;

        public event EventHandler? StateChanged;

        public SystemSettingsState CurrentState { get; private set; }

        public SystemSettingsService()
        {
            settingsFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ContentSafetyGuard");

            settingsFilePath = Path.Combine(settingsFolderPath, "system_settings.json");
            CurrentState = LoadStateFromFile();

            ApplyStartupRegistration();
        }

        public bool IsCurrentProcessAdministrator()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void SetStartWithWindowsEnabled(bool enabled)
        {
            CurrentState.StartWithWindowsEnabled = enabled;
            SaveAndNotify();
            ApplyStartupRegistration();
        }

        public void SetRunAsAdministratorEnabled(bool enabled)
        {
            CurrentState.RunAsAdministratorEnabled = enabled;
            SaveAndNotify();
        }

        public void SetRunMinimizedEnabled(bool enabled)
        {
            CurrentState.RunMinimizedEnabled = enabled;
            SaveAndNotify();
            ApplyStartupRegistration();
        }

        public void SetShowTrayIconEnabled(bool enabled)
        {
            CurrentState.ShowTrayIconEnabled = enabled;
            SaveAndNotify();
        }

        private SystemSettingsState LoadStateFromFile()
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    return new SystemSettingsState();
                }

                string json = File.ReadAllText(settingsFilePath);
                SystemSettingsState? state = JsonSerializer.Deserialize<SystemSettingsState>(json);

                return state ?? new SystemSettingsState();
            }
            catch
            {
                return new SystemSettingsState();
            }
        }

        private void SaveAndNotify()
        {
            try
            {
                Directory.CreateDirectory(settingsFolderPath);

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(CurrentState, options);
                File.WriteAllText(settingsFilePath, json);
            }
            catch
            {
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyStartupRegistration()
        {
            try
            {
                using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);

                if (runKey == null)
                {
                    return;
                }

                if (!CurrentState.StartWithWindowsEnabled)
                {
                    runKey.DeleteValue(RunRegistryName, throwOnMissingValue: false);
                    return;
                }

                string executablePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
                string command = $"\"{executablePath}\"";

                if (CurrentState.RunMinimizedEnabled)
                {
                    command += " --minimized";
                }

                runKey.SetValue(RunRegistryName, command);
            }
            catch
            {
            }
        }
    }
}
