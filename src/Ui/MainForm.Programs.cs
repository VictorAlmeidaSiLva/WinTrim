using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm
    {
        private DataGridView gridPrograms;
        private TextBox txtSearchPrograms;
        private List<ProgramRow> allProgramRows = new List<ProgramRow>();

        private Panel BuildProgramsSection()
        {
            Panel host = new Panel();
            SectionTitle(host, Loc.T("programs.title"));

            Button btnRefreshProg = new Button();
            btnRefreshProg.Text = Loc.T("common.refresh");
            btnRefreshProg.Location = new Point(0, 40);
            btnRefreshProg.Size = new Size(110, 28);
            StyleButton(btnRefreshProg, Color.FromArgb(55, 55, 55));
            btnRefreshProg.Click += delegate { RefreshProgramsAsync(); };
            host.Controls.Add(btnRefreshProg);

            Label lblSearchProg = new Label();
            lblSearchProg.Text = Loc.T("common.search");
            lblSearchProg.ForeColor = MutedColor;
            lblSearchProg.Location = new Point(130, 46);
            lblSearchProg.AutoSize = true;
            host.Controls.Add(lblSearchProg);

            txtSearchPrograms = new TextBox();
            txtSearchPrograms.Location = new Point(180, 42);
            txtSearchPrograms.Size = new Size(220, 24);
            txtSearchPrograms.TextChanged += delegate { ApplyProgramsFilter(); };
            host.Controls.Add(txtSearchPrograms);

            gridPrograms = MakeGrid();
            gridPrograms.Location = new Point(0, 76);
            gridPrograms.Size = new Size(788, 444);
            AddColumn(gridPrograms, "Name", Loc.T("programs.col.name"), 170);
            AddColumn(gridPrograms, "Scope", Loc.T("programs.col.scope"), 120);
            AddColumn(gridPrograms, "Command", Loc.T("programs.col.command"), 290);
            AddButtonColumn(gridPrograms, "InfoBtn", "Info", 50);
            AddButtonColumn(gridPrograms, "ToggleBtn", Loc.T("common.action"), 110);
            gridPrograms.CellContentClick += GridPrograms_CellContentClick;
            host.Controls.Add(gridPrograms);

            return host;
        }

        private async void RefreshProgramsAsync()
        {
            if (busyPrograms) return;
            busyPrograms = true;
            SetStatus(Loc.T("programs.loading"), true);
            try
            {
                allProgramRows = await Task.Run(new Func<List<ProgramRow>>(RegistryHelper.GetAll));
                ApplyProgramsFilter();
                SetStatus(Loc.F("programs.loaded", allProgramRows.Count), true);
            }
            finally { busyPrograms = false; }
        }

        private void ApplyProgramsFilter()
        {
            string filter = txtSearchPrograms != null ? txtSearchPrograms.Text.Trim() : "";
            gridPrograms.SuspendLayout();
            gridPrograms.Rows.Clear();
            foreach (ProgramRow r in allProgramRows)
            {
                if (filter.Length > 0
                    && (r.Name == null || r.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    && (r.Command == null || r.Command.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                int idx = gridPrograms.Rows.Add();
                DataGridViewRow row = gridPrograms.Rows[idx];
                row.Cells["Name"].Value = r.Name;
                row.Cells["Scope"].Value = r.Scope;
                row.Cells["Command"].Value = r.Command;
                row.Cells["InfoBtn"].Value = "?";
                row.Cells["ToggleBtn"].Value = r.Enabled ? Loc.T("common.disable") : Loc.T("common.enable");
                row.Tag = r;
            }
            gridPrograms.ResumeLayout();
        }

        private async void GridPrograms_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || busyPrograms) return;
            ProgramRow r = gridPrograms.Rows[e.RowIndex].Tag as ProgramRow;
            if (r == null) return;

            if (gridPrograms.Columns[e.ColumnIndex].Name == "InfoBtn")
            {
                string desc = WindowsStartupCatalog.Lookup(r.Name);
                MessageBox.Show(
                    desc != null ? desc : Loc.F("programs.noInfo", r.Command),
                    r.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (gridPrograms.Columns[e.ColumnIndex].Name != "ToggleBtn") return;

            bool enableNow = !r.Enabled;
            bool ok;
            busyPrograms = true;
            try
            {
                SetStatus(Loc.F("common.changing", r.Name), true);
                ok = await Task.Run(new Func<bool>(delegate
                {
                    try { RegistryHelper.SetApproved(r.ApprovedHive, r.ApprovedSubkey, r.Name, enableNow); return true; }
                    catch { return ElevationHelper.RunElevated("reg-toggle", r.ApprovedHive, r.ApprovedSubkey, r.Name, enableNow ? "enable" : "disable"); }
                }));
            }
            finally { busyPrograms = false; }

            SetStatus(ok ? Loc.F(enableNow ? "programs.enabled" : "programs.disabled", r.Name) : Loc.F("common.changeFailed", r.Name), ok);
            if (ok) RefreshProgramsAsync();
        }
    }
}
