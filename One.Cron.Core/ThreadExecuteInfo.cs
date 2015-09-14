using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace One.Cron.Core
{
    public class ThreadExecuteInfo
    {
        public string ClassName { get; set; }
        public string Settings { get; set; }

        public string Description { get; set; }
        public int JobId { get; set; }
    }
}
