using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm
    {
        private DataGridView gridServices;
        private CheckBox chkAllServices;
        private TextBox txtSearchServices;
        private List<ServiceRow> allServiceRows = new List<ServiceRow>();

        private Panel BuildServicesSection()
        {
            Panel host = new Panel();
            SectionTitle(host, Loc.T("nav.services"));

            chkAllServices = new CheckBox();
            chkAllServices.Text = Loc.T("services.showAll");
            chkAllServices.Location = new Point(0, 40);
            chkAllServices.Size = new Size(280, 22);
            chkAllServices.ForeColor = TextColor;
            chkAllServices.Checked = config.ShowAllServices;
            chkAllServices.CheckedChanged += delegate { config.ShowAllServices = chkAllServices.Checked; config.Save(); RefreshServicesAsync(); };
            host.Controls.Add(chkAllServices);

            Label lblSearchSvc = new Label();
            lblSearchSvc.Text = Loc.T("common.search");
            lblSearchSvc.ForeColor = MutedColor;
            lblSearchSvc.Location = new Point(300, 43);
            lblSearchSvc.AutoSize = true;
            host.Controls.Add(lblSearchSvc);

            txtSearchServices = new TextBox();
            txtSearchServices.Location = new Point(350, 39);
            txtSearchServices.Size = new Size(220, 24);
            txtSearchServices.TextChanged += delegate { ApplyServicesFilter(); };
            host.Controls.Add(txtSearchServices);

            gridServices = MakeGrid();
            gridServices.Location = new Point(0, 70);
            gridServices.Size = new Size(788, 450);
            AddColumn(gridServices, "DisplayName", Loc.T("services.col.name"), 240);
            AddColumn(gridServices, "State", "Status", 70);
            AddColumn(gridServices, "StartMode", Loc.T("services.col.startup"), 70);
            AddColumn(gridServices, "RamMb", "RAM (MB)", 80);
            AddButtonColumn(gridServices, "InfoBtn", "Info", 50);
            AddButtonColumn(gridServices, "ToggleBtn", Loc.T("common.action"), 120);
            AddButtonColumn(gridServices, "StopBtn", Loc.T("services.col.stopNow"), 110);
            gridServices.CellContentClick += GridServices_CellContentClick;
            host.Controls.Add(gridServices);

            return host;
        }

        private async void RefreshServicesAsync()
        {
            if (busyServices) return;
            busyServices = true;
            bool all = chkAllServices.Checked;
            SetStatus(Loc.T("services.loading"), true);
            try
            {
                allServiceRows = await Task.Run(new Func<List<ServiceRow>>(delegate { return ServiceHelper.GetServices(all); }));
                ApplyServicesFilter();
                SetStatus(Loc.F("services.loaded", allServiceRows.Count), true);
            }
            finally { busyServices = false; }
        }

        private void ApplyServicesFilter()
        {
            string filter = txtSearchServices != null ? txtSearchServices.Text.Trim() : "";
            gridServices.SuspendLayout();
            gridServices.Rows.Clear();
            foreach (ServiceRow r in allServiceRows)
            {
                if (filter.Length > 0
                    && (r.DisplayName == null || r.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    && (r.Name == null || r.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                int idx = gridServices.Rows.Add();
                DataGridViewRow row = gridServices.Rows[idx];
                row.Cells["DisplayName"].Value = r.DisplayName;
                row.Cells["State"].Value = r.State;
                row.Cells["StartMode"].Value = r.StartMode;
                row.Cells["RamMb"].Value = r.RamMb >= 0 ? r.RamMb.ToString("0.0") : "-";
                row.Cells["InfoBtn"].Value = "?";
                row.Cells["ToggleBtn"].Value = r.StartMode == "Auto" ? Loc.T("common.disable") : Loc.T("common.enable");
                row.Cells["StopBtn"].Value = r.State == "Running" ? Loc.T("services.stop") : "-";
                row.Tag = r;

                ServiceCatalogEntry info = WindowsServiceCatalog.Lookup(r.Name);
                if (info != null && info.Critical)
                    row.Cells["DisplayName"].Style.ForeColor = Color.FromArgb(255, 120, 120);
            }
            gridServices.ResumeLayout();
        }

        private async void GridServices_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || busyServices) return;
            ServiceRow r = gridServices.Rows[e.RowIndex].Tag as ServiceRow;
            if (r == null) return;
            string colName = gridServices.Columns[e.ColumnIndex].Name;

            ServiceCatalogEntry info = WindowsServiceCatalog.Lookup(r.Name);

            if (colName == "InfoBtn")
            {
                MessageBox.Show(
                    info != null ? info.Description : Loc.T("services.noInfo"),
                    r.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (colName != "ToggleBtn" && colName != "StopBtn") return;
            if (colName == "StopBtn" && r.State != "Running") return;

            if (info != null && info.Critical)
            {
                DialogResult confirm = MessageBox.Show(
                    Loc.F("services.confirm.body", info.Description),
                    Loc.F("services.confirm.title", r.DisplayName), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
            }

            bool ok;
            string successMsg, failMsg;
            busyServices = true;
            try
            {
                if (colName == "ToggleBtn")
                {
                    bool disabling = r.StartMode == "Auto";
                    string newMode = disabling ? "Manual" : "Auto";
                    SetStatus(Loc.F("common.changing", r.Name), true);
                    ok = await Task.Run(new Func<bool>(delegate
                    {
                        try { ServiceHelper.SetStartMode(r.Name, newMode); return true; }
                        catch { return ElevationHelper.RunElevated("svc-mode", r.Name, newMode); }
                    }));
                    successMsg = Loc.F("services.changed", r.Name, newMode);
                    failMsg = Loc.F("common.changeFailed", r.Name);
                }
                else
                {
                    SetStatus(Loc.F("services.stopping", r.Name), true);
                    ok = await Task.Run(new Func<bool>(delegate
                    {
                        try { ServiceHelper.Stop(r.Name); return true; }
                        catch { return ElevationHelper.RunElevated("svc-stop", r.Name); }
                    }));
                    successMsg = Loc.F("services.stopped", r.Name);
                    failMsg = Loc.F("services.stopFailed", r.Name);
                }
            }
            finally { busyServices = false; }

            SetStatus(ok ? successMsg : failMsg, ok);
            if (ok) RefreshServicesAsync();
        }
    }
}
