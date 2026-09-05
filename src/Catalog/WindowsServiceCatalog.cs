using System;
using System.Collections.Generic;

namespace PcToolkit
{
    public static class WindowsServiceCatalog
    {
        private static readonly Dictionary<string, ServiceCatalogEntry> Map = new Dictionary<string, ServiceCatalogEntry>(StringComparer.OrdinalIgnoreCase)
        {
            { "BFE", new ServiceCatalogEntry("svccatalog.BFE", true) },
            { "MpsSvc", new ServiceCatalogEntry("svccatalog.MpsSvc", true) },
            { "WinDefend", new ServiceCatalogEntry("svccatalog.WinDefend", true) },
            { "wscsvc", new ServiceCatalogEntry("svccatalog.wscsvc", false) },
            { "EventLog", new ServiceCatalogEntry("svccatalog.EventLog", true) },
            { "PlugPlay", new ServiceCatalogEntry("svccatalog.PlugPlay", true) },
            { "RpcSs", new ServiceCatalogEntry("svccatalog.RpcSs", true) },
            { "RpcEptMapper", new ServiceCatalogEntry("svccatalog.RpcEptMapper", true) },
            { "DcomLaunch", new ServiceCatalogEntry("svccatalog.DcomLaunch", true) },
            { "Power", new ServiceCatalogEntry("svccatalog.Power", true) },
            { "Schedule", new ServiceCatalogEntry("svccatalog.Schedule", true) },
            { "SamSs", new ServiceCatalogEntry("svccatalog.SamSs", true) },
            { "LSM", new ServiceCatalogEntry("svccatalog.LSM", true) },
            { "CryptSvc", new ServiceCatalogEntry("svccatalog.CryptSvc", true) },
            { "Winmgmt", new ServiceCatalogEntry("svccatalog.Winmgmt", true) },
            { "gpsvc", new ServiceCatalogEntry("svccatalog.gpsvc", true) },
            { "Dhcp", new ServiceCatalogEntry("svccatalog.Dhcp", true) },
            { "Dnscache", new ServiceCatalogEntry("svccatalog.Dnscache", true) },
            { "NlaSvc", new ServiceCatalogEntry("svccatalog.NlaSvc", true) },
            { "Themes", new ServiceCatalogEntry("svccatalog.Themes", false) },
            { "AudioSrv", new ServiceCatalogEntry("svccatalog.AudioSrv", true) },
            { "AudioEndpointBuilder", new ServiceCatalogEntry("svccatalog.AudioEndpointBuilder", true) },
            { "Spooler", new ServiceCatalogEntry("svccatalog.Spooler", false) },
            { "WSearch", new ServiceCatalogEntry("svccatalog.WSearch", false) },
            { "SysMain", new ServiceCatalogEntry("svccatalog.SysMain", false) },
            { "wuauserv", new ServiceCatalogEntry("svccatalog.wuauserv", false) },
            { "LanmanServer", new ServiceCatalogEntry("svccatalog.LanmanServer", false) },
            { "LanmanWorkstation", new ServiceCatalogEntry("svccatalog.LanmanWorkstation", true) },
            { "TrustedInstaller", new ServiceCatalogEntry("svccatalog.TrustedInstaller", true) },
            { "WlanSvc", new ServiceCatalogEntry("svccatalog.WlanSvc", true) },
            { "BITS", new ServiceCatalogEntry("svccatalog.BITS", false) },
            { "Netman", new ServiceCatalogEntry("svccatalog.Netman", true) },
        };

        public static ServiceCatalogEntry Lookup(string serviceName)
        {
            ServiceCatalogEntry e;
            return Map.TryGetValue(serviceName, out e) ? e : null;
        }
    }
}
