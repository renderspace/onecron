using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using log4net;
using One.Cron.API;

namespace One.Cron.Core
{
    public class TestJob : IJob
    {
        private volatile bool isRunning;


        private static readonly ILog log = LogManager.GetLogger(typeof(TestJob));

        public bool IsRunning { get { return isRunning; } set { isRunning = value; } }

        public string Description { get; set; }

        public void Execute()
        {
            log.Info("Execute for 10 seconds: " + Description);
            Thread.Sleep(10000);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Description))
                return "TestJob - " + Description;
            return base.ToString();
        }
    }
}
