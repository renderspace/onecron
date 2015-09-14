using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using ServiceDebuggerHelper;

namespace One.Cron
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            #if DEBUG
                System.Windows.Forms.Application.Run(new ServiceRunner(new ServiceCron()));
            #else
                var ServicesToRun = new ServiceBase[] { new ServiceCron() };
                ServiceBase.Run(ServicesToRun);
            #endif
        }
    }
}
