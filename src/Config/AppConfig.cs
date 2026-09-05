using System;
using System.IO;
using System.Text;

namespace PcToolkit
{
    public class AppConfig
    {
        public int OverlayX = -1;
        public int OverlayY = 12;
        public double OverlayOpacity = 0.80;
        public bool ShowAllServices = false;
        public uint HotkeyMod = HotkeyMods.MOD_CONTROL | HotkeyMods.MOD_SHIFT;
        public uint HotkeyVk = 0x56; 
        public int WindowW = 1040;
        public int WindowH = 680;
        public string Language = "en";

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinTrim");
                try { Directory.CreateDirectory(dir); } catch { }
                return Path.Combine(dir, "config.json");
            }
        }

        public static AppConfig Load()
        {
            AppConfig cfg = new AppConfig();
            try
            {
                if (!File.Exists(FilePath)) return cfg;
                foreach (string rawLine in File.ReadAllLines(FilePath))
                {
                    string line = rawLine.Trim().TrimEnd(',');
                    int colon = line.IndexOf(':');
                    if (colon < 0) continue;
                    string key = line.Substring(0, colon).Trim().Trim('"');
                    string val = line.Substring(colon + 1).Trim().Trim('"');
                    switch (key)
                    {
                        case "OverlayX": cfg.OverlayX = ParseInt(val, cfg.OverlayX); break;
                        case "OverlayY": cfg.OverlayY = ParseInt(val, cfg.OverlayY); break;
                        case "OverlayOpacity": cfg.OverlayOpacity = ParseDouble(val, cfg.OverlayOpacity); break;
                        case "ShowAllServices": cfg.ShowAllServices = ParseBool(val, cfg.ShowAllServices); break;
                        case "HotkeyMod": cfg.HotkeyMod = (uint)ParseInt(val, (int)cfg.HotkeyMod); break;
                        case "HotkeyVk": cfg.HotkeyVk = (uint)ParseInt(val, (int)cfg.HotkeyVk); break;
                        case "WindowW": cfg.WindowW = ParseInt(val, cfg.WindowW); break;
                        case "WindowH": cfg.WindowH = ParseInt(val, cfg.WindowH); break;
                        case "Language": cfg.Language = val; break;
                    }
                }
            }
            catch { }
            return cfg;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{\n");
                sb.Append("  \"OverlayX\": " + OverlayX + ",\n");
                sb.Append("  \"OverlayY\": " + OverlayY + ",\n");
                sb.Append("  \"OverlayOpacity\": " + OverlayOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\n");
                sb.Append("  \"ShowAllServices\": " + (ShowAllServices ? "true" : "false") + ",\n");
                sb.Append("  \"HotkeyMod\": " + HotkeyMod + ",\n");
                sb.Append("  \"HotkeyVk\": " + HotkeyVk + ",\n");
                sb.Append("  \"WindowW\": " + WindowW + ",\n");
                sb.Append("  \"WindowH\": " + WindowH + ",\n");
                sb.Append("  \"Language\": \"" + Language + "\"\n");
                sb.Append("}\n");
                File.WriteAllText(FilePath, sb.ToString());
            }
            catch { }
        }

        private static int ParseInt(string s, int def) { int v; return int.TryParse(s, out v) ? v : def; }
        private static double ParseDouble(string s, double def) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : def; }
        private static bool ParseBool(string s, bool def) { if (s == "true") return true; if (s == "false") return false; return def; }
    }
}
