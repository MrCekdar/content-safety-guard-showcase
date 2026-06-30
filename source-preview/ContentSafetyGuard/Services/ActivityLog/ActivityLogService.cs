using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ContentSafetyGuard.Services.ActivityLog
{
    public class ActivityLogService
    {
        private readonly string logFolderPath;
        private readonly string logFilePath;

        public ActivityLogService()
        {
            logFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ContentSafetyGuard");

            logFilePath = Path.Combine(logFolderPath, "activity_log.json");
        }

        public IReadOnlyList<ActivityLogEntry> LoadEntries()
        {
            try
            {
                if (!File.Exists(logFilePath))
                {
                    return Array.Empty<ActivityLogEntry>();
                }

                string json = File.ReadAllText(logFilePath);
                List<ActivityLogEntry>? entries = JsonSerializer.Deserialize<List<ActivityLogEntry>>(json);

                return entries ?? new List<ActivityLogEntry>();
            }
            catch
            {
                return Array.Empty<ActivityLogEntry>();
            }
        }

        public void AddEntry(ActivityLogEventType eventType, string reason = "")
        {
            List<ActivityLogEntry> entries = new List<ActivityLogEntry>(LoadEntries())
            {
                new ActivityLogEntry
                {
                    Timestamp = DateTimeOffset.Now,
                    EventType = eventType,
                    Reason = reason
                }
            };

            SaveEntries(entries);
        }

        private void SaveEntries(IReadOnlyList<ActivityLogEntry> entries)
        {
            try
            {
                Directory.CreateDirectory(logFolderPath);

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(entries, options);
                File.WriteAllText(logFilePath, json);
            }
            catch
            {
            }
        }
    }
}
