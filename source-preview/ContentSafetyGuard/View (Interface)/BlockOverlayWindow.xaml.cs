using ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.BlockingBehavior;
using System;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace ContentSafetyGuard
{
    public partial class BlockOverlayWindow : Window
    {
        private readonly BlockingBehaviorSettingsStateService settingsService;

        public BlockOverlayWindow()
            : this("Inhalt blockiert", "Dieser Bildschirm wurde von Content Safety Guard ausgeblendet.")
        {
        }

        public BlockOverlayWindow(string title, string reason)
        {
            InitializeComponent();

            settingsService = new BlockingBehaviorSettingsStateService();
            BlockTitleText.Text = title;
            BlockReasonText.Text = reason;

            if (settingsService.CurrentState.SoundAlertsEnabled)
            {
                SystemSounds.Exclamation.Play();
            }

            ApplyUnlockDelay();
        }

        private void Entsperren_Event(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyUnlockDelay()
        {
            int seconds = settingsService.CurrentState.UnlockDelaySeconds;

            if (seconds <= 0)
            {
                SchutzStop.Content = "ENTSPERREN";
                return;
            }

            SchutzStop.IsEnabled = false;
            SchutzStop.Content = $"ENTSPERREN IN {seconds} SEK.";

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += (sender, e) =>
            {
                seconds--;

                if (seconds <= 0)
                {
                    timer.Stop();
                    SchutzStop.IsEnabled = true;
                    SchutzStop.Content = "ENTSPERREN";
                    return;
                }

                SchutzStop.Content = $"ENTSPERREN IN {seconds} SEK.";
            };

            timer.Start();
        }
    }
}
