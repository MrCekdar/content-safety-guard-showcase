using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode
{
    public partial class FocusModeCard : UserControl
    {
        private const string SettingsFolderPath = @"C:\ProgramData\ContentSafetyGuard";
        private static readonly string BlockedProgramsPath = Path.Combine(SettingsFolderPath, "blocked-programs.json");
        private static readonly string FocusModeSettingsPath = Path.Combine(SettingsFolderPath, "focus-mode-settings.json");

        public event EventHandler? ManageExceptionsRequested;

        private readonly ProgramBlockService blockservice = new ProgramBlockService();
        private readonly BreakReminderService breakReminderService = new BreakReminderService();
        private readonly WindowsNotificationService notificationService = new WindowsNotificationService();
        private readonly DispatcherTimer sessionTimer = new DispatcherTimer();

        private bool isLoadingSettings = true;

        public FocusModeCard()
        {
            InitializeComponent();

            Loaded += FocusModeCard_Loaded;
            sessionTimer.Tick += SessionTimer_Tick;
            breakReminderService.ReminderDue += BreakReminderService_ReminderDue;
        }

        private void FocusModeCard_Loaded(object sender, RoutedEventArgs e)
        {
            isLoadingSettings = true;
            ApplySettingsToUi(LoadFocusModeSettings());
            isLoadingSettings = false;

            RefreshProgramBlocksIfFocusMode();
            UpdateFocusTimers();
        }

        private void Manage_Exceptions_Click(object sender, RoutedEventArgs e)
        {
            ManageExceptionsRequested?.Invoke(this, EventArgs.Empty);
        }

        private List<string> LoadBlockedProgramPaths()
        {
            if (!File.Exists(BlockedProgramsPath))
            {
                return new List<string>();
            }

            try
            {
                string json = File.ReadAllText(BlockedProgramsPath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private FocusModeSettings LoadFocusModeSettings()
        {
            if (!File.Exists(FocusModeSettingsPath))
            {
                return new FocusModeSettings();
            }

            try
            {
                string json = File.ReadAllText(FocusModeSettingsPath);
                return JsonSerializer.Deserialize<FocusModeSettings>(json) ?? new FocusModeSettings();
            }
            catch
            {
                return new FocusModeSettings();
            }
        }

        private void SaveFocusModeSettings()
        {
            Directory.CreateDirectory(SettingsFolderPath);

            FocusModeSettings settings = ReadSettingsFromUi();
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(FocusModeSettingsPath, json);
        }

        private FocusModeSettings ReadSettingsFromUi()
        {
            return new FocusModeSettings
            {
                IsFocusModeEnabled = FocusModeToggle.IsChecked == true,
                SessionDurationMinutes = GetSelectedMinutes(SessionDurationComboBox, 5),
                BreakRemindersEnabled = BreakReminderToggle.IsChecked == true,
                BreakReminderIntervalMinutes = GetSelectedMinutes(BreakReminderIntervalComboBox, 5)
            };
        }

        private void ApplySettingsToUi(FocusModeSettings settings)
        {
            SelectMinutes(SessionDurationComboBox, settings.SessionDurationMinutes);
            SelectMinutes(BreakReminderIntervalComboBox, settings.BreakReminderIntervalMinutes);

            FocusModeToggle.IsChecked = settings.IsFocusModeEnabled;
            BreakReminderToggle.IsChecked = settings.BreakRemindersEnabled;
        }

        private int GetSelectedMinutes(ComboBox comboBox, int fallbackMinutes)
        {
            if (comboBox.SelectedItem is ComboBoxItem item &&
                item.Tag is string tag &&
                int.TryParse(tag, out int minutes))
            {
                return minutes;
            }

            return fallbackMinutes;
        }

        private void SelectMinutes(ComboBox comboBox, int minutes)
        {
            foreach (object itemObject in comboBox.Items)
            {
                if (itemObject is ComboBoxItem item &&
                    item.Tag is string tag &&
                    int.TryParse(tag, out int itemMinutes) &&
                    itemMinutes == minutes)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        public void RefreshProgramBlocksIfFocusMode()
        {
            if (FocusModeToggle.IsChecked == true)
            {
                blockservice.SaveBlockedPrograms(LoadBlockedProgramPaths());
                return;
            }

            blockservice.ClearBlockedPrograms();
        }

        private void UpdateFocusTimers()
        {
            if (FocusModeToggle.IsChecked != true)
            {
                sessionTimer.Stop();
                breakReminderService.Stop();
                return;
            }

            sessionTimer.Stop();
            sessionTimer.Interval = TimeSpan.FromMinutes(GetSelectedMinutes(SessionDurationComboBox, 5));
            sessionTimer.Start();

            if (BreakReminderToggle.IsChecked == true)
            {
                breakReminderService.Start(GetSelectedMinutes(BreakReminderIntervalComboBox, 5));
            }
            else
            {
                breakReminderService.Stop();
            }
        }

        private void FocusModeSetting_Changed(object sender, SelectionChangedEventArgs e)
        {
            SaveSettingsAndRefreshTimers();
        }

        private void BreakReminderToggle_Changed(object sender, RoutedEventArgs e)
        {
            SaveSettingsAndRefreshTimers();
        }

        private void SaveSettingsAndRefreshTimers()
        {
            if (isLoadingSettings)
            {
                return;
            }

            SaveFocusModeSettings();
            UpdateFocusTimers();
        }

        private void FocusModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (isLoadingSettings)
            {
                return;
            }

            SaveFocusModeSettings();
            RefreshProgramBlocksIfFocusMode();
            UpdateFocusTimers();
        }

        private void FocusModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isLoadingSettings)
            {
                return;
            }

            SaveFocusModeSettings();
            RefreshProgramBlocksIfFocusMode();
            UpdateFocusTimers();
        }

        private void SessionTimer_Tick(object? sender, EventArgs e)
        {
            FocusModeToggle.IsChecked = false;
        }

        private void BreakReminderService_ReminderDue(object? sender, EventArgs e)
        {
            if (FocusModeToggle.IsChecked == true && BreakReminderToggle.IsChecked == true)
            {
                notificationService.ShowFocusReminder();
            }
        }
    }
}
