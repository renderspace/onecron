using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace One.Cron.Core
{
    public static class Tool
    {
        public static List<string> SplitString(string toSplit)
        {
            List<string> splitted = new List<string>();

            if (toSplit != null && toSplit.Length > 0)
            {
                string[] iSplitted = toSplit.Split(new char[] { ' ', ';', ',' });
                foreach (string s in iSplitted)
                {
                    if (s.Trim().Length > 0)
                        splitted.Add(s.Trim());
                }
            }
            return splitted;
        }
    }
}
