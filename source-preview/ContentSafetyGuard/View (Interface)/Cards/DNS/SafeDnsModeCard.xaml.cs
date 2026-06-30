using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ContentSafetyGuard.View__Interface_.Cards.DNS
{
    public partial class SafeDnsModeCard : UserControl
    {
        private readonly DnsSettingsService dnsSettingsService = new DnsSettingsService();


        public SafeDnsModeCard()
        {

            InitializeComponent();

            ProviderComboBox.ItemsSource = dnsSettingsService.GetProviderPresets();
            ProviderComboBox.SelectedIndex = 0;


            ApplyProState();
            ProviderComboBox.SelectionChanged += ProviderComboBox_SelectionChanged;
        }

        private void ApplyProState()
        {
            LockedMessage.Visibility = Visibility.Collapsed;
            ProviderComboBox.IsEnabled = true;
            CustomDnsTextBox.IsEnabled = true;
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CustomDnsTextBox.Visibility = IsCustomDnsSelected() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyDns_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "This will change your Windows DNS settings. Do you want to continue?",
                "Apply Safe DNS Mode",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            List<string> servers = GetSelectedServers(); // Custom DNS 
            dnsSettingsService.ApplyDnsServers(servers);
            MessageBox.Show("DNS settings were updated.", "Content Safety Guard");
        }

        private void ResetDns_Click(object sender, RoutedEventArgs e)
{
    try
    {
        dnsSettingsService.ResetDnsToAutomatic();

        MessageBox.Show(
            "DNS settings were reset to automatic.",
            "Content Safety Guard");
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            "DNS settings could not be reset:\n\n" + ex.Message,
            "Content Safety Guard",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}

        private void RestorePrevious_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool restored = dnsSettingsService.RestorePreviousDnsSettings();

                if (!restored)
                {
                    MessageBox.Show("No previous DNS settings were found.", "Content Safety Guard");
                    return;
                }

                MessageBox.Show("Previous DNS settings were restored.", "Content Safety Guard");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "DNS settings could not be restored:\n\n" + ex.Message,
                    "Content Safety Guard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // Liest unser DNS VON CUSTOM DNS
        private List<string> GetSelectedServers()
        {
            if (IsCustomDnsSelected())
            {
                return CustomDnsTextBox.Text
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            if (ProviderComboBox.SelectedItem is DnsProviderPresetState preset)
            {
                return preset.Servers;
            }

            return new List<string>();
        }

        private bool IsCustomDnsSelected()
        {
            return ProviderComboBox.SelectedItem is DnsProviderPresetState preset &&
                   string.Equals(preset.Name, "Custom DNS", StringComparison.OrdinalIgnoreCase);
        }


    }
}
