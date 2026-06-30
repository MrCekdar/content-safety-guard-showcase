using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace ContentSafetyGuard.View__Interface_.Cards.DNS
{
    public class DnsSettingsService
    {
        private const string SettingsFolderPath = @"C:\ProgramData\ContentSafetyGuard";
        private static readonly string BackupPath = Path.Combine(SettingsFolderPath, "previous-dns-settings.json");

        public List<DnsProviderPresetState> GetProviderPresets()
        {
            return new List<DnsProviderPresetState>
            {
                new DnsProviderPresetState { Name = "OpenDNS FamilyShield", Servers = new List<string> { "208.67.222.123", "208.67.220.123" } },
                new DnsProviderPresetState { Name = "Cloudflare Family DNS", Servers = new List<string> { "1.1.1.3", "1.0.0.3" } },
                new DnsProviderPresetState { Name = "CleanBrowsing Family DNS", Servers = new List<string> { "185.228.168.168", "185.228.169.168" } },
                new DnsProviderPresetState { Name = "AdGuard DNS", Servers = new List<string> { "94.140.14.15", "94.140.15.16" } },
                new DnsProviderPresetState { Name = "Custom DNS", Servers = new List<string>() }
            };
        }

        // Hier wird DNS Aktiviert
        public void ApplyDnsServers(IEnumerable<string> servers)
        {
            List<string> dnsServers = servers
                .Where(server => !string.IsNullOrWhiteSpace(server))
                .Select(server => server.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (dnsServers.Count == 0)
            {
                return;
            }

            BackupCurrentDnsSettings();
            string joinedServers = string.Join(",", dnsServers.Select(server => $"'{server}'"));

            RunPowerShellCommand(
                $"Get-NetAdapter | Where-Object {{$_.Status -eq 'Up'}} | ForEach-Object {{ Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses @({joinedServers}) }}");
        }

        // DNS EINSTELLUNG ZU AUTOMATISCH WIEDER 
        public void ResetDnsToAutomatic()
        {
            RunPowerShellCommand(
                "Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ResetServerAddresses }");

            RunPowerShellCommand("ipconfig /flushdns");
        }

        /// <summary>
        ///  Wiederherstelle meine DNS Einstellungen
        /// </summary>
        public bool RestorePreviousDnsSettings()
        {
            if (!File.Exists(BackupPath))
            {
                return false;
            }

            string json = File.ReadAllText(BackupPath);
            List<AdapterDnsBackup>? backups = JsonSerializer.Deserialize<List<AdapterDnsBackup>>(json);

            if (backups == null || backups.Count == 0)
            {
                return false;
            }

            foreach (AdapterDnsBackup backup in backups)
            {
                if (backup.ServerAddresses.Count == 0)
                {
                    RunPowerShellCommand($"Set-DnsClientServerAddress -InterfaceIndex {backup.InterfaceIndex} -ResetServerAddresses");
                    continue;
                }

                string joinedServers = string.Join(",", backup.ServerAddresses.Select(server => $"'{server}'"));
                RunPowerShellCommand($"Set-DnsClientServerAddress -InterfaceIndex {backup.InterfaceIndex} -ServerAddresses @({joinedServers})");
            }

            return true;
        }


        // Speicherderzeitiges DNS Einstellungen 
        private void BackupCurrentDnsSettings()
        {
            Directory.CreateDirectory(SettingsFolderPath);

            string command =
                "Get-DnsClientServerAddress -AddressFamily IPv4 | " +
                "Select-Object InterfaceIndex,ServerAddresses | ConvertTo-Json -Depth 3";

            string output = RunPowerShellCommand(command);
            File.WriteAllText(BackupPath, output);
        }


     
        private string RunPowerShellCommand(string command)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo)!;
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(error);
            }

            return output;
        }

        private class AdapterDnsBackup
        {
            public int InterfaceIndex { get; set; }
            public List<string> ServerAddresses { get; set; } = new List<string>();
        }


    }
}
