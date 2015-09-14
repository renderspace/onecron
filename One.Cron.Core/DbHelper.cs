using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace One.Cron.Core
{
    internal class DbHelper
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Cron"].ConnectionString; }
        }
    }
}
