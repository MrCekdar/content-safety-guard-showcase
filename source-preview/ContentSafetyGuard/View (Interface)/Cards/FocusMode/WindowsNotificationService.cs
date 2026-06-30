using System;
using CommunityToolkit.WinUI.Notifications;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode
{
    public class WindowsNotificationService
    {
        public void ShowFocusReminder()
        {
            try
            {
                new ToastContentBuilder()
                    .AddText("Focus Mode")
                    .AddText("Take a short break.")
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddMinutes(5);
                    });
            }
            catch
            {
                // Windows notifications can be disabled by system settings or unavailable while debugging.
            }
        }
    }
}
