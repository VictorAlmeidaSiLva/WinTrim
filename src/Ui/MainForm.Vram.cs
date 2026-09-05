using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm
    {
        private CheckBox chkHags;
        private DataGridView gridVramProcs;
        private List<VramProcRow> allVramRows = new List<VramProcRow>();
        private bool busyVram;
        private bool loadingHagsCheckbox;

        private Panel BuildVramSection()
        {
            Panel host = new Panel();
            SectionTitle(host, Loc.T("nav.vram"));

            AccentCard hagsCard = new AccentCard(AccentColor);
            hagsCard.Location = new Point(0, 40);
            hagsCard.Size = new Size(788, 92);
            hagsCard.BackColor = PanelColor;
            host.Controls.Add(hagsCard);

            Label lblHagsTitle = new Label();
            lblHagsTitle.Text = Loc.T("vram.hags.title");
            lblHagsTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblHagsTitle.ForeColor = TextColor;
            lblHagsTitle.Location = new Point(16, 12);
            lblHagsTitle.AutoSize = true;
            hagsCard.Controls.Add(lblHagsTitle);

            Label lblHagsDesc = new Label();
            lblHagsDesc.Text = Loc.T("vram.hags.desc");
            lblHagsDesc.ForeColor = MutedColor;
            lblHagsDesc.Location = new Point(16, 36);
            lblHagsDesc.Size = new Size(750, 32);
            hagsCard.Controls.Add(lblHagsDesc);

            chkHags = new CheckBox();
            chkHags.Text = Loc.T("vram.hags.checkbox");
            chkHags.ForeColor = TextColor;
            chkHags.Location = new Point(16, 68);
            chkHags.Size = new Size(200, 20);
            chkHags.CheckedChanged += ChkHags_CheckedChanged;
            hagsCard.Controls.Add(chkHags);

            Label gridTitle = new Label();
            gridTitle.Text = Loc.T("vram.gridTitle");
            gridTitle.ForeColor = TextColor;
            gridTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            gridTitle.Location = new Point(0, 148);
            gridTitle.AutoSize = true;
            host.Controls.Add(gridTitle);

            Button btnRefreshVram = new Button();
            btnRefreshVram.Text = Loc.T("common.refresh");
            btnRefreshVram.Location = new Point(688, 142);
            btnRefreshVram.Size = new Size(100, 28);
            StyleButton(btnRefreshVram, Color.FromArgb(55, 55, 55));
            btnRefreshVram.Click += delegate { RefreshVramAsync(); };
            host.Controls.Add(btnRefreshVram);

            gridVramProcs = MakeGrid();
            gridVramProcs.Location = new Point(0, 176);
            gridVramProcs.Size = new Size(788, 380);
            AddColumn(gridVramProcs, "Process", Loc.T("ramclean.col.process"), 190);
            AddColumn(gridVramProcs, "VramMb", "VRAM (MB)", 90);
            AddColumn(gridVramProcs, "Pref", Loc.T("vram.col.pref"), 150);
            AddButtonColumn(gridVramProcs, "PrefBtn", Loc.T("common.action"), 150);
            AddButtonColumn(gridVramProcs, "CloseBtn", Loc.T("vram.close"), 90);
            gridVramProcs.CellContentClick += GridVram_CellContentClick;
            host.Controls.Add(gridVramProcs);

            return host;
        }

        private async void RefreshVramAsync()
        {
            if (busyVram) return;
            busyVram = true;
            SetStatus(Loc.T("vram.loading"), true);
            try
            {
                loadingHagsCheckbox = true;
                chkHags.Checked = await Task.Run(new Func<bool>(VramTools.GetHagsEnabled));
                loadingHagsCheckbox = false;

                allVramRows = await Task.Run(new Func<List<VramProcRow>>(delegate { return VramTools.GetTopVramProcesses(20); }));

                gridVramProcs.SuspendLayout();
                gridVramProcs.Rows.Clear();
                foreach (VramProcRow r in allVramRows)
                {
                    int idx = gridVramProcs.Rows.Add();
                    DataGridViewRow row = gridVramProcs.Rows[idx];
                    row.Cells["Process"].Value = r.ProcessName;
                    row.Cells["VramMb"].Value = r.DedicatedMb.ToString("0.0");
                    row.Cells["Pref"].Value = PrefName(r.GpuPreference);
                    row.Cells["PrefBtn"].Value = r.ExePath != null ? Loc.F("vram.setPrefTo", PrefName(NextPref(r.GpuPreference))) : "-";
                    row.Cells["CloseBtn"].Value = VramTools.IsProtectedProcess(r.ProcessName) ? "-" : Loc.T("vram.close");
                    row.Tag = r;
                }
                gridVramProcs.ResumeLayout();

                SetStatus(allVramRows.Count > 0 ? Loc.F("vram.loaded", allVramRows.Count) : Loc.T("vram.unavailable"), true);
            }
            finally { busyVram = false; }
        }

        private static int NextPref(int current)
        {
            return (current + 1) % 3;
        }

        private static string PrefName(int pref)
        {
            if (pref == 1) return Loc.T("vram.pref.powersaving");
            if (pref == 2) return Loc.T("vram.pref.highperf");
            return Loc.T("vram.pref.default");
        }

        private async void GridVram_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || busyVram) return;
            VramProcRow r = gridVramProcs.Rows[e.RowIndex].Tag as VramProcRow;
            if (r == null) return;
            string colName = gridVramProcs.Columns[e.ColumnIndex].Name;

            if (colName == "PrefBtn")
            {
                if (r.ExePath == null)
                {
                    SetStatus(Loc.T("vram.prefUnavailable"), false);
                    return;
                }
                int newPref = NextPref(r.GpuPreference);
                string exePath = r.ExePath;
                busyVram = true;
                try
                {
                    await Task.Run(delegate { VramTools.SetGpuPreference(exePath, newPref); });
                }
                finally { busyVram = false; }
                SetStatus(Loc.F("vram.prefChanged", r.ProcessName, PrefName(newPref)), true);
                RefreshVramAsync();
                return;
            }

            if (colName == "CloseBtn")
            {
                if (VramTools.IsProtectedProcess(r.ProcessName)) return;

                DialogResult confirm = MessageBox.Show(
                    Loc.F("vram.confirm.close", r.ProcessName),
                    Loc.T("vram.confirm.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                busyVram = true;
                SetStatus(Loc.F("vram.closing", r.ProcessName), true);
                bool ok;
                int pid = r.Pid;
                try
                {
                    ok = await Task.Run(new Func<bool>(delegate
                    {
                        try { VramTools.KillProcess(pid); return true; }
                        catch { return ElevationHelper.RunElevated("kill-process", pid.ToString()); }
                    }));
                }
                finally { busyVram = false; }

                SetStatus(ok ? Loc.F("vram.closed", r.ProcessName) : Loc.F("vram.closeFailed", r.ProcessName), ok);
                if (ok) RefreshVramAsync();
            }
        }

        private async void ChkHags_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingHagsCheckbox) return;
            bool enable = chkHags.Checked;
            bool ok = await Task.Run(new Func<bool>(delegate
            {
                try { VramTools.SetHagsEnabled(enable); return true; }
                catch { return ElevationHelper.RunElevated("hags-toggle", enable ? "enable" : "disable"); }
            }));
            SetStatus(ok ? Loc.T(enable ? "vram.hags.changedOn" : "vram.hags.changedOff") : Loc.F("common.changeFailed", Loc.T("vram.hags.title")), ok);
        }
    }
}
