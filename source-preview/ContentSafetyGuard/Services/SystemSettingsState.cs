namespace ContentSafetyGuard.Services
{
    public class SystemSettingsState
    {
        public bool StartWithWindowsEnabled { get; set; } = false;
        public bool RunAsAdministratorEnabled { get; set; } = false;
        public bool RunMinimizedEnabled { get; set; } = false;
        public bool ShowTrayIconEnabled { get; set; } = true;
    }
}
