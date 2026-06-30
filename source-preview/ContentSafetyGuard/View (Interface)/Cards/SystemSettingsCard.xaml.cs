using ContentSafetyGuard.Services;
using System.Windows;
using System.Windows.Controls;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards
{
    public partial class SystemSettingsCard : UserControl
    {
        private SystemSettingsService settingsService = new SystemSettingsService();
        private bool isApplyingState;

        public SystemSettingsCard()
        {
            InitializeComponent();
            ApplyState(settingsService.CurrentState);
        }

        public void SetService(SystemSettingsService service)
        {
            settingsService = service;
            settingsService.StateChanged += SettingsService_StateChanged;
            ApplyState(settingsService.CurrentState);
        }

        private void SettingsService_StateChanged(object? sender, System.EventArgs e)
        {
            ApplyState(settingsService.CurrentState);
        }

        private void ApplyState(SystemSettingsState state)
        {
            isApplyingState = true;

            StartWithWindowsToggle.IsChecked = state.StartWithWindowsEnabled;
            RunAsAdministratorToggle.IsChecked = state.RunAsAdministratorEnabled;
            RunMinimizedToggle.IsChecked = state.RunMinimizedEnabled;
            ShowTrayIconToggle.IsChecked = state.ShowTrayIconEnabled;

            isApplyingState = false;
        }

        private void StartWithWindowsToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetStartWithWindowsEnabled(true);
            }
        }

        private void StartWithWindowsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetStartWithWindowsEnabled(false);
            }
        }

        private void RunAsAdministratorToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (isApplyingState)
            {
                return;
            }

            settingsService.SetRunAsAdministratorEnabled(true);

            if (!settingsService.IsCurrentProcessAdministrator())
            {
                MessageBox.Show(
                    "This setting is saved, but Windows elevation still requires a restart or a signed app manifest/scheduled task later.",
                    "Content Safety Guard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void RunAsAdministratorToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetRunAsAdministratorEnabled(false);
            }
        }

        private void RunMinimizedToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetRunMinimizedEnabled(true);
            }
        }

        private void RunMinimizedToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetRunMinimizedEnabled(false);
            }
        }

        private void ShowTrayIconToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetShowTrayIconEnabled(true);
            }
        }

        private void ShowTrayIconToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetShowTrayIconEnabled(false);
            }
        }
    }
}
