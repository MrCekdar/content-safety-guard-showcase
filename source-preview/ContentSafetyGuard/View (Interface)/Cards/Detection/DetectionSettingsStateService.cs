using System;
using System.IO;
using System.Text.Json;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.Detection
{
    public class DetectionSettingsStateService
    {
        private readonly string settingsFolderPath;
        private readonly string settingsFilePath;

        public event EventHandler? StateChanged;

        public DetectionSettingsState CurrentState { get; private set; }

        public DetectionSettingsStateService()
        {
            settingsFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ContentSafetyGuard");

            settingsFilePath = Path.Combine(
                settingsFolderPath,
                "detection_settings.json");

            bool settingsFileExists = File.Exists(settingsFilePath);

            CurrentState = LoadStateFromFile();
            bool stateWasNormalized = NormalizeLoadedState(CurrentState);

            if (!settingsFileExists || stateWasNormalized)
            {
                SaveStateToFile();
            }
        }

        private DetectionSettingsState LoadStateFromFile()
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    return new DetectionSettingsState();
                }

                string json = File.ReadAllText(settingsFilePath);

                DetectionSettingsState? state =
                    JsonSerializer.Deserialize<DetectionSettingsState>(json);

                return state ?? new DetectionSettingsState();
            }
            catch
            {
                return new DetectionSettingsState();
            }
        }

        private static bool NormalizeLoadedState(DetectionSettingsState state)
        {
            bool changed = false;

            int normalizedNsfwSensitivity = Math.Clamp(state.NsfwSensitivityPercent, 0, 100);
            int normalizedSensitivity = Math.Clamp(state.SensitivityPercent, 0, 100);

            changed |= state.NsfwSensitivityPercent != normalizedNsfwSensitivity;
            changed |= state.SensitivityPercent != normalizedSensitivity;

            state.NsfwSensitivityPercent = normalizedNsfwSensitivity;
            state.SensitivityPercent = normalizedSensitivity;

            return changed;
        }

        public void Update(DetectionSettingsState state)
        {
            CurrentState = state;

            SaveStateToFile();

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SaveStateToFile()
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
        }

        public void SetDetectionMode(DetectionMode mode)
        {
            CurrentState.DetectionMode = mode;

            CurrentState.NsfwSensitivityPercent = mode switch
            {
                DetectionMode.Balanced => 50,
                DetectionMode.Strict => 75,
                DetectionMode.Maximum => 100,
                _ => 50
            };

            CurrentState.SensitivityPercent = CurrentState.NsfwSensitivityPercent;

            SaveStateToFile();

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetSensitivityPercent(int percent)
        {
            SetNsfwSensitivityPercent(percent);
        }

        public void SetNsfwSensitivityPercent(int percent)
        {
            if (percent < 0)
            {
                percent = 0;
            }

            if (percent > 100)
            {
                percent = 100;
            }

            CurrentState.NsfwSensitivityPercent = percent;
            CurrentState.SensitivityPercent = percent;
            CurrentState.DetectionMode = DetectionMode.Custom;

            SaveStateToFile();

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetAiDetectionEnabled(bool enabled)
        {
            CurrentState.AiDetectionEnabled = enabled;
            SaveStateToFile();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetGridScanningEnabled(bool enabled)
        {
            CurrentState.GridScanningEnabled = enabled;
            SaveStateToFile();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetScanFrequency(ScanFrequencyMode scanFrequency)
        {
            CurrentState.ScanFrequency = scanFrequency;
            SaveStateToFile();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
