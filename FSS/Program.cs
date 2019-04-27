using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string arg = "";

            if (args.Length > 0)
            {
                // We expect one param only, two characters long: "/s"
                arg = args[0].ToLower().Trim().Substring(0, 2);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ScreenSaver(arg));
        }
    }
}
