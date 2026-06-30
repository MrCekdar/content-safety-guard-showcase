using ContentSafetyGuard.AI;
using ContentSafetyGuard.Services;
using ContentSafetyGuard.Services.ActivityLog;
using ContentSafetyGuard.State;
using ContentSafetyGuard.View__Interface_;
using ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.Detection;
using ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Win32;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Text;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Collections.Specialized.BitVector32;

namespace ContentSafetyGuard
{

    public partial class MainWindow : Window
    {

        private ProtectionStateManager Protection;
        private BlockOverlayService Overlay;
        private ScanTimerService Timer;
        private NsfwDetector Detect;
        private ScreenCaptureService Capture;
        private DetectionSettingsStateService DetectionSettings;
        private SystemSettingsService SystemSettings;
        private SystemTrayService TrayIcon;
        private ActivityLogService ActivityLog;
        private float nsfwthreshold = 0.7f;
        private bool forceExit;

        public MainWindow()
        {
            InitializeComponent();

            // Klassen intialisieren
            Protection = new ProtectionStateManager();
            Overlay = new BlockOverlayService(Protection, this);
            Detect = new NsfwDetector(nsfwthreshold);
            Capture = new ScreenCaptureService(Protection);
            Timer = new ScanTimerService(Overlay, Detect, Capture, this);


            DetectionSettings = new DetectionSettingsStateService();
            ActivityLog = new ActivityLogService();
            SystemSettings = new SystemSettingsService();
            SystemSettings.StateChanged += SystemSettings_StateChanged;
            TrayIcon = new SystemTrayService(RestoreFromTray, ExitFromTray);
            TrayIcon.SetVisible(SystemSettings.CurrentState.ShowTrayIconEnabled || SystemSettings.CurrentState.RunMinimizedEnabled);

            DashboardDetectionSettingsCard.SetServices(DetectionSettings, Timer);
            KiView.SetServices(DetectionSettings, Timer);
            SettingView.SystemSettingsCard.SetService(SystemSettings);

            Loaded += MainWindow_Loaded;
        }

        public void SetProtectionState(bool state) // Zeigt an ob das Status Aktiv oder Inaktiv ist
        {
            Protection.setProtectionState(state);

            if (Protection.getProtectionState())
            {
                Status_Text.Text = "Status: Schutz ist aktiv";
            }
            else
            {
                Status_Text.Text = "Status: Schutz ist inaktiv";
            }
        }


        // Button um den Schutz zu proben
        private void Blockieren_testen(object sender, RoutedEventArgs e)
        {
            Overlay.ShowBlockOverlay();
        }

        public void ShowDashboardView()
        {
            DashboardView.Visibility = Visibility.Visible;
            InternetView.Visibility = Visibility.Collapsed;
            KiView.Visibility = Visibility.Collapsed;
            SettingView.Visibility = Visibility.Collapsed;
            InfoView.Visibility = Visibility.Collapsed;
        }

        public void ShowInternetView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            InternetView.Visibility = Visibility.Visible;
            KiView.Visibility = Visibility.Collapsed;
            SettingView.Visibility = Visibility.Collapsed;
            InfoView.Visibility = Visibility.Collapsed;
        }

        public void ShowKiView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            InternetView.Visibility = Visibility.Collapsed;
            KiView.Visibility = Visibility.Visible;
            SettingView.Visibility = Visibility.Collapsed;
            InfoView.Visibility = Visibility.Collapsed;
        }

        public void ShowSettingsView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            InternetView.Visibility = Visibility.Collapsed;
            KiView.Visibility = Visibility.Collapsed;
            SettingView.Visibility = Visibility.Visible;
            InfoView.Visibility = Visibility.Collapsed;
        }

        public void ShowInfoView()
        {
            DashboardView.Visibility = Visibility.Collapsed;
            InternetView.Visibility = Visibility.Collapsed;
            KiView.Visibility = Visibility.Collapsed;
            SettingView.Visibility = Visibility.Collapsed;
            InfoView.Visibility = Visibility.Visible;
        }

        //PlayButton Zustandwechsel 
        private bool protectionRunning = false;

        private LinearGradientBrush CreateProtectionCardBrush(bool isActive)
        {
            LinearGradientBrush brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };

            if (isActive)
            {
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(8, 49, 39), 0));
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(14, 39, 47), 0.55));
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(16, 31, 48), 1));
            }
            else
            {
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(16, 28, 46), 0));
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(16, 26, 42), 0.55));
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(19, 40, 58), 1));
            }

            return brush;
        }

        private void ProtectionCard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ProtectionCard.Clip = new RectangleGeometry(
                new Rect(0, 0, ProtectionCard.ActualWidth, ProtectionCard.ActualHeight),
                18,
                18);
        }

        private void ApplyProtectionVisualState(bool isActive)
        {
            if (isActive)
            {
                // Status punkt interpolieren lassen: Status_Text.Text = "ACTIVE";
                Status_Text.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                StatusFarbe.Background = new SolidColorBrush(Color.FromRgb(6, 61, 43));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));

                Storyboard pulse = (Storyboard)FindResource("PulseStatusDotStoryboard");
                pulse.Begin(this, true);

                // Card Effekt aktiv
                ProtectionCard.Background = CreateProtectionCardBrush(true);
                ProtectionCard.BorderBrush = new SolidColorBrush(Color.FromRgb(20, 81, 63));

                ProtectionGlow.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                ProtectionGlow.Opacity = 0.18;

                ProtectionGlowSmall.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                ProtectionGlowSmall.Opacity = 0.10;

                // Shield aktiv
                ShieldIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ShieldCheckOutline;
                ShieldIcon.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                ShieldBox.Background = new SolidColorBrush(Color.FromRgb(6, 61, 43));
                ShieldBox.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));

                // Texte aktiv
                ProtectionTitleText.Text = "Protection Active";
                ProtectionTitleText.Foreground = new SolidColorBrush(Color.FromRgb(167, 243, 208));

                ProtectionDescriptionText.Text = "Scanning screen content locally using the ONNX AI model.";
                ProtectionStatusSmallText.Text = "↯ Protection is running";
                ProtectionStatusSmallText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));

                // Button aktiv
                ProtectionToggleText.Text = "Stop";
                ProtectionToggleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Stop;
                ProtectionToggleButton.Background = new SolidColorBrush(Color.FromRgb(230, 35, 35));

                // Status oben rechts
                Status_Text.Text = "ACTIVE";
                Status_Text.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                StatusFarbe.Background = new SolidColorBrush(Color.FromRgb(6, 61, 43));
            }
            else
            {
                // Status Punkt kleiner machen
             
                    Status_Text.Text = "INACTIVE";
                    Status_Text.Foreground = (Brush)FindResource("TextSecondary");
                    StatusFarbe.Background = new SolidColorBrush(Color.FromRgb(28, 39, 58));
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(139, 149, 165));

                    Storyboard pulse = (Storyboard)FindResource("PulseStatusDotStoryboard");
                    pulse.Stop(this);

                    StatusDotScale.ScaleX = 1;
                    StatusDotScale.ScaleY = 1;
                    StatusDot.Opacity = 1;

                // Card Effekt inaktiv
                ProtectionCard.Background = CreateProtectionCardBrush(false);
                ProtectionCard.BorderBrush = new SolidColorBrush(Color.FromRgb(36, 75, 122));

                ProtectionGlow.Fill = new SolidColorBrush(Color.FromRgb(48, 120, 230));
                ProtectionGlow.Opacity = 0.13;

                ProtectionGlowSmall.Fill = new SolidColorBrush(Color.FromRgb(48, 120, 230));
                ProtectionGlowSmall.Opacity = 0.07;

                // Shield inaktiv
                ShieldIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ShieldOffOutline;
                ShieldIcon.Foreground = new SolidColorBrush(Color.FromRgb(74, 118, 173));
                ShieldBox.Background = new SolidColorBrush(Color.FromRgb(18, 40, 74));
                ShieldBox.BorderBrush = new SolidColorBrush(Color.FromRgb(36, 75, 122));

                // Texte inaktiv
                ProtectionTitleText.Text = "Protection Inactive";
                ProtectionTitleText.Foreground = (Brush)FindResource("TextPrimary");

                ProtectionDescriptionText.Text = "Click Start Protection to begin monitoring your screen.";
                ProtectionStatusSmallText.Text = "Ready to start";
                ProtectionStatusSmallText.Foreground = (Brush)FindResource("TextSecondary");

                // Button inaktiv
                ProtectionToggleText.Text = "Start Protection";
                ProtectionToggleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                ProtectionToggleButton.Background = (Brush)FindResource("AccentBlue");

                // Status oben rechts
                Status_Text.Text = "● INACTIVE";
                Status_Text.Foreground = (Brush)FindResource("TextSecondary");
                StatusFarbe.Background = new SolidColorBrush(Color.FromRgb(28, 39, 58));
            }
        }

        private void ProtectionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            protectionRunning = !protectionRunning;

            SetProtectionState(protectionRunning);

            if (protectionRunning)
            {
                Timer.Start();
                ActivityLog.AddEntry(ActivityLogEventType.ProtectionStarted, "User started protection");
            }
            else
            {
                Timer.Stop();
                ActivityLog.AddEntry(ActivityLogEventType.ProtectionStopped, "User stopped protection");
            }

            ApplyProtectionVisualState(protectionRunning);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleWindowState();
                return;
            }

            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InternetView.ApplyWebsiteBlockingRules();

            bool shouldStartMinimized = Environment.GetCommandLineArgs()
                .Any(argument => argument.Equals("--minimized", StringComparison.OrdinalIgnoreCase));

            if (shouldStartMinimized && SystemSettings.CurrentState.RunMinimizedEnabled)
            {
                Hide();
            }
        }

        private void SystemSettings_StateChanged(object? sender, EventArgs e)
        {
            TrayIcon.SetVisible(SystemSettings.CurrentState.ShowTrayIconEnabled || SystemSettings.CurrentState.RunMinimizedEnabled);
        }

        private void RestoreFromTray()
        {
            Dispatcher.Invoke(() =>
            {
                Show();

                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();
            });
        }

        private void ExitFromTray()
        {
            Dispatcher.Invoke(() =>
            {
                forceExit = true;
                Close();
            });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            MaximizeIcon.Kind = WindowState == WindowState.Maximized
                ? MaterialDesignThemes.Wpf.PackIconKind.WindowRestore
                : MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceExit && SystemSettings.CurrentState.RunMinimizedEnabled)
            {
                e.Cancel = true;
                Hide();
                TrayIcon.SetVisible(true);
                return;
            }

            try
            {
                InternetView.ClearWebsiteBlockingRules();

                ProgramBlockService blockService = new ProgramBlockService();
                blockService.ClearBlockedPrograms();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Temporary program blocks could not be cleared: {ex.Message}");
                MessageBox.Show(
                    "Temporary program blocks could not be cleared automatically. Please reset AppLocker manually if programs remain blocked.",
                    "Content Safety Guard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            TrayIcon.Dispose();

            base.OnClosing(e);
        }
    }
}
