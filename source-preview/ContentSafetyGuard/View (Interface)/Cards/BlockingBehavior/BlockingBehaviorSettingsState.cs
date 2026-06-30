using System;
using System.Collections.Generic;
using System.Text;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.BlockingBehavior
{
    public class BlockingBehaviorSettingsState
    {
        public int UnlockDelaySeconds { get; set; } = 15;
        public bool CloseProgramWhenDetected { get; set; } = false;
        public bool SoundAlertsEnabled { get; set; } = false;
    }
}
