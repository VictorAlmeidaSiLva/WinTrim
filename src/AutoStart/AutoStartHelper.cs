using System;
using System.IO;
using System.Windows.Forms;

namespace PcToolkit
{
    public static class AutoStartHelper
    {
        private static string ShortcutPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "WinTrim.lnk"); }
        }

        public static bool IsEnabled()
        {
            return File.Exists(ShortcutPath);
        }

        public static void SetEnabled(bool enable)
        {
            if (enable)
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(ShortcutPath);
                shortcut.TargetPath = Application.ExecutablePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                shortcut.Description = Loc.T("autostart.shortcutDesc");
                shortcut.Save();
            }
            else
            {
                try { if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath); } catch { }
            }
        }
    }
}
