using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PcToolkit
{
    public class OverlayForm : Form
    {
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_LAYERED = 0x00080000;

        private Label label;
        private Timer timer;
        private double vramTotalGB;
        private PerformanceCounter[] counters;
        private PerformanceCounter cpuCounter;
        private bool visibleState = true;
        private AppConfig config;
        private uint hotkeyMod;
        private uint hotkeyVk;
        private bool hotkeyRegistered;
        private bool dragging;
        private Point dragOffset;

        public event EventHandler HotkeyChanged;
        public uint HotkeyMod { get { return hotkeyMod; } }
        public uint HotkeyVk { get { return hotkeyVk; } }

        public double LastCpuPct { get; private set; }
        public double LastVramPct { get; private set; }
        public double LastRamPct { get; private set; }
        public double LastVramUsedGB { get; private set; }
        public double LastRamUsedGB { get; private set; }

        public OverlayForm(AppConfig cfg)
        {
            config = cfg;
            hotkeyMod = config.HotkeyMod;
            hotkeyVk = config.HotkeyVk;
            vramTotalGB = 8.0;
            try
            {
                string regPath = @"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";
                RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key != null)
                {
                    object qword = key.GetValue("HardwareInformation.qwMemorySize");
                    if (qword != null)
                    {
                        long bytes = Convert.ToInt64(qword);

                        if (bytes > 0) vramTotalGB = Math.Round(bytes / 1024.0 / 1024.0 / 1024.0, 1);
                    }
                    key.Close();
                }
            }
            catch { }

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            Opacity = ClampOpacity(config.OverlayOpacity);

            Size = new Size(280, 80);
            MinimumSize = new Size(280, 80);

            label = new Label();
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font("Consolas", 10, FontStyle.Bold);
            label.ForeColor = Color.Lime;
            label.BackColor = Color.Black;
            label.Text = Loc.T("overlay.initial");
            label.Cursor = Cursors.SizeAll;
            label.MouseDown += Label_MouseDown;
            label.MouseMove += Label_MouseMove;
            label.MouseUp += Label_MouseUp;
            Controls.Add(label);

            Load += delegate
            {
                PositionFromConfigOrDefault();
                hotkeyRegistered = RegisterHotKey(Handle, HOTKEY_ID, hotkeyMod, hotkeyVk);
                InitCounters();
            };

            FormClosing += delegate { if (hotkeyRegistered) UnregisterHotKey(Handle, HOTKEY_ID); };

            Resize += delegate
            {
                if (visibleState && WindowState == FormWindowState.Minimized)
                    BeginInvoke((MethodInvoker)delegate { ReapplyVisibility(); });
            };

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += delegate { ReapplyVisibility(); UpdateReading(); };
            timer.Start();
        }

        private void PositionFromConfigOrDefault()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            if (config.OverlayX >= 0 && config.OverlayX <= area.Width - 40 && config.OverlayY >= 0 && config.OverlayY <= area.Height - 20)
                Location = new Point(config.OverlayX, config.OverlayY);
            else
                Location = new Point(area.Width - Size.Width - 12, 12);
        }

        public void ResetPosition()
        {
            config.OverlayX = -1;
            config.OverlayY = 12;
            PositionFromConfigOrDefault();
            config.Save();
        }

        private void Label_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragOffset = e.Location;
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point screenPt = PointToScreen(e.Location);
            Location = new Point(screenPt.X - dragOffset.X, screenPt.Y - dragOffset.Y);
        }

        private void Label_MouseUp(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            dragging = false;
            config.OverlayX = Location.X;
            config.OverlayY = Location.Y;
            config.Save();
        }

        public void SetOpacity(double opacity)
        {
            opacity = ClampOpacity(opacity);
            Opacity = opacity;
            config.OverlayOpacity = opacity;
            config.Save();
        }

        private static double ClampOpacity(double opacity)
        {
            if (opacity > 0.99) return 0.99;
            if (opacity < 0.20) return 0.20;
            return opacity;
        }

        public void SetHotkey(uint mod, uint vk)
        {
            if (hotkeyRegistered) UnregisterHotKey(Handle, HOTKEY_ID);
            hotkeyMod = mod;
            hotkeyVk = vk;
            hotkeyRegistered = RegisterHotKey(Handle, HOTKEY_ID, hotkeyMod, hotkeyVk);
            config.HotkeyMod = mod;
            config.HotkeyVk = vk;
            config.Save();
            if (HotkeyChanged != null) HotkeyChanged(this, EventArgs.Empty);
        }

        private void ReapplyVisibility()
        {
            if (!visibleState)
            {
                if (Visible) Visible = false;
                return;
            }
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            if (!Visible) Visible = true;
            if (!TopMost) TopMost = true;
        }

        private void InitCounters()
        {
            try
            {
                PerformanceCounterCategory cat = new PerformanceCounterCategory("GPU Adapter Memory");
                string[] instances = cat.GetInstanceNames();
                counters = instances.Select(n => new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", n, true)).ToArray();
            }
            catch { counters = new PerformanceCounter[0]; }

            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                cpuCounter.NextValue(); 
            }
            catch { cpuCounter = null; }

            UpdateReading();
        }

        public bool IsOverlayVisible { get { return visibleState; } }

        public void ToggleVisible()
        {
            visibleState = !visibleState;
            ReapplyVisibility();
        }

        private void UpdateReading()
        {
            string cpuLine;
            int cpuPct = 0;
            try
            {
                if (cpuCounter != null)
                {
                    cpuPct = (int)Math.Round(cpuCounter.NextValue());
                    cpuLine = Loc.F("overlay.cpu", cpuPct);
                }
                else cpuLine = Loc.T("overlay.cpu.unavailable");
            }
            catch { cpuLine = Loc.T("overlay.cpu.error"); }

            string vramLine;
            int vramPct = 0;
            double vramUsedGB = 0;
            try
            {
                double usedBytes = 0;
                if (counters != null)
                {
                    foreach (PerformanceCounter c in counters)
                    {
                        try { usedBytes += c.NextValue(); } catch { }
                    }
                }
                vramUsedGB = usedBytes / 1024.0 / 1024.0 / 1024.0;
                vramPct = vramTotalGB > 0 ? (int)Math.Round((vramUsedGB / vramTotalGB) * 100.0) : 0;
                vramLine = Loc.F("overlay.vram", vramUsedGB, vramTotalGB, vramPct);
            }
            catch { vramLine = Loc.T("overlay.vram.error"); }

            string ramLine;
            int ramPct = 0;
            double ramUsedGB = 0;
            try
            {
                MemReport mr = RamTools.GetMemReport();
                ramUsedGB = mr.TotalGB - mr.AvailableGB;
                ramPct = mr.TotalGB > 0 ? (int)Math.Round((ramUsedGB / mr.TotalGB) * 100.0) : 0;
                ramLine = Loc.F("overlay.ram", ramUsedGB, mr.TotalGB, ramPct);
            }
            catch { ramLine = Loc.T("overlay.ram.error"); }

            label.Text = cpuLine + "\n" + vramLine + "\n" + ramLine;
            LastCpuPct = cpuPct; LastVramPct = vramPct; LastRamPct = ramPct; LastVramUsedGB = vramUsedGB; LastRamUsedGB = ramUsedGB;

            int worstPct = Math.Max(cpuPct, Math.Max(vramPct, ramPct));
            if (worstPct >= 75) label.ForeColor = Color.Red;
            else if (worstPct >= 50) label.ForeColor = Color.Yellow;
            else label.ForeColor = Color.Lime;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) ToggleVisible();
            base.WndProc(ref m);
        }
    }
}
