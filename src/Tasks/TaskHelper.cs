using System;
using System.Collections.Generic;

namespace PcToolkit
{
    public static class TaskHelper
    {
        public static List<TaskRow> GetLogonBootTasks()
        {
            List<TaskRow> list = new List<TaskRow>();
            try
            {
                Type schedType = Type.GetTypeFromProgID("Schedule.Service");
                dynamic svc = Activator.CreateInstance(schedType);
                svc.Connect();
                dynamic root = svc.GetFolder("\\");
                CollectFolder(root, list, 0);
            }
            catch { }
            return list;
        }

        private static void CollectFolder(dynamic folder, List<TaskRow> list, int depth)
        {
            try
            {
                dynamic tasks = folder.GetTasks(1);
                int taskCount = tasks.Count;
                for (int ti = 1; ti <= taskCount; ti++)
                {
                    try
                    {
                        dynamic t = tasks.Item(ti);
                        dynamic def = t.Definition;
                        dynamic triggers = def.Triggers;
                        bool isLogonOrBoot = false;
                        List<string> typeNames = new List<string>();
                        int count = triggers.Count;
                        for (int i = 1; i <= count; i++)
                        {
                            int type = (int)triggers.Item(i).Type;
                            if (type == 8) { isLogonOrBoot = true; typeNames.Add("Boot"); }
                            else if (type == 9) { isLogonOrBoot = true; typeNames.Add("Logon"); }
                        }
                        if (!isLogonOrBoot) continue;

                        TaskRow row = new TaskRow();
                        row.Path = (string)t.Path;
                        row.Name = (string)t.Name;
                        row.Enabled = (bool)t.Enabled;
                        row.Triggers = string.Join(",", typeNames.ToArray());
                        list.Add(row);
                    }
                    catch { }
                }
            }
            catch { }

            if (depth < 1)
            {
                try
                {
                    dynamic folders = folder.GetFolders(0);
                    int fCount = folders.Count;
                    for (int fi = 1; fi <= fCount; fi++)
                    {
                        dynamic f = folders.Item(fi);
                        string fname = (string)f.Name;
                        if (fname == "Microsoft") continue;
                        CollectFolder(f, list, depth + 1);
                    }
                }
                catch { }
            }
        }

        public static void SetEnabled(string taskPath, bool enabled)
        {
            Type schedType = Type.GetTypeFromProgID("Schedule.Service");
            dynamic svc = Activator.CreateInstance(schedType);
            svc.Connect();

            int slash = taskPath.LastIndexOf('\\');
            string folderPath = slash > 0 ? taskPath.Substring(0, slash) : "\\";
            string taskName = slash >= 0 ? taskPath.Substring(slash + 1) : taskPath;
            if (folderPath.Length == 0) folderPath = "\\";

            dynamic folder = svc.GetFolder(folderPath);
            dynamic task = folder.GetTask(taskName);
            task.Enabled = enabled;
        }
    }
}
