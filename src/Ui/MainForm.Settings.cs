using System.Drawing;
using System.Windows.Forms;

namespace PcToolkit
{
    public partial class MainForm
    {
        private TextBox txtHotkey;
        private TrackBar trkOpacity;
        private Label lblOpacityValue;
        private ComboBox cmbLanguage;
        private bool loadingLanguageCombo;

        private Panel BuildConfigSection()
        {
            Panel host = new Panel();
            SectionTitle(host, Loc.T("nav.settings"));

            AccentCard hotkeyCard = new AccentCard(AccentColor);
            hotkeyCard.Location = new Point(0, 50);
            hotkeyCard.Size = new Size(560, 110);
            hotkeyCard.BackColor = PanelColor;
            host.Controls.Add(hotkeyCard);

            Label lblHk = new Label();
            lblHk.Text = Loc.T("settings.hotkey.title");
            lblHk.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblHk.ForeColor = TextColor;
            lblHk.Location = new Point(16, 14);
            lblHk.AutoSize = true;
            hotkeyCard.Controls.Add(lblHk);

            Label lblHkDesc = new Label();
            lblHkDesc.Text = Loc.T("settings.hotkey.desc");
            lblHkDesc.ForeColor = MutedColor;
            lblHkDesc.Location = new Point(16, 38);
            lblHkDesc.Size = new Size(520, 32);
            hotkeyCard.Controls.Add(lblHkDesc);

            txtHotkey = new TextBox();
            txtHotkey.ReadOnly = true;
            txtHotkey.Location = new Point(16, 72);
            txtHotkey.Size = new Size(220, 24);
            txtHotkey.TextAlign = HorizontalAlignment.Center;
            txtHotkey.KeyDown += TxtHotkey_KeyDown;
            hotkeyCard.Controls.Add(txtHotkey);

            AccentCard opacityCard = new AccentCard(AccentColor);
            opacityCard.Location = new Point(0, 176);
            opacityCard.Size = new Size(560, 100);
            opacityCard.BackColor = PanelColor;
            host.Controls.Add(opacityCard);

            Label lblOp = new Label();
            lblOp.Text = Loc.T("settings.opacity.title");
            lblOp.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblOp.ForeColor = TextColor;
            lblOp.Location = new Point(16, 14);
            lblOp.AutoSize = true;
            opacityCard.Controls.Add(lblOp);

            trkOpacity = new TrackBar();
            trkOpacity.Minimum = 20;
            trkOpacity.Maximum = 99;
            trkOpacity.TickFrequency = 10;
            trkOpacity.Location = new Point(12, 42);
            trkOpacity.Size = new Size(340, 40);
            trkOpacity.Scroll += delegate
            {
                overlay.SetOpacity(trkOpacity.Value / 100.0);
                lblOpacityValue.Text = trkOpacity.Value + "%";
            };
            opacityCard.Controls.Add(trkOpacity);

            lblOpacityValue = new Label();
            lblOpacityValue.ForeColor = AccentColor;
            lblOpacityValue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblOpacityValue.Location = new Point(360, 50);
            lblOpacityValue.AutoSize = true;
            opacityCard.Controls.Add(lblOpacityValue);

            AccentCard languageCard = new AccentCard(AccentColor);
            languageCard.Location = new Point(0, 292);
            languageCard.Size = new Size(560, 96);
            languageCard.BackColor = PanelColor;
            host.Controls.Add(languageCard);

            Label lblLang = new Label();
            lblLang.Text = Loc.T("settings.language.title");
            lblLang.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblLang.ForeColor = TextColor;
            lblLang.Location = new Point(16, 14);
            lblLang.AutoSize = true;
            languageCard.Controls.Add(lblLang);

            Label lblLangDesc = new Label();
            lblLangDesc.Text = Loc.T("settings.language.desc");
            lblLangDesc.ForeColor = MutedColor;
            lblLangDesc.Location = new Point(16, 38);
            lblLangDesc.Size = new Size(520, 20);
            languageCard.Controls.Add(lblLangDesc);

            cmbLanguage = new ComboBox();
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.DrawMode = DrawMode.OwnerDrawFixed;
            cmbLanguage.ItemHeight = 22;
            cmbLanguage.Location = new Point(16, 62);
            cmbLanguage.Size = new Size(240, 24);
            cmbLanguage.Items.Add("English");
            cmbLanguage.Items.Add("Portugues (Brasil)");
            cmbLanguage.DrawItem += CmbLanguage_DrawItem;
            cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
            languageCard.Controls.Add(cmbLanguage);

            Button btnResetPos = new Button();
            btnResetPos.Text = Loc.T("settings.resetPos");
            btnResetPos.Location = new Point(0, 402);
            btnResetPos.Size = new Size(260, 30);
            StyleButton(btnResetPos, Color.FromArgb(55, 55, 55));
            btnResetPos.Click += delegate { overlay.ResetPosition(); SetStatus(Loc.T("settings.resetPos.done"), true); };
            host.Controls.Add(btnResetPos);

            Label lblHint = new Label();
            lblHint.Text = Loc.T("settings.dragHint");
            lblHint.ForeColor = MutedColor;
            lblHint.Location = new Point(0, 442);
            lblHint.Size = new Size(560, 20);
            host.Controls.Add(lblHint);

            return host;
        }

        private void LoadConfigSection()
        {
            txtHotkey.Text = HotkeyMods.Format(overlay.HotkeyMod, overlay.HotkeyVk);
            int pct = (int)System.Math.Round(overlay.Opacity * 100.0);
            if (pct > trkOpacity.Maximum) pct = trkOpacity.Maximum;
            if (pct < trkOpacity.Minimum) pct = trkOpacity.Minimum;
            trkOpacity.Value = pct;
            lblOpacityValue.Text = trkOpacity.Value + "%";

            loadingLanguageCombo = true;
            cmbLanguage.SelectedIndex = config.Language == "pt-BR" ? 1 : 0;
            loadingLanguageCombo = false;
        }

        private void CmbLanguage_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                Rectangle flagRect = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 4, 20, 14);
                if (e.Index == 0) FlagIcons.DrawUs(e.Graphics, flagRect);
                else FlagIcons.DrawBr(e.Graphics, flagRect);

                Color textColor = (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : Color.Black;
                using (Brush b = new SolidBrush(textColor))
                    e.Graphics.DrawString(cmbLanguage.Items[e.Index].ToString(), e.Font, b, e.Bounds.Left + 32, e.Bounds.Top + 3);
            }
            e.DrawFocusRectangle();
        }

        private void CmbLanguage_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (loadingLanguageCombo) return;
            string newLang = cmbLanguage.SelectedIndex == 1 ? "pt-BR" : "en";
            if (newLang == config.Language) return;

            config.Language = newLang;
            config.Save();
            restartAction();
        }

        private void TxtHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                return;

            uint mod = 0;
            if (e.Control) mod |= HotkeyMods.MOD_CONTROL;
            if (e.Alt) mod |= HotkeyMods.MOD_ALT;
            if (e.Shift) mod |= HotkeyMods.MOD_SHIFT;

            if (mod == 0)
            {
                SetStatus(Loc.T("settings.hotkey.needModifier"), false);
                return;
            }

            uint vk = (uint)e.KeyCode;
            overlay.SetHotkey(mod, vk);
            txtHotkey.Text = HotkeyMods.Format(mod, vk);
            SetStatus(Loc.F("settings.hotkey.updated", txtHotkey.Text), true);
        }
    }
}
