using ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.BlockingBehavior;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.BlockingBehavior
{
    /// <summary>
    /// Interaktionslogik für BlockingBehaviorCard.xaml
    /// </summary>
    public partial class BlockingBehaviorCard : UserControl
    {

        private readonly BlockingBehaviorSettingsStateService settingsService;
        private bool isApplyingState;

        public BlockingBehaviorCard()
        {

            settingsService = new BlockingBehaviorSettingsStateService();

            InitializeComponent();

            Loaded += BlockingBehaviorCard_Loaded;
        }

        private void BlockingBehaviorCard_Loaded(object sender, RoutedEventArgs e)
        {
            isApplyingState = true;
            ApplyStateToUI(settingsService.CurrentState);
            isApplyingState = false;
        }

        private void UnlockDelayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isApplyingState)
            {
                return;
            }

            if (UnlockDelayComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                return;
            }

            if (selectedItem.Tag is not string tag)
            {
                return;
            }

            if (!int.TryParse(tag, out int seconds))
            {
                return;
            }

            settingsService.SetUnlockDelaySeconds(seconds);
        }

        private void ApplyStateToUI(BlockingBehaviorSettingsState state)
        {
            CloseProgramToggle.IsChecked = state.CloseProgramWhenDetected;
            SoundAlertsToggle.IsChecked = state.SoundAlertsEnabled;

            foreach (ComboBoxItem item in UnlockDelayComboBox.Items)
            {
                if (item.Tag is string tag &&
                   int.TryParse(tag, out int seconds) &&
                   seconds == state.UnlockDelaySeconds)
                {
                    UnlockDelayComboBox.SelectedItem = item;
                    return;
                }
            }

            UnlockDelayComboBox.SelectedIndex = 0;
        }

        private void CloseProgramToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetCloseProgramWhenDetected(true);
            }
        }

        private void CloseProgramToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetCloseProgramWhenDetected(false);
            }
        }

        private void SoundAlertsToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetSoundAlertsEnabled(true);
            }
        }

        private void SoundAlertsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState)
            {
                settingsService.SetSoundAlertsEnabled(false);
            }
        }

    }
}
