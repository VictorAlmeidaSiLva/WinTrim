using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

namespace PcToolkit
{
    public static class VramTools
    {
        private const string GraphicsDriversPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        private const string GpuPrefPath = @"Software\Microsoft\DirectX\UserGpuPreferences";

        private static readonly HashSet<string> ProtectedProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "csrss", "wininit", "winlogon", "smss", "services", "lsass", "dwm",
        };

        public static bool IsProtectedProcess(string processName)
        {
            return processName != null && ProtectedProcessNames.Contains(processName);
        }

        public static List<VramProcRow> GetTopVramProcesses(int top)
        {
            Dictionary<int, double> mbByPid = new Dictionary<int, double>();
            try
            {
                PerformanceCounterCategory cat = new PerformanceCounterCategory("GPU Process Memory");
                string[] instances = cat.GetInstanceNames();
                foreach (string inst in instances)
                {
                    int pid = ParsePid(inst);
                    if (pid <= 0) continue;
                    try
                    {
                        using (PerformanceCounter c = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", inst, true))
                        {
                            double mb = c.NextValue() / 1024.0 / 1024.0;
                            if (mbByPid.ContainsKey(pid)) mbByPid[pid] += mb; else mbByPid[pid] = mb;
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                return new List<VramProcRow>();
            }

            List<VramProcRow> list = new List<VramProcRow>();
            foreach (KeyValuePair<int, double> kv in mbByPid)
            {
                if (kv.Value < 1.0) continue;

                VramProcRow row = new VramProcRow();
                row.Pid = kv.Key;
                row.DedicatedMb = kv.Value;
                try
                {
                    using (Process p = Process.GetProcessById(kv.Key))
                    {
                        row.ProcessName = p.ProcessName;
                        try { row.ExePath = p.MainModule.FileName; }
                        catch { row.ExePath = null; }
                    }
                }
                catch
                {
                    row.ProcessName = "(pid " + kv.Key + ")";
                    row.ExePath = null;
                }

                row.GpuPreference = row.ExePath != null ? GetGpuPreference(row.ExePath) : 0;
                list.Add(row);
            }

            list.Sort(delegate (VramProcRow a, VramProcRow b) { return b.DedicatedMb.CompareTo(a.DedicatedMb); });
            return list.Count > top ? list.GetRange(0, top) : list;
        }

        private static int ParsePid(string instanceName)
        {
            
            string[] parts = instanceName.Split('_');
            int pid;
            if (parts.Length >= 2 && parts[0] == "pid" && int.TryParse(parts[1], out pid)) return pid;
            return -1;
        }

        public static void KillProcess(int pid)
        {
            using (Process p = Process.GetProcessById(pid))
            {
                p.Kill();
            }
        }

        public static bool GetHagsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(GraphicsDriversPath))
                {
                    if (key == null) return false;
                    object val = key.GetValue("HwSchMode");
                    return val != null && Convert.ToInt32(val) == 2;
                }
            }
            catch { return false; }
        }

        public static void SetHagsEnabled(bool enable)
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(GraphicsDriversPath))
            {
                key.SetValue("HwSchMode", enable ? 2 : 1, RegistryValueKind.DWord);
            }
        }

        public static int GetGpuPreference(string exePath)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(GpuPrefPath))
                {
                    if (key == null) return 0;
                    string val = key.GetValue(exePath) as string;
                    if (string.IsNullOrEmpty(val)) return 0;

                    int eq = val.IndexOf('=');
                    int semi = val.IndexOf(';');
                    if (eq >= 0 && semi > eq)
                    {
                        int n;
                        if (int.TryParse(val.Substring(eq + 1, semi - eq - 1), out n)) return n;
                    }
                    return 0;
                }
            }
            catch { return 0; }
        }

        public static void SetGpuPreference(string exePath, int pref)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(GpuPrefPath))
            {
                key.SetValue(exePath, "GpuPreference=" + pref + ";", RegistryValueKind.String);
            }
        }
    }
}
