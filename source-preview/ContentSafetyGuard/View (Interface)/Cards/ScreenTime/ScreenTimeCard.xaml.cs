using ContentSafetyGuard.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.ScreenTime
{
    public partial class ScreenTimeCard : UserControl
    {
        private const int InitialVisibleApps = 5;
        private const int AppPageSize = 5;

        private readonly ScreenTimeService screenTimeService = new ScreenTimeService();
        private readonly ObservableCollection<ScreenTimeUsageItem> displayedUsageItems = new ObservableCollection<ScreenTimeUsageItem>();
        private BlockOverlayWindow? screenTimeBlockOverlay;
        private int visibleAppCount = InitialVisibleApps;

        public ScreenTimeCard()
        {
            InitializeComponent();
            DataContext = screenTimeService;

            UsageList.ItemsSource = displayedUsageItems;
            CategoryUsageList.ItemsSource = screenTimeService.CategoryUsageItems;
            WeeklyUsageList.ItemsSource = screenTimeService.WeeklyUsageItems;

            screenTimeService.PropertyChanged += ScreenTimeService_PropertyChanged;
            screenTimeService.TopUsageItems.CollectionChanged += TopUsageItems_CollectionChanged;
            screenTimeService.CategoryUsageItems.CollectionChanged += CategoryUsageItems_CollectionChanged;
            screenTimeService.BlockingRequested += ScreenTimeService_BlockingRequested;

            Loaded += ScreenTimeCard_Loaded;
            Unloaded += ScreenTimeCard_Unloaded;
        }

        private void ScreenTimeCard_Loaded(object sender, RoutedEventArgs e)
        {
            screenTimeService.Start();
            TotalScreenTimeText.Text = screenTimeService.TotalScreenTimeToday;
            RefreshDisplayedUsageItems();
            UpdateEmptyStates();
        }

        private void ScreenTimeCard_Unloaded(object sender, RoutedEventArgs e)
        {
            screenTimeService.Stop();
        }

        private void ScreenTimeService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScreenTimeService.TotalScreenTimeToday))
            {
                TotalScreenTimeText.Text = screenTimeService.TotalScreenTimeToday;
            }
        }

        private void TopUsageItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshDisplayedUsageItems();
            UpdateEmptyStates();
        }

        private void CategoryUsageItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateEmptyStates();
        }

        private void ShowMore_Click(object sender, RoutedEventArgs e)
        {
            visibleAppCount = System.Math.Min(
                visibleAppCount + AppPageSize,
                screenTimeService.TopUsageItems.Count);

            RefreshDisplayedUsageItems();
            UpdateEmptyStates();
        }

        private void ShowLess_Click(object sender, RoutedEventArgs e)
        {
            visibleAppCount = InitialVisibleApps;

            RefreshDisplayedUsageItems();
            UpdateEmptyStates();
        }

        private void ScreenTimeService_BlockingRequested(object? sender, string reason)
        {
            if (screenTimeBlockOverlay != null)
            {
                return;
            }

            screenTimeBlockOverlay = new BlockOverlayWindow(
                "Access blocked",
                reason);

            screenTimeBlockOverlay.Closed += (_, _) => screenTimeBlockOverlay = null;
            screenTimeBlockOverlay.Show();
        }

        private void RefreshDisplayedUsageItems()
        {
            displayedUsageItems.Clear();

            foreach (ScreenTimeUsageItem item in screenTimeService.TopUsageItems.Take(visibleAppCount))
            {
                displayedUsageItems.Add(item);
            }

            bool canExpand = screenTimeService.TopUsageItems.Count > displayedUsageItems.Count;
            bool canCollapse = visibleAppCount > InitialVisibleApps && displayedUsageItems.Count > InitialVisibleApps;

            ShowMoreButton.Visibility = canExpand ? Visibility.Visible : Visibility.Collapsed;
            ShowLessButton.Visibility = canCollapse ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateEmptyStates()
        {
            EmptyStateText.Visibility = displayedUsageItems.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            CategoryEmptyText.Visibility = screenTimeService.CategoryUsageItems.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
