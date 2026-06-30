using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;

/*
        Einstellungen für Blocking Behavior verwalten
        → beim Start laden
        → bei Änderung speichern
        → andere Klassen informieren, wenn sich etwas geändert hat
*/

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.BlockingBehavior
{
    public class BlockingBehaviorSettingsStateService
    {

        private readonly string settingsFolderPath;
        private readonly string settingsFilePath;

        public event EventHandler? StateChanged;

        public BlockingBehaviorSettingsState CurrentState { get; private set; } 

        public BlockingBehaviorSettingsStateService()
        {
            settingsFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ContentSafetyGuard");

            settingsFilePath = Path.Combine(settingsFolderPath, "blocking_behavior_settings.json");

            CurrentState = LoadStateFromFile();
        }

        private BlockingBehaviorSettingsState LoadStateFromFile()
        {
            try
            {
                if (!File.Exists(settingsFilePath)) // Wenn Datei nicht existiert nehme bitte den Standard Wert
                {
                    return new BlockingBehaviorSettingsState(); // Der StandardWert hier ist auf 15 gesetzt
                }

                string json = File.ReadAllText(settingsFilePath); // Ansonsten lies dir die Datei durch

                BlockingBehaviorSettingsState? state =
                    JsonSerializer.Deserialize<BlockingBehaviorSettingsState>(json); // und wandel die Json in ein Objekt um

                return state ?? new BlockingBehaviorSettingsState(); // Wenn nicht gefunden lade Standart auf 15 gesetzt
            }
            catch
            {
                return new BlockingBehaviorSettingsState();
            }
        }


        public void Update(BlockingBehaviorSettingsState state)
        {
            CurrentState = state;

            SaveStateToFile();

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetUnlockDelaySeconds(int seconds)
        {
            CurrentState.UnlockDelaySeconds = seconds;
            SaveStateToFile();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetCloseProgramWhenDetected(bool enabled)
        {
            CurrentState.CloseProgramWhenDetected = enabled;
            SaveStateToFile();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetSoundAlertsEnabled(bool enabled)
        {
            CurrentState.SoundAlertsEnabled = enabled;
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
                    // Später optional Debug.WriteLine(...) einbauen.
                }
            }


    }
}
