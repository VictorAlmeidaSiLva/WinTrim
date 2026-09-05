using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm
    {
        private Label lblRamTotal, lblRamUsed, lblRamAvail, lblRamStandby, lblRamModified, lblRamCommitted, lblRamPool;
        private DataGridView gridRamProcs;
        private Button btnCleanRam;
        private Label lblCleanStatus;
        private MemReport lastRamReport;
        private double lastTotalProcGB;

        private Panel BuildRamCleanSection()
        {
            Panel host = new Panel();
            SectionTitle(host, Loc.T("nav.ramclean"));

            AccentCard statsCard = new AccentCard(AccentColor);
            statsCard.Location = new Point(0, 40);
            statsCard.Size = new Size(788, 150);
            statsCard.BackColor = PanelColor;
            host.Controls.Add(statsCard);

            lblRamTotal = RamStatLabel(statsCard, Loc.T("ramclean.stat.total"), 12, 10);
            lblRamUsed = RamStatLabel(statsCard, Loc.T("ramclean.stat.usedReal"), 12, 34);
            lblRamAvail = RamStatLabel(statsCard, Loc.T("ramclean.stat.available"), 12, 58);
            lblRamStandby = RamStatLabel(statsCard, Loc.T("ramclean.stat.standby"), 12, 82);
            lblRamModified = RamStatLabel(statsCard, Loc.T("ramclean.stat.modified"), 12, 104);
            lblRamCommitted = RamStatLabel(statsCard, Loc.T("ramclean.stat.committed"), 400, 10);
            lblRamPool = RamStatLabel(statsCard, Loc.T("ramclean.stat.pool"), 400, 34);

            btnCleanRam = new Button();
            btnCleanRam.Text = Loc.T("ramclean.btn.clean");
            btnCleanRam.Location = new Point(0, 200);
            btnCleanRam.Size = new Size(180, 34);
            StyleButton(btnCleanRam, AccentColor);
            btnCleanRam.Click += BtnCleanRam_Click;
            host.Controls.Add(btnCleanRam);

            Button btnRefreshRam = new Button();
            btnRefreshRam.Text = Loc.T("common.refresh");
            btnRefreshRam.Location = new Point(190, 200);
            btnRefreshRam.Size = new Size(120, 34);
            StyleButton(btnRefreshRam, Color.FromArgb(55, 55, 55));
            btnRefreshRam.Click += delegate { RefreshRamCleanAsync(); };
            host.Controls.Add(btnRefreshRam);

            lblCleanStatus = new Label();
            lblCleanStatus.Location = new Point(0, 244);
            lblCleanStatus.Size = new Size(788, 40);
            lblCleanStatus.ForeColor = MutedColor;
            lblCleanStatus.Font = new Font("Segoe UI", 8F);
            lblCleanStatus.Text = Loc.T("ramclean.desc");
            host.Controls.Add(lblCleanStatus);

            Label gridTitle = new Label();
            gridTitle.Text = Loc.T("ramclean.gridTitle");
            gridTitle.ForeColor = TextColor;
            gridTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            gridTitle.Location = new Point(0, 292);
            gridTitle.AutoSize = true;
            host.Controls.Add(gridTitle);

            gridRamProcs = MakeGrid();
            gridRamProcs.Location = new Point(0, 318);
            gridRamProcs.Size = new Size(788, 250);
            AddColumn(gridRamProcs, "Processo", Loc.T("ramclean.col.process"), 300);
            AddColumn(gridRamProcs, "RamMb", Loc.T("ramclean.col.privateRam"), 160);
            host.Controls.Add(gridRamProcs);

            return host;
        }

        private Label RamStatLabel(Panel parent, string title, int x, int y)
        {
            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.ForeColor = TextColor;
            lblTitle.Location = new Point(x, y);
            lblTitle.Size = new Size(230, 20);
            parent.Controls.Add(lblTitle);

            Label lblValue = new Label();
            lblValue.Text = "--";
            lblValue.ForeColor = AccentColor;
            lblValue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblValue.Location = new Point(x + 232, y);
            lblValue.Size = new Size(150, 20);
            parent.Controls.Add(lblValue);

            return lblValue;
        }

        private async void RefreshRamCleanAsync()
        {
            if (busyRamClean) return;
            busyRamClean = true;
            try
            {
                MemReport r = await Task.Run(new Func<MemReport>(RamTools.GetMemReport));
                lastRamReport = r;
                double usedGB = r.TotalGB - r.AvailableGB;
                double pct = r.TotalGB > 0 ? (usedGB / r.TotalGB) * 100.0 : 0;

                lblRamTotal.Text = r.TotalGB.ToString("0.0") + " GB";
                lblRamUsed.Text = usedGB.ToString("0.00") + " GB (" + pct.ToString("0") + "%)";
                lblRamUsed.ForeColor = pct >= 85 ? Color.OrangeRed : (pct >= 65 ? Color.Gold : AccentColor);
                lblRamAvail.Text = r.AvailableGB.ToString("0.00") + " GB";
                lblRamStandby.Text = r.DetalheOk ? r.StandbyGB.ToString("0.00") + " GB" : Loc.T("ramclean.unavailable");
                lblRamModified.Text = r.DetalheOk ? r.ModifiedGB.ToString("0.00") + " GB" : Loc.T("ramclean.unavailable");
                lblRamCommitted.Text = r.DetalheOk ? r.CommittedGB.ToString("0.00") + " GB" : Loc.T("ramclean.unavailable");
                lblRamPool.Text = r.DetalheOk ? r.PoolPagedGB.ToString("0.00") + " / " + r.PoolNonPagedGB.ToString("0.00") + " GB" : Loc.T("ramclean.unavailable");

                List<ProcRam> rows = await Task.Run(new Func<List<ProcRam>>(delegate
                {
                    double tg;
                    List<ProcRam> res = RamTools.GetTopPrivateWorkingSet(20, out tg);
                    lastTotalProcGB = tg;
                    return res;
                }));

                gridRamProcs.SuspendLayout();
                gridRamProcs.Rows.Clear();
                foreach (ProcRam p in rows) gridRamProcs.Rows.Add(p.Name, p.MB.ToString("0.0"));
                gridRamProcs.ResumeLayout();

                lblCleanStatus.Text = Loc.F("ramclean.summary", lastTotalProcGB.ToString("0.00"), Loc.T("ramclean.desc"));
            }
            finally { busyRamClean = false; }
        }

        private async void BtnCleanRam_Click(object sender, EventArgs e)
        {
            if (busyRamClean) return;
            btnCleanRam.Enabled = false;
            SetStatus(Loc.T("ramclean.cleaning"), true);

            double beforeAvail = lastRamReport.AvailableGB;
            string resultado = await Task.Run(new Func<string>(delegate
            {
                try { return RamTools.PurgeAll(); }
                catch
                {
                    bool ok = ElevationHelper.RunElevated("ram-purge");
                    return ok ? Loc.T("ramclean.purgeElevated") : Loc.T("ramclean.purgeFailed");
                }
            }));
            await Task.Delay(1500);
            RefreshRamCleanAsync();
            await Task.Delay(300);

            double delta = lastRamReport.AvailableGB - beforeAvail;
            SetStatus(Loc.F("ramclean.doneAvailable", resultado, delta >= 0 ? "+" : "", delta), true);
            btnCleanRam.Enabled = true;
        }
    }
}
