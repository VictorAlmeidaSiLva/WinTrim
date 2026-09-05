using System.Drawing;
using System.Windows.Forms;

namespace PcToolkit
{
    public class AccentCard : Panel
    {
        public AccentCard(Color accent)
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Top;
            bar.Height = 3;
            bar.BackColor = accent;
            Controls.Add(bar);
        }
    }
}
