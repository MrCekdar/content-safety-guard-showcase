using ContentSafetyGuard.State;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;

namespace ContentSafetyGuard.Services
{
    internal class ScreenCaptureService
    {

        private ProtectionStateManager Protection;

        public ScreenCaptureService(ProtectionStateManager protection)
        {
            Protection = protection;
        }

        public Bitmap? CaptureCurrentScreenFrame()
        { // Funktion um ein Screenshot zu machen 
            if (Protection.getProtectionState())
            {
                double screenLeft = SystemParameters.VirtualScreenLeft;
                double screenTop = SystemParameters.VirtualScreenTop;
                double screenWidth = SystemParameters.VirtualScreenWidth;
                double screenHeight = SystemParameters.VirtualScreenHeight;

                Bitmap image = new Bitmap((int)screenWidth, (int)screenHeight);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, image.Size);
                }
                return image;
            }
            else
            {
                return null;
            }
        }

    }
}
