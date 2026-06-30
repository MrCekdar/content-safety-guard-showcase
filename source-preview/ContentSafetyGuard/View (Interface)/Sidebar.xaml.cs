using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContentSafetyGuard.View__Interface_
{
    /// <summary>
    /// Interaktionslogik für Sidebar.xaml
    /// </summary>
    public partial class Sidebar : UserControl
    {
       
        public Sidebar()
        {
            InitializeComponent();
       
        }

        private void SetActiveNavButton(Button activeButton)
        {
            DashBoardNavButton.Background = Brushes.Transparent;
            InternetNavButton.Background = Brushes.Transparent;
            KiNavButton.Background = Brushes.Transparent;
            SettingsNavButton.Background = Brushes.Transparent;
            InfoNavButton.Background = Brushes.Transparent;

            DashBoardNavButton.BorderThickness = new Thickness(0);
            InternetNavButton.BorderThickness = new Thickness(0);
            KiNavButton.BorderThickness = new Thickness(0);
            SettingsNavButton.BorderThickness = new Thickness(0);
            InfoNavButton.BorderThickness = new Thickness(0);

            activeButton.Background = new SolidColorBrush(Color.FromRgb(26, 37, 56));
            activeButton.BorderBrush = (Brush)FindResource("AccentBlue");
            activeButton.BorderThickness = new Thickness(1);
        }

        
        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(DashBoardNavButton);
            
            MainWindow mainwindow = (MainWindow)Window.GetWindow(this);
            mainwindow.ShowDashboardView();
        }

        private void InternetButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(InternetNavButton);

            MainWindow mainwindow = (MainWindow)Window.GetWindow(this);
            mainwindow.ShowInternetView();
        }

        private void KiButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(KiNavButton);

            MainWindow mainwindow = (MainWindow)Window.GetWindow(this);
            mainwindow.ShowKiView();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(SettingsNavButton);

            MainWindow mainwindow = (MainWindow)Window.GetWindow(this);
            mainwindow.ShowSettingsView();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(InfoNavButton);
            
            MainWindow mainwindow = (MainWindow)Window.GetWindow(this);
            mainwindow.ShowInfoView();
        }

    }

      

    }
