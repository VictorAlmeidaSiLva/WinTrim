using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm
    {
        private Label lblVramValue, lblRamValue, lblSvcValue, lblProgValue;
        private Button btnToggleOverlay;
        private CheckBox chkAutoStart;
        private Timer overviewTimer;

        private Panel BuildOverviewSection()
        {
            Panel host = new Panel();
            SectionTitle(host, Loc.T("nav.overview"));

            Label valVram, valRam, valSvc, valProg;
            MakeStatCard(host, Loc.T("overview.vramInUse"), 0, 50, 190, 88, out valVram);
            MakeStatCard(host, Loc.T("overview.ramInUse"), 200, 50, 190, 88, out valRam);
            MakeStatCard(host, Loc.T("overview.autoServices"), 400, 50, 190, 88, out valSvc);
            MakeStatCard(host, Loc.T("programs.title"), 600, 50, 190, 88, out valProg);
            lblVramValue = valVram; lblRamValue = valRam; lblSvcValue = valSvc; lblProgValue = valProg;

            AccentCard overlayCard = new AccentCard(AccentColor);
            overlayCard.Location = new Point(0, 160);
            overlayCard.Size = new Size(790, 110);
            overlayCard.BackColor = PanelColor;
            host.Controls.Add(overlayCard);

            Label lblOv = new Label();
            lblOv.Text = Loc.T("overlay.card.title");
            lblOv.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblOv.ForeColor = TextColor;
            lblOv.Location = new Point(16, 14);
            lblOv.AutoSize = true;
            overlayCard.Controls.Add(lblOv);

            Label lblOvDesc = new Label();
            lblOvDesc.Text = Loc.T("overlay.card.desc");
            lblOvDesc.ForeColor = MutedColor;
            lblOvDesc.Location = new Point(16, 40);
            lblOvDesc.AutoSize = true;
            overlayCard.Controls.Add(lblOvDesc);

            btnToggleOverlay = new Button();
            btnToggleOverlay.Text = Loc.T("overlay.btn.hide");
            btnToggleOverlay.Location = new Point(16, 68);
            btnToggleOverlay.Size = new Size(150, 30);
            StyleButton(btnToggleOverlay, Color.FromArgb(55, 55, 55));
            btnToggleOverlay.Click += delegate
            {
                overlay.ToggleVisible();
                btnToggleOverlay.Text = overlay.IsOverlayVisible ? Loc.T("overlay.btn.hide") : Loc.T("overlay.btn.show");
            };
            overlayCard.Controls.Add(btnToggleOverlay);

            chkAutoStart = new CheckBox();
            chkAutoStart.Text = Loc.T("overview.chkAutoStart");
            chkAutoStart.Location = new Point(190, 74);
            chkAutoStart.Size = new Size(320, 22);
            chkAutoStart.ForeColor = TextColor;
            chkAutoStart.Checked = AutoStartHelper.IsEnabled();
            chkAutoStart.CheckedChanged += ChkAutoStart_CheckedChanged;
            overlayCard.Controls.Add(chkAutoStart);

            overviewTimer = new Timer();
            overviewTimer.Interval = 3000;
            overviewTimer.Tick += delegate { if (currentSection == 0) RefreshOverviewAsync(); };
            overviewTimer.Start();

            return host;
        }

        private async void ChkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            bool enable = chkAutoStart.Checked;
            bool ok = await Task.Run(new Func<bool>(delegate
            {
                try { AutoStartHelper.SetEnabled(enable); return true; }
                catch { return ElevationHelper.RunElevated("self-autostart", enable ? "enable" : "disable"); }
            }));
            SetStatus(ok ? Loc.T(enable ? "overview.autostart.on" : "overview.autostart.off") : Loc.T("overview.autostart.fail"), ok);
        }

        private async void RefreshOverviewAsync()
        {
            if (busyOverview) return;
            busyOverview = true;
            try
            {
                lblVramValue.Text = overlay.LastVramPct.ToString("0") + "%";
                lblRamValue.Text = overlay.LastRamPct.ToString("0") + "%";
                btnToggleOverlay.Text = overlay.IsOverlayVisible ? Loc.T("overlay.btn.hide") : Loc.T("overlay.btn.show");

                List<ServiceRow> svc = await Task.Run(new Func<List<ServiceRow>>(delegate { return ServiceHelper.GetServices(false); }));
                lblSvcValue.Text = svc.Count.ToString();

                List<ProgramRow> prog = await Task.Run(new Func<List<ProgramRow>>(RegistryHelper.GetAll));
                int enabledCount = prog.Count(p => p.Enabled);
                lblProgValue.Text = enabledCount + " / " + prog.Count;
            }
            finally { busyOverview = false; }
        }
    }
}
