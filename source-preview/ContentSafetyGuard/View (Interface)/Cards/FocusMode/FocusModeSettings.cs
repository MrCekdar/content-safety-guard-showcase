namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode
{
    public class FocusModeSettings
    {
        public bool IsFocusModeEnabled { get; set; }
        public int SessionDurationMinutes { get; set; } = 5;
        public bool BreakRemindersEnabled { get; set; } = true;
        public int BreakReminderIntervalMinutes { get; set; } = 5;
    }
}
