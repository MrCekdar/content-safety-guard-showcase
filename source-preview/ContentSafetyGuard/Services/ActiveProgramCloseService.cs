using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ContentSafetyGuard.Services
{
    internal class ActiveProgramCloseService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public bool TryCloseForegroundProgram()
        {
            IntPtr foregroundWindow = GetForegroundWindow();

            if (foregroundWindow == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(foregroundWindow, out uint processId);

            if (processId == 0 || processId == Environment.ProcessId)
            {
                return false;
            }

            try
            {
                using Process process = Process.GetProcessById((int)processId);

                if (IsProtectedProcess(process))
                {
                    return false;
                }

                return process.CloseMainWindow();
            }
            catch
            {
                return false;
            }
        }

        private bool IsProtectedProcess(Process process)
        {
            string processName = process.ProcessName;
            string[] protectedNames =
            {
                "explorer",
                "ShellExperienceHost",
                "StartMenuExperienceHost",
                "SearchHost",
                "TextInputHost",
                "ApplicationFrameHost",
                "ContentSafetyGuard"
            };

            if (protectedNames.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string processPath = process.MainModule?.FileName ?? "";

                return processPath.StartsWith(windowsPath, StringComparison.OrdinalIgnoreCase)
                       || !File.Exists(processPath);
            }
            catch
            {
                return true;
            }
        }
    }
}
