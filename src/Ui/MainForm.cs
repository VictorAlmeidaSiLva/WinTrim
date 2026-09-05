using System;
using System.Drawing;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm : Form
    {
        private static readonly Color BgColor = Color.FromArgb(18, 18, 18);
        private static readonly Color SidebarColor = Color.FromArgb(24, 24, 24);
        private static readonly Color PanelColor = Color.FromArgb(32, 32, 32);
        private static readonly Color TextColor = Color.Gainsboro;
        private static readonly Color MutedColor = Color.FromArgb(150, 150, 150);
        private static readonly Color AccentColor = Color.FromArgb(0, 200, 130);
        private static readonly Color NavActiveColor = Color.FromArgb(0, 90, 65);
        private static readonly Color NavHoverColor = Color.FromArgb(40, 40, 40);

        private OverlayForm overlay;
        private AppConfig config;
        private Action restartAction;
        private Panel contentHost;
        private Button[] navButtons;
        private Panel[] sections;
        private bool[] loaded;
        private int currentSection = -1;
        private Label lblStatus;

        private bool busyOverview, busyServices, busyPrograms, busyTasks, busyRamClean;

        public MainForm(OverlayForm overlayRef, AppConfig cfg, Action restart)
        {
            overlay = overlayRef;
            config = cfg;
            restartAction = restart;

            Text = "WinTrim";
            Size = new Size(Math.Max(config.WindowW, 860), Math.Max(config.WindowH, 560));
            MinimumSize = new Size(860, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgColor;
            Font = new Font("Segoe UI", 9F);

            BuildShell();
            SelectSection(0);

            FormClosed += delegate
            {
                if (overviewTimer != null)
                {
                    overviewTimer.Stop();
                    overviewTimer.Dispose();
                }
            };

            FormClosing += delegate
            {
                if (WindowState == FormWindowState.Normal)
                {
                    config.WindowW = Size.Width;
                    config.WindowH = Size.Height;
                    config.Save();
                }
            };
        }

        private void BuildShell()
        {
            Panel sidebar = new Panel();
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 220;
            sidebar.BackColor = SidebarColor;
            Controls.Add(sidebar);

            Label title = new Label();
            title.Text = "WinTrim";
            title.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            title.ForeColor = TextColor;
            title.Location = new Point(20, 22);
            title.AutoSize = true;
            sidebar.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = Loc.T("shell.subtitle");
            subtitle.Font = new Font("Segoe UI", 8F);
            subtitle.ForeColor = MutedColor;
            subtitle.Location = new Point(21, 52);
            subtitle.AutoSize = true;
            sidebar.Controls.Add(subtitle);

            string[] names = new string[] { Loc.T("nav.overview"), Loc.T("nav.services"), Loc.T("nav.programs"), Loc.T("nav.tasks"), Loc.T("nav.ramclean"), Loc.T("nav.vram"), Loc.T("nav.settings") };
            navButtons = new Button[names.Length];
            int navY = 100;
            for (int i = 0; i < names.Length; i++)
            {
                Button b = new Button();
                b.Text = "   " + names[i];
                b.TextAlign = ContentAlignment.MiddleLeft;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = NavHoverColor;
                b.BackColor = SidebarColor;
                b.ForeColor = TextColor;
                b.Font = new Font("Segoe UI", 10F);
                b.Location = new Point(0, navY);
                b.Size = new Size(220, 42);
                b.Cursor = Cursors.Hand;
                int idx = i;
                b.Click += delegate { SelectSection(idx); };
                sidebar.Controls.Add(b);
                navButtons[i] = b;
                navY += 44;
            }

            contentHost = new Panel();
            contentHost.Dock = DockStyle.Fill;
            contentHost.BackColor = BgColor;
            contentHost.Padding = new Padding(28, 24, 28, 24);
            Controls.Add(contentHost);
            contentHost.BringToFront();

            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 26;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(20, 0, 0, 0);
            lblStatus.ForeColor = MutedColor;
            lblStatus.BackColor = SidebarColor;
            sidebar.Controls.Add(lblStatus);

            sections = new Panel[7];
            loaded = new bool[7];
            sections[0] = BuildOverviewSection();
            sections[1] = BuildServicesSection();
            sections[2] = BuildProgramsSection();
            sections[3] = BuildTasksSection();
            sections[4] = BuildRamCleanSection();
            sections[5] = BuildVramSection();
            sections[6] = BuildConfigSection();

            foreach (Panel s in sections)
            {
                s.Dock = DockStyle.Fill;

                s.AutoScroll = true;
                s.Visible = false;
                contentHost.Controls.Add(s);
            }
        }

        private void SelectSection(int index)
        {
            currentSection = index;
            for (int i = 0; i < navButtons.Length; i++)
                navButtons[i].BackColor = i == index ? NavActiveColor : SidebarColor;
            for (int i = 0; i < sections.Length; i++)
                sections[i].Visible = i == index;

            if (!loaded[index])
            {
                loaded[index] = true;
                if (index == 0) RefreshOverviewAsync();
                else if (index == 1) RefreshServicesAsync();
                else if (index == 2) RefreshProgramsAsync();
                else if (index == 3) RefreshTasksAsync();
                else if (index == 4) RefreshRamCleanAsync();
                else if (index == 5) RefreshVramAsync();
                else if (index == 6) LoadConfigSection();
            }
        }

        private void SetStatus(string text, bool ok)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = ok ? AccentColor : Color.OrangeRed;
        }

        private Label SectionTitle(Panel host, string text)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            l.ForeColor = TextColor;
            l.Location = new Point(0, 0);
            l.AutoSize = true;
            host.Controls.Add(l);
            return l;
        }

        private Panel MakeStatCard(Panel host, string title, int x, int y, int w, int h, out Label valueLabel)
        {
            AccentCard card = new AccentCard(AccentColor);
            card.Location = new Point(x, y);
            card.Size = new Size(w, h);
            card.BackColor = PanelColor;
            host.Controls.Add(card);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.ForeColor = MutedColor;
            lblTitle.Font = new Font("Segoe UI", 8.5F);
            lblTitle.Location = new Point(14, 16);
            lblTitle.AutoSize = true;
            card.Controls.Add(lblTitle);

            Label lblValue = new Label();
            lblValue.Text = "--";
            lblValue.ForeColor = TextColor;
            lblValue.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            lblValue.Location = new Point(14, 36);
            lblValue.AutoSize = true;
            card.Controls.Add(lblValue);

            valueLabel = lblValue;
            return card;
        }

        private void StyleButton(Button b, Color back)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = back;
            b.ForeColor = Color.White;
            b.Cursor = Cursors.Hand;
        }

        private DataGridView MakeGrid()
        {
            DataGridView grid = new DataGridView();
            grid.BackgroundColor = PanelColor;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Color.FromArgb(50, 50, 50);
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.DefaultCellStyle.BackColor = PanelColor;
            grid.DefaultCellStyle.ForeColor = TextColor;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 80);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            grid.RowTemplate.Height = 24;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.None;

            grid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            grid.ReadOnly = true;
            return grid;
        }

        private void AddColumn(DataGridView grid, string name, string header, int width)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = header;
            col.Width = width;
            grid.Columns.Add(col);
        }

        private void AddButtonColumn(DataGridView grid, string name, string header, int width)
        {
            DataGridViewButtonColumn col = new DataGridViewButtonColumn();
            col.Name = name;
            col.HeaderText = header;
            col.Width = width;
            col.UseColumnTextForButtonValue = false;
            col.FlatStyle = FlatStyle.Flat;
            grid.Columns.Add(col);
        }
    }
}
