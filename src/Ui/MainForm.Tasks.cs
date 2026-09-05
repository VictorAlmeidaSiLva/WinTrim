using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm
    {
        private DataGridView gridTasks;
        private TextBox txtSearchTasks;
        private List<TaskRow> allTaskRows = new List<TaskRow>();

        private Panel BuildTasksSection()
        {
            Panel host = new Panel();
            SectionTitle(host, Loc.T("tasks.title"));

            Button btnRefreshTasks = new Button();
            btnRefreshTasks.Text = Loc.T("common.refresh");
            btnRefreshTasks.Location = new Point(0, 40);
            btnRefreshTasks.Size = new Size(110, 28);
            StyleButton(btnRefreshTasks, Color.FromArgb(55, 55, 55));
            btnRefreshTasks.Click += delegate { RefreshTasksAsync(); };
            host.Controls.Add(btnRefreshTasks);

            Label lblSearchTasks = new Label();
            lblSearchTasks.Text = Loc.T("common.search");
            lblSearchTasks.ForeColor = MutedColor;
            lblSearchTasks.Location = new Point(130, 46);
            lblSearchTasks.AutoSize = true;
            host.Controls.Add(lblSearchTasks);

            txtSearchTasks = new TextBox();
            txtSearchTasks.Location = new Point(180, 42);
            txtSearchTasks.Size = new Size(220, 24);
            txtSearchTasks.TextChanged += delegate { ApplyTasksFilter(); };
            host.Controls.Add(txtSearchTasks);

            gridTasks = MakeGrid();
            gridTasks.Location = new Point(0, 76);
            gridTasks.Size = new Size(788, 444);
            AddColumn(gridTasks, "Path", Loc.T("tasks.col.task"), 460);
            AddColumn(gridTasks, "Triggers", Loc.T("tasks.col.trigger"), 100);
            AddButtonColumn(gridTasks, "ToggleBtn", Loc.T("common.action"), 120);
            gridTasks.CellContentClick += GridTasks_CellContentClick;
            host.Controls.Add(gridTasks);

            return host;
        }

        private async void RefreshTasksAsync()
        {
            if (busyTasks) return;
            busyTasks = true;
            SetStatus(Loc.T("tasks.loading"), true);
            try
            {
                allTaskRows = await Task.Run(new Func<List<TaskRow>>(TaskHelper.GetLogonBootTasks));
                ApplyTasksFilter();
                SetStatus(Loc.F("tasks.loaded", allTaskRows.Count), true);
            }
            finally { busyTasks = false; }
        }

        private void ApplyTasksFilter()
        {
            string filter = txtSearchTasks != null ? txtSearchTasks.Text.Trim() : "";
            gridTasks.SuspendLayout();
            gridTasks.Rows.Clear();
            foreach (TaskRow r in allTaskRows)
            {
                if (filter.Length > 0 && (r.Path == null || r.Path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                int idx = gridTasks.Rows.Add();
                DataGridViewRow row = gridTasks.Rows[idx];
                row.Cells["Path"].Value = r.Path;
                row.Cells["Triggers"].Value = r.Triggers;
                row.Cells["ToggleBtn"].Value = r.Enabled ? Loc.T("common.disable") : Loc.T("common.enable");
                row.Tag = r;
            }
            gridTasks.ResumeLayout();
        }

        private async void GridTasks_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || busyTasks) return;
            TaskRow r = gridTasks.Rows[e.RowIndex].Tag as TaskRow;
            if (r == null) return;
            if (gridTasks.Columns[e.ColumnIndex].Name != "ToggleBtn") return;

            bool enableNow = !r.Enabled;
            bool ok;
            busyTasks = true;
            try
            {
                SetStatus(Loc.F("common.changing", r.Path), true);
                ok = await Task.Run(new Func<bool>(delegate
                {
                    try { TaskHelper.SetEnabled(r.Path, enableNow); return true; }
                    catch { return ElevationHelper.RunElevated("task-toggle", r.Path, enableNow ? "enable" : "disable"); }
                }));
            }
            finally { busyTasks = false; }

            SetStatus(ok ? Loc.F(enableNow ? "tasks.enabled" : "tasks.disabled", r.Path) : Loc.F("common.changeFailed", r.Path), ok);
            if (ok) RefreshTasksAsync();
        }
    }
}
