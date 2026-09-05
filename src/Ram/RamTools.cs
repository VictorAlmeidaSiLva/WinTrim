using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PcToolkit
{
    public static class RamTools
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID Luid; public uint Attributes; }

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x0002;
        private const int SystemMemoryListInformation = 80;
        private const int MemoryEmptyWorkingSets = 2;
        private const int MemoryFlushModifiedList = 3;
        private const int MemoryPurgeStandbyList = 4;

        private static PerformanceCounter pcAvail, pcFree, pcSbCore, pcSbNorm, pcSbRes, pcMod, pcComm, pcPoolPg, pcPoolNp;
        private static bool countersReady;
        private static readonly object initLock = new object();

        private static bool EnablePrivilege(string name)
        {
            IntPtr hToken;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken)) return false;
            try
            {
                LUID luid;
                if (!LookupPrivilegeValue(null, name, out luid)) return false;
                TOKEN_PRIVILEGES tp;
                tp.PrivilegeCount = 1;
                tp.Luid = luid;
                tp.Attributes = SE_PRIVILEGE_ENABLED;
                return AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            finally { CloseHandle(hToken); }
        }

        private static int RunCommand(int cmd)
        {
            IntPtr p = Marshal.AllocHGlobal(4);
            try
            {
                Marshal.WriteInt32(p, cmd);
                return NtSetSystemInformation(SystemMemoryListInformation, p, 4);
            }
            finally { Marshal.FreeHGlobal(p); }
        }

        public static string PurgeAll()
        {
            EnablePrivilege("SeProfileSingleProcessPrivilege");
            EnablePrivilege("SeIncreaseQuotaPrivilege");

            int rWorkingSets = RunCommand(MemoryEmptyWorkingSets);
            int rFlush = RunCommand(MemoryFlushModifiedList);
            int rPurge = RunCommand(MemoryPurgeStandbyList);

            bool ok = (rWorkingSets == 0) && (rFlush == 0) && (rPurge == 0);
            if (!ok)
            {

throw new UnauthorizedAccessException(string.Format(
                    "PurgeAll falhou: WS=0x{0:X8} Flush=0x{1:X8} Standby=0x{2:X8}", rWorkingSets, rFlush, rPurge));
            }
            return Loc.T("ramclean.purgeSuccess");
        }

        private static void EnsureCounters()
        {
            if (countersReady) return;
            lock (initLock)
            {
                if (countersReady) return;
                try
                {
                    pcAvail = new PerformanceCounter("Memory", "Available MBytes");
                    pcFree = new PerformanceCounter("Memory", "Free & Zero Page List Bytes");
                    pcSbCore = new PerformanceCounter("Memory", "Standby Cache Core Bytes");
                    pcSbNorm = new PerformanceCounter("Memory", "Standby Cache Normal Priority Bytes");
                    pcSbRes = new PerformanceCounter("Memory", "Standby Cache Reserve Bytes");
                    pcMod = new PerformanceCounter("Memory", "Modified Page List Bytes");
                    pcComm = new PerformanceCounter("Memory", "Committed Bytes");
                    pcPoolPg = new PerformanceCounter("Memory", "Pool Paged Bytes");
                    pcPoolNp = new PerformanceCounter("Memory", "Pool Nonpaged Bytes");
                    countersReady = true;
                }
                catch { countersReady = false; }
            }
        }

        public static void Cleanup()
        {
            TryDispose(pcAvail); TryDispose(pcFree); TryDispose(pcSbCore); TryDispose(pcSbNorm);
            TryDispose(pcSbRes); TryDispose(pcMod); TryDispose(pcComm); TryDispose(pcPoolPg); TryDispose(pcPoolNp);
        }

        private static void TryDispose(PerformanceCounter pc)
        {
            if (pc == null) return;
            try { pc.Dispose(); } catch { }
        }

        public static MemReport GetMemReport()
        {
            MemReport r = new MemReport();

            MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref mem);
            r.TotalGB = mem.ullTotalPhys / 1024.0 / 1024.0 / 1024.0;

            try
            {
                EnsureCounters();
                if (!countersReady) throw new InvalidOperationException("contadores indisponiveis");

                double availMB = pcAvail.NextValue();
                double freeB = pcFree.NextValue();
                double sbCore = pcSbCore.NextValue();
                double sbNorm = pcSbNorm.NextValue();
                double sbRes = pcSbRes.NextValue();
                double modB = pcMod.NextValue();
                double commB = pcComm.NextValue();
                double poolPg = pcPoolPg.NextValue();
                double poolNp = pcPoolNp.NextValue();

                r.AvailableGB = availMB / 1024.0;
                r.FreeGB = freeB / 1024.0 / 1024.0 / 1024.0;
                r.StandbyGB = (sbCore + sbNorm + sbRes) / 1024.0 / 1024.0 / 1024.0;
                r.ModifiedGB = modB / 1024.0 / 1024.0 / 1024.0;
                r.CommittedGB = commB / 1024.0 / 1024.0 / 1024.0;
                r.PoolPagedGB = poolPg / 1024.0 / 1024.0 / 1024.0;
                r.PoolNonPagedGB = poolNp / 1024.0 / 1024.0 / 1024.0;
                r.DetalheOk = true;
            }
            catch
            {
                r.AvailableGB = mem.ullAvailPhys / 1024.0 / 1024.0 / 1024.0;
                r.DetalheOk = false;
            }

            return r;
        }

        public static List<ProcRam> GetTopPrivateWorkingSet(int top, out double totalGB)
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();
            double totalMB = 0;

            Process[] processes = Process.GetProcesses();
            try
            {
                foreach (Process p in processes)
                {
                    try
                    {
                        if (p.ProcessName == "Idle") continue;
                        double mb = p.PrivateMemorySize64 / 1024.0 / 1024.0;
                        string name = p.ProcessName;
                        if (dict.ContainsKey(name)) dict[name] += mb; else dict[name] = mb;
                        totalMB += mb;
                    }
                    catch { }
                }
            }
            finally
            {
                foreach (Process p in processes) p.Dispose();
            }

            totalGB = totalMB / 1024.0;

            List<ProcRam> list = new List<ProcRam>();
            foreach (KeyValuePair<string, double> kv in dict)
            {
                ProcRam pr = new ProcRam();
                pr.Name = kv.Key;
                pr.MB = kv.Value;
                list.Add(pr);
            }
            list.Sort(delegate (ProcRam a, ProcRam b) { return b.MB.CompareTo(a.MB); });
            return list.Count > top ? list.GetRange(0, top) : list;
        }
    }
}
