using System.ComponentModel;

namespace ContentSafetyGuard.Services
{
    public class ScreenTimeUsageItem : INotifyPropertyChanged
    {
        private string title = "";
        private string processName = "";
        private string categoryName = "";
        private double totalMinutes;
        private double barWidth;
        private string color = "#3078E6";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title
        {
            get => title;
            set
            {
                title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public string ProcessName
        {
            get => processName;
            set
            {
                processName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessName)));
            }
        }

        public double TotalMinutes
        {
            get => totalMinutes;
            set
            {
                totalMinutes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalMinutes)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayDuration)));
            }
        }

        public string CategoryName
        {
            get => categoryName;
            set
            {
                categoryName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoryName)));
            }
        }

        public double BarWidth
        {
            get => barWidth;
            set
            {
                barWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BarWidth)));
            }
        }

        public string Color
        {
            get => color;
            set
            {
                color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }

        public string DisplayDuration
        {
            get
            {
                return ScreenTimeCategoryUsageItem.FormatMinutes(TotalMinutes);
            }
        }
    }
}
