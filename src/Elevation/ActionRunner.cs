namespace PcToolkit
{
    public static class ActionRunner
    {
        public static int Run(string[] args)
        {
            try
            {
                string action = args[1];
                if (action == "svc-mode") { ServiceHelper.SetStartMode(args[2], args[3]); return 0; }
                if (action == "svc-stop") { ServiceHelper.Stop(args[2]); return 0; }
                if (action == "reg-toggle") { RegistryHelper.SetApproved(args[2], args[3], args[4], args[5] == "enable"); return 0; }
                if (action == "task-toggle") { TaskHelper.SetEnabled(args[2], args[3] == "enable"); return 0; }
                if (action == "ram-purge") { RamTools.PurgeAll(); return 0; }
                if (action == "self-autostart") { AutoStartHelper.SetEnabled(args[2] == "enable"); return 0; }
                if (action == "hags-toggle") { VramTools.SetHagsEnabled(args[2] == "enable"); return 0; }
                if (action == "kill-process") { VramTools.KillProcess(int.Parse(args[2])); return 0; }
                return 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}
