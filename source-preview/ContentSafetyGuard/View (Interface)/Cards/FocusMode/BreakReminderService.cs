using System;
using System.Windows.Threading;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode
{
    public class BreakReminderService
    {
        private readonly DispatcherTimer timer = new DispatcherTimer();

        public event EventHandler? ReminderDue;

        public BreakReminderService()
        {
            timer.Tick += Timer_Tick;
        }

        public void Start(int intervalMinutes)
        {
            timer.Stop();
            timer.Interval = TimeSpan.FromMinutes(intervalMinutes);
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            ReminderDue?.Invoke(this, EventArgs.Empty);
        }
    }
}
