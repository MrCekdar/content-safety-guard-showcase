using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode
{
    public class ProgramBlockService
    {
        public ProgramBlockService()
        {

        }

        public void SaveBlockedPrograms(IEnumerable<string> exepaths)
        {
            List<string> blockedexePaths = exepaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(path => File.Exists(path))
                .ToList();
   
            BackupCurrentAppLockerPolicy();

            string policyPath = CreateAppLockerPolicy(blockedexePaths);

            ApplyAppLockerPolicy(policyPath);
        }

        public void ClearBlockedPrograms()
        {
            string policyPath = CreateResetAppLockerPolicy();
            ApplyAppLockerPolicy(policyPath);
        }

        private XElement CreateAllowRule(string name, string sid, string path)
        {
            return new XElement("FilePathRule",
                new XAttribute("Id", Guid.NewGuid()),
                new XAttribute("Name", name),
                new XAttribute("Description", "Default allow rule"),
                new XAttribute("UserOrGroupSid", sid),
                new XAttribute("Action", "Allow"),
                new XElement("Conditions",
                new XElement("FilePathCondition",
                new XAttribute("Path", path))));
        }

        private XElement CreateDenyRule(string exePath)
        {
            return new XElement("FilePathRule",
                new XAttribute("Id", Guid.NewGuid()),
                new XAttribute("Name", $"ContentSafetyGuard block {Path.GetFileName(exePath)}"),
                new XAttribute("Description", "Blocked by ContentSafetyGuard"),
                new XAttribute("UserOrGroupSid", "S-1-1-0"),
                new XAttribute("Action", "Deny"),
                new XElement("Conditions",
                    new XElement("FilePathCondition",
                        new XAttribute("Path", exePath))));
        }

        // Erlaube erstamall alle Programme an dem Benutzer
        private void AddDefaultAllowRules(XElement ruleCollection)
        {
            ruleCollection.Add(
                CreateAllowRule("Allow all executable files", "S-1-1-0", "*")
            );
        }

        private string CreateAppLockerPolicy(List<string> blockedExePaths)
        {
            string folderPath = @"C:\ProgramData\ContentSafetyGuard";
            Directory.CreateDirectory(folderPath);

            string policyPath = Path.Combine(folderPath, "content-safety-guard-applocker-policy.xml");

            XElement exeRuleCollection = new XElement("RuleCollection",
                new XAttribute("Type", "Exe"),
                new XAttribute("EnforcementMode", "Enabled"));

            AddDefaultAllowRules(exeRuleCollection);

            foreach (string exePath in blockedExePaths)
            {
                if (IsProtectedWindowsProgram(exePath))
                {
                    continue;
                }

                exeRuleCollection.Add(CreateDenyRule(exePath));
            }

            XDocument policy = new XDocument(
                new XElement("AppLockerPolicy",
                    new XAttribute("Version", "1"),
                    exeRuleCollection));

            policy.Save(policyPath);

            return policyPath;
        }

        private string CreateResetAppLockerPolicy()
        {
            string folderPath = @"C:\ProgramData\ContentSafetyGuard";
            Directory.CreateDirectory(folderPath);

            string policyPath = Path.Combine(folderPath, "reset-applocker-policy.xml");

            XDocument policy = new XDocument(
                new XElement("AppLockerPolicy",
                    new XAttribute("Version", "1"),
                    CreateNotConfiguredRuleCollection("Exe"),
                    CreateNotConfiguredRuleCollection("Msi"),
                    CreateNotConfiguredRuleCollection("Script"),
                    CreateNotConfiguredRuleCollection("Dll"),
                    CreateNotConfiguredRuleCollection("Appx")));

            policy.Save(policyPath);

            return policyPath;
        }

        private XElement CreateNotConfiguredRuleCollection(string type)
        {
            return new XElement("RuleCollection",
                new XAttribute("Type", type),
                new XAttribute("EnforcementMode", "NotConfigured"));
        }

        private bool IsProtectedWindowsProgram(string exePath)
        {
            string fullPath = Path.GetFullPath(exePath);

            string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            string[] protectedNames =
            {
                "explorer.exe",
                "StartMenuExperienceHost.exe",
                "ShellExperienceHost.exe",
                "SearchHost.exe",
                "TextInputHost.exe",
                "ApplicationFrameHost.exe",
                "cmd.exe",
                "SnippingTool.exe",
                "ScreenClippingHost.exe",
                "OneDrive.exe",
                "powershell.exe",
                "msedge.exe",
                "WindowsTerminal.exe",
                "SnippingTool.exe"
        };

            string fileName = Path.GetFileName(fullPath);

            return fullPath.StartsWith(windowsPath, StringComparison.OrdinalIgnoreCase)
                   || protectedNames.Contains(fileName, StringComparer.OrdinalIgnoreCase);
        }

        private void RunPowerShellCommand(string command)
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
            process.WaitForExit();

            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(error);
            }
        }

        private void ApplyAppLockerPolicy(string policyPath)
        {
            RunPowerShellCommand("sc.exe config AppIDSvc start= auto");
            RunPowerShellCommand("$svc = Get-Service AppIDSvc; if ($svc.Status -ne 'Running') { Start-Service AppIDSvc }");
            RunPowerShellCommand($"Set-AppLockerPolicy -XmlPolicy '{policyPath}'");
        }

        // Diese Methode speichert erstmal die aktuelle lokale AppLocker-Policy als XML:
        private void BackupCurrentAppLockerPolicy()
        {
            string folderPath = @"C:\ProgramData\ContentSafetyGuard";
            Directory.CreateDirectory(folderPath);

            string backupPath = Path.Combine(
                folderPath,
                $"applocker-backup-{DateTime.Now:yyyyMMdd-HHmmss}.xml");

            RunPowerShellCommand(
                $"Get-AppLockerPolicy -Local -Xml | Set-Content -Path '{backupPath}' -Encoding UTF8");
        }

    }
}
