using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ContentSafetyGuard.Services
{
    public class ScreenTimeCategoryUsageItem : INotifyPropertyChanged
    {
        private double totalMinutes;
        private double barWidth;
        private string color = "#3078E6";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CategoryName { get; set; } = "";

        public double TotalMinutes
        {
            get => totalMinutes;
            set
            {
                totalMinutes = value;
                OnChanged(nameof(TotalMinutes));
                OnChanged(nameof(DisplayDuration));
            }
        }

        public double BarWidth
        {
            get => barWidth;
            set
            {
                barWidth = value;
                OnChanged(nameof(BarWidth));
            }
        }

        public string Color
        {
            get => color;
            set
            {
                color = value;
                OnChanged(nameof(Color));
            }
        }

        public string DisplayDuration => FormatMinutes(TotalMinutes);

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal static string FormatMinutes(double minutes)
        {
            if (minutes < 1)
            {
                return "< 1 min";
            }

            int roundedMinutes = Math.Max(1, (int)Math.Round(minutes, MidpointRounding.AwayFromZero));

            if (roundedMinutes < 60)
            {
                return $"{roundedMinutes} min";
            }

            int hours = roundedMinutes / 60;
            int remainingMinutes = roundedMinutes % 60;

            return remainingMinutes == 0
                ? $"{hours}h"
                : $"{hours}h {remainingMinutes}m";
        }
    }

    public class WeeklyUsageItem : INotifyPropertyChanged
    {
        private double totalMinutes;
        private double barHeight = 8;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string DayLabel { get; set; } = "";
        public ObservableCollection<WeeklyUsageSegmentItem> Segments { get; } = new ObservableCollection<WeeklyUsageSegmentItem>();

        public double TotalMinutes
        {
            get => totalMinutes;
            set
            {
                totalMinutes = value;
                OnChanged(nameof(TotalMinutes));
                OnChanged(nameof(DisplayDuration));
            }
        }

        public double BarHeight
        {
            get => barHeight;
            set
            {
                barHeight = value;
                OnChanged(nameof(BarHeight));
            }
        }

        public string DisplayDuration => ScreenTimeCategoryUsageItem.FormatMinutes(TotalMinutes);

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WeeklyUsageSegmentItem
    {
        public string CategoryName { get; set; } = "";
        public double SegmentHeight { get; set; }
        public string Color { get; set; } = "#3078E6";
    }

    public class ScreenTimeLimitItem : INotifyPropertyChanged
    {
        private bool isEnabled;
        private int limitMinutes;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string TargetName { get; set; } = "";
        public string CategoryName { get; set; } = "";

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                OnChanged(nameof(IsEnabled));
            }
        }

        public int LimitMinutes
        {
            get => limitMinutes;
            set
            {
                limitMinutes = value;
                OnChanged(nameof(LimitMinutes));
                OnChanged(nameof(DisplayLimit));
            }
        }

        public string DisplayLimit => ScreenTimeCategoryUsageItem.FormatMinutes(LimitMinutes);

        private void OnChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DowntimeScheduleItem : INotifyPropertyChanged
    {
        private bool isEnabled;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; } = "";
        public string Days { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }

        public string DisplayRange => $"{Days}, {StartTime}-{EndTime}";
    }

    public class AlwaysAllowedAppItem
    {
        public string Name { get; set; } = "";
        public string ProcessName { get; set; } = "";
    }
}
