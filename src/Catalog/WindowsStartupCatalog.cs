using System;

namespace PcToolkit
{
    public static class WindowsStartupCatalog
    {
        private static readonly string[] VendorKeys = new[]
        {
            "OneDrive",
            "SecurityHealth",
            "Windows Defender",
            "RtkAudUService",
            "IAStorIcon",
            "NvBackend",
            "NVIDIA",
            "Steam",
            "Discord",
            "Skype",
            "Dropbox",
            "CCleaner",
            "Adobe",
            "Spotify",
        };

        public static string Lookup(string valueName)
        {
            foreach (string vendorKey in VendorKeys)
                if (valueName.IndexOf(vendorKey, StringComparison.OrdinalIgnoreCase) >= 0)
                    return Loc.T("startupcatalog." + vendorKey);
            return null;
        }
    }
}
