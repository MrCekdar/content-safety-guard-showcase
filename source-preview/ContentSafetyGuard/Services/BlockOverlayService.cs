using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Text;
using System.Windows;
using ContentSafetyGuard.State;
using ContentSafetyGuard.Services.ActivityLog;
using ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.BlockingBehavior;

namespace ContentSafetyGuard.Services
{
    internal class BlockOverlayService
    {
        private MainWindow Window;

        // Feldinstanzen
        private BlockOverlayWindow? blockierWindow; //Die anzeige die Auftritt wenn was blockiert wird
        private ProtectionStateManager Protection;
        private readonly BlockingBehaviorSettingsStateService blockingSettings = new BlockingBehaviorSettingsStateService();
        private readonly ActiveProgramCloseService activeProgramCloseService = new ActiveProgramCloseService();
        private readonly ActivityLogService activityLog = new ActivityLogService();

        public BlockOverlayService(ProtectionStateManager protection, MainWindow _window)
        {
            Protection = protection;
            Window = _window;
        }

        public void ShowBlockOverlay() // wird verwendet um das Blockierungsfenster anzeigen zu lassen
        {
            if (Protection.getProtectionState() && blockierWindow == null) // Schutz ist aktiv und Overlay existiert nicht
            {
                if (blockingSettings.CurrentState.CloseProgramWhenDetected)
                {
                    activeProgramCloseService.TryCloseForegroundProgram();
                }

               Window.Status_Text.Text = "Status: Blocken wird simuliert";
                activityLog.AddEntry(ActivityLogEventType.AiContentBlocked, "AI detection triggered block overlay");
                blockierWindow = new BlockOverlayWindow();
                blockierWindow.Closed += (object? sender, EventArgs e) => { blockierWindow = null;};
                blockierWindow.Show();
            }
            else if (Protection.getProtectionState() && blockierWindow != null) // Schutz ist aktiv und Overlay existiert bereits
            {
                Window.Status_Text.Text = "Status: Overlay ist bereits aktiv!";
            }
            else if (!Protection.getProtectionState()) // Schutz ist nicht aktiv
            {
                Window.Status_Text.Text = "Bitte aktivieren Sie erst Ihren Schutz";
                return;
            }
        }

    }

}
