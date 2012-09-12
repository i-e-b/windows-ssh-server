using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsSshServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check if application is running as service.
            if (Environment.UserName == "SYSTEM" && !Environment.UserInteractive)
            {
                // Run OS service.
                using (var service = new ServerService())
                {
                    System.ServiceProcess.ServiceBase.Run(service);
                }
            }
            else
            {
                // Run user interface.
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());

                // Save user settings.
                Properties.Settings.Default.Save();
            }
        }
    }
}
