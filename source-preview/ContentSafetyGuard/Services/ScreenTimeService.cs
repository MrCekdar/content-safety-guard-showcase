using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;

namespace ContentSafetyGuard.Services
{
    internal class ScreenTimeService : INotifyPropertyChanged
    {
        private const double WeeklyChartHeight = 126;
        private const double MinimumAppListReferenceMinutes = 10;
        private const double MinimumCategoryReferenceMinutes = 10;
        private const double MaximumTrackedGapMinutes = 1;
        private const double AppBarMaximumWidth = 340;
        private const double CategoryBarMaximumWidth = 280;

        private static readonly Dictionary<string, string> CategoryColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Social Media"] = "#0A84FF",
            ["Browsers"] = "#30A8FF",
            ["Streaming"] = "#BF5AF2",
            ["Gaming"] = "#FF9F0A",
            ["Productivity"] = "#18D3D8",
            ["Communication"] = "#64D2FF",
            ["Education"] = "#30D158",
            ["Other"] = "#8B95A5"
        };

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private readonly DispatcherTimer timer;
        private readonly Dictionary<string, ScreenTimeUsageItem> usageByKey = new Dictionary<string, ScreenTimeUsageItem>(StringComparer.OrdinalIgnoreCase);
        private readonly string storageFolderPath;
        private readonly string storageFilePath;
        private readonly string ownProcessName;
        private ScreenTimeStorageState storageState = new ScreenTimeStorageState();
        private DateTime lastTick = DateTime.Now;
        private DateTime lastSaveTime = DateTime.MinValue;
        private string currentDateKey = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private double totalMinutesToday;
        private double weeklyChartReferenceMinutes = 30;
        private string lastBlockKey = "";
        private DateTime lastBlockTime = DateTime.MinValue;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<string>? BlockingRequested;

        public ObservableCollection<ScreenTimeUsageItem> TopUsageItems { get; } = new ObservableCollection<ScreenTimeUsageItem>();
        public ObservableCollection<ScreenTimeCategoryUsageItem> CategoryUsageItems { get; } = new ObservableCollection<ScreenTimeCategoryUsageItem>();
        public ObservableCollection<WeeklyUsageItem> WeeklyUsageItems { get; } = new ObservableCollection<WeeklyUsageItem>();
        public ObservableCollection<ScreenTimeLimitItem> AppLimits { get; } = new ObservableCollection<ScreenTimeLimitItem>();
        public ObservableCollection<DowntimeScheduleItem> DowntimeSchedules { get; } = new ObservableCollection<DowntimeScheduleItem>();
        public ObservableCollection<AlwaysAllowedAppItem> AlwaysAllowedApps { get; } = new ObservableCollection<AlwaysAllowedAppItem>();

        public ScreenTimeService()
        {
            using Process currentProcess = Process.GetCurrentProcess();
            ownProcessName = currentProcess.ProcessName;

            storageFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ContentSafetyGuard");
            storageFilePath = Path.Combine(storageFolderPath, "screen_time_usage.json");

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            timer.Tick += Timer_Tick;

            SeedWeeklyItems();
            LoadStoredUsage();
            SeedControls();
        }

        public string TotalScreenTimeToday => ScreenTimeCategoryUsageItem.FormatMinutes(totalMinutesToday);
        public string WeeklyChartScaleDescription => $"Auto-Skala bis {FormatScaleLabel(weeklyChartReferenceMinutes)}";
        public string WeeklyChartTopLabel => FormatScaleLabel(weeklyChartReferenceMinutes);
        public string WeeklyChartMidLabel => FormatScaleLabel(weeklyChartReferenceMinutes / 2);

        public void Start()
        {
            EnsureCurrentDay();
            lastTick = DateTime.Now;
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
            SaveUsage();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            EnsureCurrentDay();

            DateTime now = DateTime.Now;
            double elapsedMinutes = Math.Max(0, (now - lastTick).TotalMinutes);
            lastTick = now;

            ForegroundUsage? currentUsage = GetForegroundUsage();

            if (currentUsage == null || elapsedMinutes <= 0)
            {
                return;
            }

            elapsedMinutes = Math.Min(elapsedMinutes, MaximumTrackedGapMinutes);

            if (!usageByKey.TryGetValue(currentUsage.Key, out ScreenTimeUsageItem? item))
            {
                item = new ScreenTimeUsageItem
                {
                    ProcessName = currentUsage.ProcessName,
                    Title = currentUsage.Title,
                    CategoryName = currentUsage.CategoryName,
                    Color = GetCategoryColor(currentUsage.CategoryName)
                };

                usageByKey[currentUsage.Key] = item;
            }

            item.TotalMinutes += elapsedMinutes;
            totalMinutesToday += elapsedMinutes;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalScreenTimeToday)));

            RefreshTopItems();
            RefreshCategoryItems();
            RefreshWeeklyItems();
            CheckLimits(currentUsage);

            if (DateTime.Now - lastSaveTime > TimeSpan.FromSeconds(15))
            {
                SaveUsage();
            }
        }

        private void RefreshTopItems()
        {
            List<ScreenTimeUsageItem> topItems = usageByKey.Values
                .OrderByDescending(item => item.TotalMinutes)
                .Take(20)
                .ToList();

            double maxItemMinutes = topItems.Count == 0
                ? 0
                : topItems.Max(item => item.TotalMinutes);
            double referenceMinutes = Math.Max(MinimumAppListReferenceMinutes, maxItemMinutes);

            foreach (ScreenTimeUsageItem item in topItems)
            {
                item.BarWidth = item.TotalMinutes <= 0
                    ? 0
                    : Math.Min(AppBarMaximumWidth, Math.Max(22, item.TotalMinutes / referenceMinutes * AppBarMaximumWidth));
                item.Color = GetCategoryColor(item.CategoryName);
            }

            TopUsageItems.Clear();

            foreach (ScreenTimeUsageItem item in topItems)
            {
                TopUsageItems.Add(item);
            }
        }

        private void RefreshCategoryItems()
        {
            List<ScreenTimeCategoryUsageItem> categoryItems = usageByKey.Values
                .GroupBy(item => item.CategoryName)
                .Select(group => new ScreenTimeCategoryUsageItem
                {
                    CategoryName = group.Key,
                    TotalMinutes = group.Sum(item => item.TotalMinutes),
                    Color = GetCategoryColor(group.Key)
                })
                .OrderByDescending(item => item.TotalMinutes)
                .Take(4)
                .ToList();

            double maxCategoryMinutes = categoryItems.Count == 0
                ? 0
                : categoryItems.Max(item => item.TotalMinutes);
            double referenceMinutes = Math.Max(MinimumCategoryReferenceMinutes, maxCategoryMinutes);

            foreach (ScreenTimeCategoryUsageItem item in categoryItems)
            {
                item.BarWidth = Math.Min(CategoryBarMaximumWidth, Math.Max(18, item.TotalMinutes / referenceMinutes * CategoryBarMaximumWidth));
            }

            CategoryUsageItems.Clear();

            foreach (ScreenTimeCategoryUsageItem item in categoryItems)
            {
                CategoryUsageItems.Add(item);
            }
        }

        private void RefreshWeeklyItems()
        {
            DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            List<Dictionary<string, double>> weeklyCategoryTotals = new List<Dictionary<string, double>>();
            double maxDayMinutes = 0;

            for (int index = 0; index < WeeklyUsageItems.Count; index++)
            {
                DateTime date = startOfWeek.AddDays(index);
                Dictionary<string, double> categoryTotals = GetCategoryTotalsForDate(date);
                weeklyCategoryTotals.Add(categoryTotals);
                maxDayMinutes = Math.Max(maxDayMinutes, categoryTotals.Values.Sum());
            }

            double referenceMinutes = GetWeeklyChartReferenceMinutes(maxDayMinutes);

            if (Math.Abs(referenceMinutes - weeklyChartReferenceMinutes) > 0.1)
            {
                weeklyChartReferenceMinutes = referenceMinutes;
                NotifyWeeklyChartScaleChanged();
            }

            for (int index = 0; index < WeeklyUsageItems.Count; index++)
            {
                WeeklyUsageItem item = WeeklyUsageItems[index];
                Dictionary<string, double> categoryTotals = weeklyCategoryTotals[index];

                item.TotalMinutes = categoryTotals.Values.Sum();
                item.BarHeight = item.TotalMinutes <= 0
                    ? 8
                    : Math.Min(WeeklyChartHeight, Math.Max(4, item.TotalMinutes / referenceMinutes * WeeklyChartHeight));

                RefreshWeeklySegments(item, categoryTotals, referenceMinutes);
            }
        }

        private void EnsureCurrentDay()
        {
            string todayKey = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (todayKey.Equals(currentDateKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SaveUsage();

            currentDateKey = todayKey;
            usageByKey.Clear();
            totalMinutesToday = 0;

            LoadCurrentDayFromStorage();
            RefreshTopItems();
            RefreshCategoryItems();
            RefreshWeeklyItemsFromStorage();
            RefreshWeeklyItems();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalScreenTimeToday)));
        }

        private void LoadStoredUsage()
        {
            try
            {
                if (File.Exists(storageFilePath))
                {
                    string json = File.ReadAllText(storageFilePath);
                    storageState = JsonSerializer.Deserialize<ScreenTimeStorageState>(json) ?? new ScreenTimeStorageState();
                }
            }
            catch
            {
                storageState = new ScreenTimeStorageState();
            }

            PruneOldUsage();
            LoadCurrentDayFromStorage();
            RefreshTopItems();
            RefreshCategoryItems();
            RefreshWeeklyItemsFromStorage();
            RefreshWeeklyItems();
        }

        private void LoadCurrentDayFromStorage()
        {
            usageByKey.Clear();

            ScreenTimeStorageDay? today = storageState.Days
                .FirstOrDefault(day => day.Date.Equals(currentDateKey, StringComparison.OrdinalIgnoreCase));

            if (today == null)
            {
                totalMinutesToday = 0;
                return;
            }

            foreach (ScreenTimeStorageApp app in today.Apps)
            {
                if (string.IsNullOrWhiteSpace(app.Key) ||
                    string.IsNullOrWhiteSpace(app.Title) ||
                    IsSelfProcess(app.ProcessName) ||
                    app.TotalMinutes <= 0)
                {
                    continue;
                }

                usageByKey[app.Key] = new ScreenTimeUsageItem
                {
                    ProcessName = app.ProcessName,
                    Title = app.Title,
                    CategoryName = string.IsNullOrWhiteSpace(app.CategoryName) ? "Other" : app.CategoryName,
                    Color = GetCategoryColor(app.CategoryName),
                    TotalMinutes = app.TotalMinutes
                };
            }

            totalMinutesToday = usageByKey.Values.Sum(item => item.TotalMinutes);
        }

        private void SaveUsage()
        {
            try
            {
                PruneOldUsage();

                ScreenTimeStorageDay? today = storageState.Days
                    .FirstOrDefault(day => day.Date.Equals(currentDateKey, StringComparison.OrdinalIgnoreCase));

                if (today == null)
                {
                    today = new ScreenTimeStorageDay { Date = currentDateKey };
                    storageState.Days.Add(today);
                }

                today.Apps = usageByKey
                    .OrderByDescending(pair => pair.Value.TotalMinutes)
                    .Select(pair => new ScreenTimeStorageApp
                    {
                        Key = pair.Key,
                        ProcessName = pair.Value.ProcessName,
                        Title = pair.Value.Title,
                        CategoryName = pair.Value.CategoryName,
                        TotalMinutes = pair.Value.TotalMinutes
                    })
                    .ToList();

                Directory.CreateDirectory(storageFolderPath);

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(storageState, options);
                File.WriteAllText(storageFilePath, json);
                lastSaveTime = DateTime.Now;
            }
            catch
            {
            }
        }

        private void RefreshWeeklyItemsFromStorage()
        {
            foreach (WeeklyUsageItem item in WeeklyUsageItems)
            {
                item.TotalMinutes = 0;
            }

            DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(6);

            foreach (ScreenTimeStorageDay day in storageState.Days)
            {
                if (!DateTime.TryParseExact(day.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    continue;
                }

                if (date < startOfWeek || date > endOfWeek)
                {
                    continue;
                }

                WeeklyUsageItems[(int)date.DayOfWeek].TotalMinutes = day.Apps.Sum(app => Math.Max(0, app.TotalMinutes));
            }
        }

        private Dictionary<string, double> GetCategoryTotalsForDate(DateTime date)
        {
            if (date.Date == DateTime.Today)
            {
                return usageByKey.Values
                    .Where(item => item.TotalMinutes > 0)
                    .GroupBy(item => item.CategoryName)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.TotalMinutes), StringComparer.OrdinalIgnoreCase);
            }

            string dateKey = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ScreenTimeStorageDay? day = storageState.Days
                .FirstOrDefault(entry => entry.Date.Equals(dateKey, StringComparison.OrdinalIgnoreCase));

            if (day == null)
            {
                return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            }

            return day.Apps
                .Where(app => !IsSelfProcess(app.ProcessName) && app.TotalMinutes > 0)
                .GroupBy(app => string.IsNullOrWhiteSpace(app.CategoryName) ? "Other" : app.CategoryName)
                .ToDictionary(group => group.Key, group => group.Sum(app => app.TotalMinutes), StringComparer.OrdinalIgnoreCase);
        }

        private void RefreshWeeklySegments(WeeklyUsageItem item, Dictionary<string, double> categoryTotals, double referenceMinutes)
        {
            item.Segments.Clear();

            double usedHeight = 0;

            foreach (KeyValuePair<string, double> category in categoryTotals
                         .Where(pair => pair.Value > 0)
                         .OrderByDescending(pair => pair.Value))
            {
                double segmentHeight = category.Value / referenceMinutes * WeeklyChartHeight;

                if (segmentHeight < 2 && usedHeight > 0)
                {
                    continue;
                }

                segmentHeight = Math.Max(3, segmentHeight);
                segmentHeight = Math.Min(segmentHeight, WeeklyChartHeight - usedHeight);

                if (segmentHeight <= 0)
                {
                    break;
                }

                item.Segments.Add(new WeeklyUsageSegmentItem
                {
                    CategoryName = category.Key,
                    SegmentHeight = segmentHeight,
                    Color = GetCategoryColor(category.Key)
                });

                usedHeight += segmentHeight;
            }
        }

        private double GetWeeklyChartReferenceMinutes(double maxDayMinutes)
        {
            if (maxDayMinutes <= 10)
            {
                return 10;
            }

            if (maxDayMinutes <= 30)
            {
                return 30;
            }

            if (maxDayMinutes <= 60)
            {
                return 60;
            }

            if (maxDayMinutes <= 120)
            {
                return 120;
            }

            if (maxDayMinutes <= 240)
            {
                return 240;
            }

            if (maxDayMinutes <= 480)
            {
                return 480;
            }

            return Math.Ceiling(maxDayMinutes / 60) * 60;
        }

        private string FormatScaleLabel(double minutes)
        {
            return ScreenTimeCategoryUsageItem.FormatMinutes(minutes);
        }

        private void NotifyWeeklyChartScaleChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WeeklyChartScaleDescription)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WeeklyChartTopLabel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WeeklyChartMidLabel)));
        }

        private void PruneOldUsage()
        {
            DateTime oldestDate = DateTime.Today.AddDays(-30);

            storageState.Days = storageState.Days
                .Where(day =>
                    DateTime.TryParseExact(day.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) &&
                    date >= oldestDate)
                .ToList();
        }

        private void CheckLimits(ForegroundUsage currentUsage)
        {
            if (IsAlwaysAllowed(currentUsage.ProcessName))
            {
                return;
            }

            string? reason = GetDowntimeReason();

            if (reason == null)
            {
                reason = GetLimitReason(currentUsage);
            }

            if (reason == null)
            {
                return;
            }

            string blockKey = $"{currentUsage.ProcessName}|{reason}";

            if (blockKey == lastBlockKey && DateTime.Now - lastBlockTime < TimeSpan.FromMinutes(1))
            {
                return;
            }

            lastBlockKey = blockKey;
            lastBlockTime = DateTime.Now;
            BlockingRequested?.Invoke(this, reason);
        }

        private string? GetLimitReason(ForegroundUsage currentUsage)
        {
            foreach (ScreenTimeLimitItem limit in AppLimits.Where(limit => limit.IsEnabled && limit.LimitMinutes > 0))
            {
                bool categoryMatch = limit.CategoryName.Equals(currentUsage.CategoryName, StringComparison.OrdinalIgnoreCase);
                bool appMatch = currentUsage.ProcessName.Contains(limit.TargetName, StringComparison.OrdinalIgnoreCase);

                if (!categoryMatch && !appMatch)
                {
                    continue;
                }

                double usedMinutes = usageByKey.Values
                    .Where(item => item.CategoryName.Equals(limit.CategoryName, StringComparison.OrdinalIgnoreCase) ||
                                   item.ProcessName.Contains(limit.TargetName, StringComparison.OrdinalIgnoreCase))
                    .Sum(item => item.TotalMinutes);

                if (usedMinutes >= limit.LimitMinutes)
                {
                    return $"App limit reached for {limit.TargetName}.";
                }
            }

            return null;
        }

        private string? GetDowntimeReason()
        {
            DateTime now = DateTime.Now;

            foreach (DowntimeScheduleItem schedule in DowntimeSchedules.Where(schedule => schedule.IsEnabled))
            {
                if (!TryParseTime(schedule.StartTime, out TimeSpan start) ||
                    !TryParseTime(schedule.EndTime, out TimeSpan end))
                {
                    continue;
                }

                TimeSpan currentTime = now.TimeOfDay;
                bool crossesMidnight = end <= start;
                bool active = crossesMidnight
                    ? currentTime >= start || currentTime <= end
                    : currentTime >= start && currentTime <= end;

                if (active && IsTodayIncluded(schedule.Days, now.DayOfWeek))
                {
                    return $"Downtime active: {schedule.Name}.";
                }
            }

            return null;
        }

        private bool IsAlwaysAllowed(string processName)
        {
            return AlwaysAllowedApps.Any(app =>
                processName.Contains(app.ProcessName, StringComparison.OrdinalIgnoreCase));
        }

        private bool TryParseTime(string value, out TimeSpan time)
        {
            return TimeSpan.TryParse(value, out time);
        }

        private bool IsTodayIncluded(string days, DayOfWeek dayOfWeek)
        {
            if (days.Equals("Every day", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (days.Equals("Mon-Fri", StringComparison.OrdinalIgnoreCase))
            {
                return dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
            }

            return days.Contains(dayOfWeek.ToString()[..3], StringComparison.OrdinalIgnoreCase);
        }

        private ForegroundUsage? GetForegroundUsage()
        {
            IntPtr foregroundWindow = GetForegroundWindow();

            if (foregroundWindow == IntPtr.Zero)
            {
                return null;
            }

            GetWindowThreadProcessId(foregroundWindow, out uint processId);

            if (processId == 0)
            {
                return null;
            }

            try
            {
                using Process process = Process.GetProcessById((int)processId);

                if (process.Id == Environment.ProcessId || IsSelfProcess(process.ProcessName))
                {
                    return null;
                }

                StringBuilder titleBuilder = new StringBuilder(256);
                GetWindowText(foregroundWindow, titleBuilder, titleBuilder.Capacity);

                string processName = process.ProcessName;
                string title = titleBuilder.ToString();

                if (string.IsNullOrWhiteSpace(title))
                {
                    title = processName;
                }

                string categoryName = Categorize(processName, title);
                string key = $"{processName}|{title}";

                return new ForegroundUsage(key, processName, title, categoryName);
            }
            catch
            {
                return null;
            }
        }

        private string Categorize(string processName, string title)
        {
            string text = $"{processName} {title}".ToLowerInvariant();

            if (ContainsAny(text, "chrome", "msedge", "firefox", "brave", "opera"))
            {
                if (ContainsAny(text, "youtube", "netflix", "twitch", "disney", "prime video"))
                {
                    return "Streaming";
                }

                if (ContainsAny(text, "instagram", "tiktok", "reddit", "facebook", "x.com", "twitter"))
                {
                    return "Social Media";
                }

                return "Browsers";
            }

            if (ContainsAny(text, "steam", "epicgames", "roblox", "fortnite", "minecraft", "xbox"))
            {
                return "Gaming";
            }

            if (ContainsAny(text, "teams", "outlook", "discord", "slack", "whatsapp", "telegram"))
            {
                return "Communication";
            }

            if (ContainsAny(text, "word", "excel", "powerpoint", "onenote", "visual studio", "devenv", "code", "notepad"))
            {
                return "Productivity";
            }

            return "Other";
        }

        private bool ContainsAny(string text, params string[] needles)
        {
            return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        private void SeedWeeklyItems()
        {
            string[] labels = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };

            foreach (string label in labels)
            {
                WeeklyUsageItems.Add(new WeeklyUsageItem
                {
                    DayLabel = label
                });
            }
        }

        private void SeedControls()
        {
            AppLimits.Add(new ScreenTimeLimitItem { TargetName = "Social Media", CategoryName = "Social Media", LimitMinutes = 60 });
            AppLimits.Add(new ScreenTimeLimitItem { TargetName = "Games", CategoryName = "Gaming", LimitMinutes = 120 });
            AppLimits.Add(new ScreenTimeLimitItem { TargetName = "Browser", CategoryName = "Browsers", LimitMinutes = 180 });

            DowntimeSchedules.Add(new DowntimeScheduleItem
            {
                Name = "Night downtime",
                Days = "Mon-Fri",
                StartTime = "22:00",
                EndTime = "07:00"
            });

            DowntimeSchedules.Add(new DowntimeScheduleItem
            {
                Name = "Study mode",
                Days = "Every day",
                StartTime = "18:00",
                EndTime = "20:00"
            });

            AlwaysAllowedApps.Add(new AlwaysAllowedAppItem { Name = "Microsoft Teams", ProcessName = "teams" });
            AlwaysAllowedApps.Add(new AlwaysAllowedAppItem { Name = "Outlook", ProcessName = "outlook" });
            AlwaysAllowedApps.Add(new AlwaysAllowedAppItem { Name = "Calculator", ProcessName = "calculator" });
            AlwaysAllowedApps.Add(new AlwaysAllowedAppItem { Name = "Notepad", ProcessName = "notepad" });
        }

        private bool IsSelfProcess(string processName)
        {
            return processName.Equals(ownProcessName, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCategoryColor(string categoryName)
        {
            return CategoryColors.TryGetValue(categoryName, out string? color)
                ? color
                : "#8B95A5";
        }

        private record ForegroundUsage(string Key, string ProcessName, string Title, string CategoryName);

        private class ScreenTimeStorageState
        {
            public List<ScreenTimeStorageDay> Days { get; set; } = new List<ScreenTimeStorageDay>();
        }

        private class ScreenTimeStorageDay
        {
            public string Date { get; set; } = "";
            public List<ScreenTimeStorageApp> Apps { get; set; } = new List<ScreenTimeStorageApp>();
        }

        private class ScreenTimeStorageApp
        {
            public string Key { get; set; } = "";
            public string ProcessName { get; set; } = "";
            public string Title { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public double TotalMinutes { get; set; }
        }
    }
}
