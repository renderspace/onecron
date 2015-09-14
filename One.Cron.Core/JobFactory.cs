using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using One.Cron.API;

namespace One.Cron.Core
{
    public class JobFactory
    {
        public static IJob CreateJob(string fullyQualifiedClassName)
        {
            string path = fullyQualifiedClassName.Substring(0, fullyQualifiedClassName.LastIndexOf('.'));
            string className = fullyQualifiedClassName;
            Assembly a = null;
            a = Assembly.Load(path);
            // Using the evidence given in the config file load the appropriate assembly and class
            return (IJob)a.CreateInstance(className);
        }
    }
}
