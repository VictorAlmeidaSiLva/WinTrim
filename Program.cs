using System;
using System.Windows.Forms;

namespace PcToolkit
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--action")
            {
                int code = ActionRunner.Run(args);
                Environment.Exit(code);
                return;
            }

            AppConfig config = AppConfig.Load();
            Loc.Current = config.Language == "pt-BR" ? Lang.PtBr : Lang.En;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayContext(config));
        }
    }
}
