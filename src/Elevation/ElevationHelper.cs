using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace PcToolkit
{
    public static class ElevationHelper
    {
        public static bool RunElevated(params string[] actionArgs)
        {
            string[] quoted = new string[actionArgs.Length];
            for (int i = 0; i < actionArgs.Length; i++) quoted[i] = QuoteArg(actionArgs[i]);
            string args = "--action " + string.Join(" ", quoted);

            ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath, args);
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        private static string QuoteArg(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            int backslashCount = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\')
                {
                    backslashCount++;
                    continue;
                }
                if (s[i] == '"')
                {
                    sb.Append('\\', backslashCount * 2 + 1);
                    sb.Append('"');
                }
                else
                {
                    sb.Append('\\', backslashCount);
                    sb.Append(s[i]);
                }
                backslashCount = 0;
            }
            sb.Append('\\', backslashCount * 2);
            sb.Append('"');
            return sb.ToString();
        }
    }
}
