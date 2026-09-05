using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace PcToolkit
{
    public static class RegistryHelper
    {
        private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string Run32Path = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedBase = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

        public static List<ProgramRow> GetAll()
        {
            List<ProgramRow> list = new List<ProgramRow>();

            AddRunEntries(list, Registry.CurrentUser, RunPath, Loc.T("startup.scope.user"), "HKCU", "Run");
            AddRunEntries(list, Registry.LocalMachine, RunPath, Loc.T("startup.scope.machine"), "HKLM", "Run");
            AddRunEntries(list, Registry.LocalMachine, Run32Path, Loc.T("startup.scope.machine32"), "HKLM", "Run32");

            AddStartupFolder(list, Environment.GetFolderPath(Environment.SpecialFolder.Startup), Loc.T("startup.scope.userFolder"), "HKCU");
            AddStartupFolder(list, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), Loc.T("startup.scope.machineFolder"), "HKLM");

            list.Sort(delegate (ProgramRow a, ProgramRow b) { return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); });
            return list;
        }

        private static void AddRunEntries(List<ProgramRow> list, RegistryKey root, string path, string scope, string hive, string approvedSubkey)
        {
            RegistryKey key = null;
            try
            {
                key = root.OpenSubKey(path, false);
                if (key == null) return;
                foreach (string name in key.GetValueNames())
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    ProgramRow row = new ProgramRow();
                    row.Name = name;
                    object val = key.GetValue(name);
                    row.Command = val != null ? val.ToString() : "";
                    row.Scope = scope;
                    row.ApprovedHive = hive;
                    row.ApprovedSubkey = approvedSubkey;
                    row.Enabled = IsApproved(hive, approvedSubkey, name);
                    list.Add(row);
                }
            }
            catch { }
            finally { if (key != null) key.Close(); }
        }

        private static void AddStartupFolder(List<ProgramRow> list, string folderPath, string scope, string hive)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;
                foreach (string file in Directory.GetFiles(folderPath))
                {
                    string name = Path.GetFileName(file);
                    if (string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    ProgramRow row = new ProgramRow();
                    row.Name = name;
                    row.Command = file;
                    row.Scope = scope;
                    row.ApprovedHive = hive;
                    row.ApprovedSubkey = "StartupFolder";
                    row.Enabled = IsApproved(hive, "StartupFolder", name);
                    list.Add(row);
                }
            }
            catch { }
        }

        private static RegistryKey GetHive(string hive)
        {
            return hive == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
        }

        private static bool IsApproved(string hive, string approvedSubkey, string name)
        {
            RegistryKey key = null;
            try
            {
                key = GetHive(hive).OpenSubKey(ApprovedBase + approvedSubkey, false);
                if (key == null) return true;
                byte[] bytes = key.GetValue(name) as byte[];
                if (bytes == null || bytes.Length == 0) return true;
                return bytes[0] != 0x03;
            }
            catch { return true; }
            finally { if (key != null) key.Close(); }
        }

        public static void SetApproved(string hive, string approvedSubkey, string name, bool enable)
        {
            RegistryKey key = GetHive(hive).CreateSubKey(ApprovedBase + approvedSubkey);
            try
            {
                if (enable)
                {
                    try { key.DeleteValue(name, false); } catch { }
                }
                else
                {
                    byte[] bytes = new byte[12];
                    bytes[0] = 0x03;
                    key.SetValue(name, bytes, RegistryValueKind.Binary);
                }
            }
            finally { key.Close(); }
        }
    }
}
