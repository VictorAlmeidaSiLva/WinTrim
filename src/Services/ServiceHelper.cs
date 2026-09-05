using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace PcToolkit
{
    public static class ServiceHelper
    {
        public static List<ServiceRow> GetServices(bool includeAll)
        {
            List<ServiceRow> list = new List<ServiceRow>();
            string query = includeAll
                ? "SELECT Name,DisplayName,State,StartMode,ProcessId FROM Win32_Service"
                : "SELECT Name,DisplayName,State,StartMode,ProcessId FROM Win32_Service WHERE StartMode='Auto'";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject mo in searcher.Get())
                {
                    ServiceRow row = new ServiceRow();
                    row.Name = Convert.ToString(mo["Name"]);
                    row.DisplayName = Convert.ToString(mo["DisplayName"]);
                    row.State = Convert.ToString(mo["State"]);
                    row.StartMode = Convert.ToString(mo["StartMode"]);
                    object pidObj = mo["ProcessId"];
                    row.ProcessId = pidObj != null ? Convert.ToInt32(pidObj) : 0;
                    row.RamMb = -1;
                    list.Add(row);
                }
            }

            Dictionary<int, double> ramByPid = new Dictionary<int, double>();
            Process[] procs = Process.GetProcesses();
            try
            {
                foreach (Process p in procs)
                {
                    try { ramByPid[p.Id] = p.PrivateMemorySize64 / 1024.0 / 1024.0; }
                    catch { }
                }
            }
            finally
            {
                foreach (Process p in procs) p.Dispose();
            }

            foreach (ServiceRow row in list)
            {
                double mb;
                if (row.ProcessId != 0 && ramByPid.TryGetValue(row.ProcessId, out mb)) row.RamMb = mb;
            }

            list.Sort(delegate (ServiceRow a, ServiceRow b) { return b.RamMb.CompareTo(a.RamMb); });
            return list;
        }

        public static void SetStartMode(string name, string mode)
        {
            using (ManagementObject mo = new ManagementObject("Win32_Service.Name='" + name.Replace("'", "\\'") + "'"))
            {
                object result = mo.InvokeMethod("ChangeStartMode", new object[] { mode });
                int code = Convert.ToInt32(result);
                if (code != 0) throw new UnauthorizedAccessException("ChangeStartMode retornou " + code);
            }
        }

        public static void Stop(string name)
        {
            using (ManagementObject mo = new ManagementObject("Win32_Service.Name='" + name.Replace("'", "\\'") + "'"))
            {
                object result = mo.InvokeMethod("StopService", null);
                int code = Convert.ToInt32(result);
                if (code != 0) throw new UnauthorizedAccessException("StopService retornou " + code);
            }
        }
    }
}
