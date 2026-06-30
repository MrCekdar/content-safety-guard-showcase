using ContentSafetyGuard.Services;
using System.Windows;
using System.Windows.Controls;
using static ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.Detection.DetectionSettingsState;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.Detection
{
    /// <summary>
    /// Interaktionslogik für DetectionSettingsCard.xaml 
    /// </summary>
    public partial class DetectionSettingsCard : UserControl
    {

        private DetectionSettingsStateService? settingsService;
        private bool isApplyingState;
        private ScanTimerService? Timer;


        // Verwaltung an übergabe mehrere Cards
        internal void SetServices(
            DetectionSettingsStateService detectionSettingsService,
            ScanTimerService scanTimerService)
        {
            if (settingsService != null)
            {
                settingsService.StateChanged -= DetectionSettingsStateService_StateChanged;
            }

            settingsService = detectionSettingsService;
            Timer = scanTimerService;

            settingsService.StateChanged += DetectionSettingsStateService_StateChanged;

            ApplyState(settingsService.CurrentState);
        }

        public DetectionSettingsCard()
        {
            InitializeComponent();
        }

        private void DetectionSettingsCard_Loaded(object sender, RoutedEventArgs e)
        {
            if (settingsService == null)
            {
                return;
            }

            ApplyState(settingsService.CurrentState);
        }

        private void DetectionSettingsCard_Unloaded(object sender, RoutedEventArgs e)
        {
            if (settingsService == null)
            {
                return;
            }

            settingsService.StateChanged -= DetectionSettingsStateService_StateChanged;
        }

        private void DetectionSettingsStateService_StateChanged(object? sender, EventArgs e)
        {
            if (settingsService == null)
            {
                return;
            }

            ApplyState(settingsService.CurrentState);
        }

        private void ApplyStateToTimer(DetectionSettingsState state)
        {
            Timer?.SetAiDetectionEnabled(state.AiDetectionEnabled);
            Timer?.ActivateGrid(state.GridScanningEnabled);
            Timer?.SetNsfwThreshold(state.NsfwThreshold);
            Timer?.SetScanFrequency(state.ScanFrequency);
        }

        private void AIScanningToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (isApplyingState || settingsService == null)
            {
                return;
            }

            settingsService.SetAiDetectionEnabled(true);
        }

        private void AIScanningToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isApplyingState || settingsService == null)
            {
                return;
            }

            settingsService.SetAiDetectionEnabled(false);
        }

        private void GridScanningToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (isApplyingState || settingsService == null)
            {
                return;
            }

            settingsService.SetGridScanningEnabled(true);
        }

        private void GridScanningToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isApplyingState || settingsService == null)
            {
                return;
            }

            settingsService.SetGridScanningEnabled(false);
        }

        private System.Windows.Media.Color InterpolateColor(System.Windows.Media.Color start,
                                                     System.Windows.Media.Color end, double t)
        {
            byte r = (byte)(start.R + (end.R - start.R) * t);
            byte g = (byte)(start.G + (end.G - start.G) * t);
            byte b = (byte)(start.B + (end.B - start.B) * t);

            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
      

            int sliderValue = (int)Math.Round(e.NewValue);
            Restrictionnumber.Text = sliderValue.ToString() + "%";

            double t = sliderValue / Slider.Maximum;

            var grau = System.Windows.Media.Color.FromRgb(127, 127, 127); //grau
            var gelb = System.Windows.Media.Color.FromRgb(255, 222, 33); // Gelb
            var rot = System.Windows.Media.Color.FromRgb(255, 35, 35); // rot

            System.Windows.Media.Color currentColor;

            if (t <= 0.25)
            {
                Prozentbeschreibung.Text = "Permissive";
            }
            else if (t > 0.25 && t <= 0.5)
            {
                Prozentbeschreibung.Text = "Moderate";
            }
            else if (t > 0.5 && t <= 0.75)
            {
                Prozentbeschreibung.Text = "Strict";
            }
            else if (t >= 0.75)
            {
                Prozentbeschreibung.Text = "Maximum";
            }


            if (t <= 0.5)
            {
                double localt = t / 0.5;
                currentColor = InterpolateColor(grau, gelb, localt);
            }
            else if (t >= 0.5)
            {
                double localt = (t - 0.5) / 0.5;
                currentColor = InterpolateColor(gelb, rot, localt);
            }

            var Brush = new System.Windows.Media.SolidColorBrush(currentColor);

            Warning.Foreground = Brush;
            Prozentbeschreibung.Foreground = Brush;
            Prozentkasten.BorderBrush = Brush;

            if (!isApplyingState && settingsService != null)
            {
                settingsService.SetNsfwSensitivityPercent(sliderValue);
            }

        }

        private void ApplyState(DetectionSettingsState state)
        {
            isApplyingState = true;

            AIScanningToggle.IsChecked = state.AiDetectionEnabled;
            GridScanningToggle.IsChecked = state.GridScanningEnabled;
            Slider.Value = state.NsfwSensitivityPercent;
            ScanFrequencyComboBox.SelectedIndex = state.ScanFrequency switch
            {
                ScanFrequencyMode.High => 0,
                ScanFrequencyMode.Normal => 1,
                ScanFrequencyMode.Low => 2,
                _ => 0
            };

            Balanced.IsChecked = state.DetectionMode == DetectionMode.Balanced;
            Strict.IsChecked = state.DetectionMode == DetectionMode.Strict;
            Maximum.IsChecked = state.DetectionMode == DetectionMode.Maximum;

            isApplyingState = false;

            ApplyStateToTimer(state);
        }

        private void Balanced_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState && settingsService != null)
            {
                settingsService.SetDetectionMode(DetectionMode.Balanced);
            }
        }

        private void Strict_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState && settingsService != null)
            {
                settingsService.SetDetectionMode(DetectionMode.Strict);
            }
        }

        private void Maximum_Checked(object sender, RoutedEventArgs e)
        {
            if (!isApplyingState && settingsService != null)
            {
                settingsService.SetDetectionMode(DetectionMode.Maximum);
            }
        }

        private void ScanFrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isApplyingState || settingsService == null)
            {
                return;
            }

            ScanFrequencyMode mode = ScanFrequencyComboBox.SelectedIndex switch
            {
                0 => ScanFrequencyMode.High,
                1 => ScanFrequencyMode.Normal,
                2 => ScanFrequencyMode.Low,
                _ => ScanFrequencyMode.High
            };

            settingsService.SetScanFrequency(mode);
        }

    }
}
