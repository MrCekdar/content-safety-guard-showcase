using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.FocusMode
{
    public class ExceptionLogic : INotifyPropertyChanged
    {
        private string programName = "";
        private string iconPath = "";
        private PackIconKind iconKind = PackIconKind.Application;
        private bool isBlocked;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ProgramName
        {
            get => programName;
            set
            {
                programName = value;
                OnPropertyChanged(nameof(ProgramName));
            }
        }

        public string IconPath
        {
            get => iconPath;
            set
            {
                iconPath = value;
                OnPropertyChanged(nameof(IconPath));
                OnPropertyChanged(nameof(IconImage));
            }
        }

        public string ExePath
        {
            get => exePath;
            set
            {
                exePath = GetExePath(value) ?? "";
                OnPropertyChanged(nameof(ExePath));
                OnPropertyChanged(nameof(HasExecutableTarget));
            }
        }

        public bool HasExecutableTarget =>
            !string.IsNullOrWhiteSpace(ExePath) &&
            ExePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

        public PackIconKind IconKind
        {
            get => iconKind;
            set
            {
                iconKind = value;
                OnPropertyChanged(nameof(IconKind));
            }
        }


        // Wenn die Programm beschränkung auf an ist oder auf aus 
        public bool IsBlocked
        {
            get => isBlocked;
            set
            {
                isBlocked = value;
                OnPropertyChanged(nameof(IsBlocked));
            }
        }

        public ImageSource? IconImage
        {

            get
            {
                string? filePath = GetIconFilePath(IconPath);

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return null;
                }

                using Icon? icon = Icon.ExtractAssociatedIcon(filePath);

                if (icon == null)
                {
                    return null;
                }

                BitmapSource image = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(18, 18));

                image.Freeze();
                return image;
            }
        }


        // Hier bekommen wir die Icon File Path: Meistens sowas: "C:\Program Files\App\App.exe",0
        private string? GetIconFilePath(string rawIconPath)
        {
            
            if (string.IsNullOrWhiteSpace(rawIconPath))
            {
                return null;
            }

            string expandedPath = Environment.ExpandEnvironmentVariables(rawIconPath.Trim());

            if (expandedPath.StartsWith("\""))
            {
                int closingQuoteIndex = expandedPath.IndexOf('"', 1);

                if (closingQuoteIndex > 1)
                {
                    return expandedPath.Substring(1, closingQuoteIndex - 1);
                }
            }

            return expandedPath.Split(',')[0].Trim();
        }

        private string exePath = "";

            private string? GetExePath(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) { 
                    return null;
                }

                // Zwei Varianten: "C:\App\App.exe",0 ; C:\App\App.exe,0
            string exepath = Environment.ExpandEnvironmentVariables(path.Trim());

            // hier bearbeiten wir das String Beispiel: "\"C:\\App\\App.exe\",0"
            if (exepath.StartsWith("\"")) // Beginnt exepath mit einem Anführungszeichen?
            {
                    int closingQuoteIndex = exepath.IndexOf('"', 1);

                    if (closingQuoteIndex > 1)
                    {
                        return exepath.Substring(1, closingQuoteIndex - 1);
                    }
                }

                return exepath.Split(',')[0].Trim();
            }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
