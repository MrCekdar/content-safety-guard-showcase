using ContentSafetyGuard.AI;
using ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.Detection;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Threading;

namespace ContentSafetyGuard.Services
{
    internal class ScanTimerService
    {
        private readonly DispatcherTimer protectionTimer;
        private readonly BlockOverlayService overlay;
        private readonly ScreenCaptureService capture;
        private readonly NsfwDetector nsfwDetector;

        private bool gridScanningEnabled;
        private bool aiDetectionEnabled = true;

        public ScanTimerService(
            BlockOverlayService overlayService,
            NsfwDetector nsfwDetect,
            ScreenCaptureService captureService,
            MainWindow window)
        {
            overlay = overlayService;
            nsfwDetector = nsfwDetect;
            capture = captureService;

            protectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };

            protectionTimer.Tick += ProtectionTimer_Tick;
        }

        public void Start()
        {
            protectionTimer.Start();
        }

        public void Stop()
        {
            protectionTimer.Stop();
        }

        public void SetAiDetectionEnabled(bool enabled)
        {
            aiDetectionEnabled = enabled;
        }

        public void ActivateGrid(bool active)
        {
            gridScanningEnabled = active;
        }

        public void SetNsfwThreshold(float threshold)
        {
            nsfwDetector.SetNsfwThreshold(threshold);
        }

        public void SetScanFrequency(ScanFrequencyMode scanFrequency)
        {
            protectionTimer.Interval = scanFrequency switch
            {
                ScanFrequencyMode.High => TimeSpan.FromSeconds(0.5),
                ScanFrequencyMode.Normal => TimeSpan.FromSeconds(1),
                ScanFrequencyMode.Low => TimeSpan.FromSeconds(2),
                _ => TimeSpan.FromSeconds(0.5)
            };
        }

        private void ProtectionTimer_Tick(object? sender, EventArgs e)
        {
            if (!aiDetectionEnabled)
            {
                Debug.WriteLine("AI detection is disabled");
                return;
            }

            using Bitmap? frame = capture.CaptureCurrentScreenFrame();

            bool shouldBlock = gridScanningEnabled
                ? nsfwDetector.DetectExplicitContentFromFrameviaGrid(frame)
                : nsfwDetector.DetectSingleFrame(frame);

            if (shouldBlock)
            {
                overlay.ShowBlockOverlay();
            }
        }
    }
}
