using ContentSafetyGuard.Services;
using ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.Detection;
using System.Windows.Controls;

namespace ContentSafetyGuard.View__Interface_.Views
{
    public partial class KIView : UserControl
    {
        public KIView()
        {
            InitializeComponent();
        }

        internal void SetServices(
           DetectionSettingsStateService detectionSettingsService,
           ScanTimerService scanTimerService)
        {
            KiDetectionSettingsCard.SetServices(
                detectionSettingsService,
                scanTimerService);
        }

    }
}
