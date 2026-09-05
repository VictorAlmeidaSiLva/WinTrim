using System.Text;
using System.Windows.Forms;

namespace PcToolkit
{
    public static class HotkeyMods
    {
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public static string Format(uint mod, uint vk)
        {
            StringBuilder sb = new StringBuilder();
            if ((mod & MOD_CONTROL) != 0) sb.Append("Ctrl+");
            if ((mod & MOD_ALT) != 0) sb.Append("Alt+");
            if ((mod & MOD_SHIFT) != 0) sb.Append("Shift+");
            if ((mod & MOD_WIN) != 0) sb.Append("Win+");
            sb.Append(((Keys)vk).ToString());
            return sb.ToString();
        }
    }
}
