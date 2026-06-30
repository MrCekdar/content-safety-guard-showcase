using System;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace ContentSafetyGuard.Services
{
    internal class SystemTrayService : IDisposable
    {
        private readonly Forms.NotifyIcon notifyIcon;

        public SystemTrayService(Action openRequested, Action exitRequested)
        {
            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open Content Safety Guard", null, (_, _) => openRequested());
            menu.Items.Add("Exit", null, (_, _) => exitRequested());

            notifyIcon = new Forms.NotifyIcon
            {
                Text = "Content Safety Guard",
                Icon = SystemIcons.Shield,
                ContextMenuStrip = menu,
                Visible = false
            };

            notifyIcon.DoubleClick += (_, _) => openRequested();
        }

        public void SetVisible(bool visible)
        {
            notifyIcon.Visible = visible;
        }

        public void Dispose()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
}
