using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace One.Cron.API
{
    public interface IJob
    {
        string Description { get; set; }
        void Execute();
        bool IsRunning { get; set; }
    }
}
