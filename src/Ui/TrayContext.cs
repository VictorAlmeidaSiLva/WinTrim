using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PcToolkit
{
    public class TrayContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private OverlayForm overlay;
        private MainForm mainForm;
        private AppConfig config;

        public TrayContext(AppConfig cfg)
        {
            config = cfg;
            overlay = new OverlayForm(config);
            overlay.Show();

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem toggleItem = new ToolStripMenuItem();
            toggleItem.Text = Loc.F("tray.toggleOverlay", HotkeyMods.Format(config.HotkeyMod, config.HotkeyVk));
            toggleItem.Click += delegate { overlay.ToggleVisible(); };
            menu.Items.Add(toggleItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Loc.T("tray.openPanel"), null, delegate { OpenMain(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Loc.T("tray.exit"), null, delegate { ExitApp(); });

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "WinTrim";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { OpenMain(); };

            overlay.HotkeyChanged += delegate { toggleItem.Text = Loc.F("tray.toggleOverlay", HotkeyMods.Format(config.HotkeyMod, config.HotkeyVk)); };

            OpenMain();
        }

        private void OpenMain()
        {
            if (mainForm == null || mainForm.IsDisposed)
            {
                mainForm = new MainForm(overlay, config, RestartApp);
                mainForm.Show();
            }
            else
            {
                if (mainForm.WindowState == FormWindowState.Minimized) mainForm.WindowState = FormWindowState.Normal;
                mainForm.Activate();
            }
        }

        private void ExitApp()
        {
            trayIcon.Visible = false;
            RamTools.Cleanup();
            if (overlay != null) overlay.Close();
            if (mainForm != null && !mainForm.IsDisposed) mainForm.Close();
            config.Save();
            ExitThread();
        }

        private void RestartApp()
        {
            try { Process.Start(Application.ExecutablePath); }
            catch { return; }
            ExitApp();
        }
    }
}
