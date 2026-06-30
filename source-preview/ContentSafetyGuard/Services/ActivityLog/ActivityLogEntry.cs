using System;

namespace ContentSafetyGuard.Services.ActivityLog
{
    public class ActivityLogEntry
    {
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
        public ActivityLogEventType EventType { get; set; }
        public string Reason { get; set; } = "";
    }
}
