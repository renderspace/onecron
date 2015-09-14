using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;



namespace One.Cron.Core
{
    public class BOCron
    {

        int lastPendingCheckMinute;

        [PrimaryKey, AutoIncrement]
        public int JobId { get; set; }

        [MaxLength(255)]
        public string CrontabEntryRaw { get; set; }

        public string CrontabEntry
        {
            get { return CrontabEntryRaw; } 
            set
            {
                CrontabEntryRaw = value;
                ReadCrontab(CrontabEntryRaw);
            }
        }

        [MaxLength(1000)]
        public string AdminEmailList { get; set; }

        public string SerializedSettings { get; set; }


        [Indexed]
        public DateTime? LastFinished { get; set; }

        [Indexed]
        public string ClassName { get; set; }

        public string Description
        {
            get { return "[" + JobId + " - " + ClassName + "] " + CrontabEntry; }
        }

        public ArrayList Months { get; set; }
        public ArrayList MDays { get; set; }
        public ArrayList WDays { get; set; }
        public ArrayList Hours { get; set; }
        public ArrayList Minutes { get; set; }

        public BOCron()
        {
            lastPendingCheckMinute = DateTime.Now.Minute - 1;
        }

        /// <summary>
        /// Pending check changes the state of the object (will not return true twice in a minute)
        /// </summary>
        public bool IsPending
        {
            get
            {
                DateTime now = DateTime.Now;
                if (now.Minute.Equals(lastPendingCheckMinute))
                    return false;

                
                // for loop: deal with the highly unexpected eventuality of
                // having lost more than one minute to unavailable processor time
                for (int minute = (lastPendingCheckMinute == 59 ? 0 : lastPendingCheckMinute + 1); minute <= now.Minute; minute++)
                {

                    if (Contains(Months, now.Month) &&
                        Contains(MDays, GetMDay(now)) &&
                        Contains(WDays, GetWDay(now)) &&
                        Contains(Hours, now.Hour) &&
                        Contains(Minutes, now.Minute))
                    {
                        lastPendingCheckMinute = now.Minute;
                        return true;
                    }
                }
                lastPendingCheckMinute = now.Minute;
                return false;
            }
        }

        // sort of a macro to keep the if-statement above readable
        private static bool Contains(IList list, int val)
        {
            // -1 represents the star * from the crontab
            return list.Contains(val) || list.Contains(-1);
        }

        private static int GetMDay(DateTime date)
        {
            date.AddMonths(-(date.Month - 1));
            return date.DayOfYear;
        }

        private static int GetWDay(DateTime date)
        {
            if (date.DayOfWeek.Equals(DayOfWeek.Sunday))
                return 7;
            else
                return (int)date.DayOfWeek;
        }

        private void ReadCrontab(string line)
        {
            line = line.Trim();

            if (line.Length == 0 || line.StartsWith("#"))
                return;

            // re-escape space- and backslash-escapes in a cheap fashion
            line = line.Replace("\\\\", "<BACKSLASH>");
            line = line.Replace("\\ ", "<SPACE>");

            // split string on whitespace
            String[] cols = line.Split(new[] { ' ', '\t' });

            for (int i = 0; i < cols.Length; i++)
            {
                cols[i] = cols[i].Replace("<BACKSLASH>", "\\");
                cols[i] = cols[i].Replace("<SPACE>", " ");
            }

            if (cols.Length < 5)
            {
                throw new ArgumentException("Parse error in crontab (line too short).");
            }

            Minutes = ParseTimes(cols[0], 0, 59);
            Hours = ParseTimes(cols[1], 0, 23);
            Months = ParseTimes(cols[3], 1, 12);

            if (!cols[2].Equals("*") && cols[3].Equals("*"))
            {
                // every n monthdays, disregarding weekdays
                MDays = ParseTimes(cols[2], 1, 31);
                WDays = new ArrayList();
                WDays.Add(-1); // empty value
            }
            else if (cols[2].Equals("*") && !cols[3].Equals("*"))
            {
                // every n weekdays, disregarding monthdays
                MDays = new ArrayList();
                MDays.Add(-1); // empty value
                WDays = ParseTimes(cols[4], 1, 7); // 60 * 24 * 7
            }
            else
            {
                // every n weekdays, every m monthdays
                MDays = ParseTimes(cols[2], 1, 31);
                WDays = ParseTimes(cols[4], 1, 7); // 60 * 24 * 7
            }
            /*
            String args = "";

            for (int i = 5; i < cols.Length; i++)
                args += " " + cols[i];
             * */
        }

        private static ArrayList ParseTimes(String line, int startNr, int maxNr)
        {
            var vals = new ArrayList();

            var list = line.Split(new char[] { ',' });

            foreach (String entry in list)
            {
                int start, end, interval;

                string[] parts = entry.Split(new char[] { '-', '/' });

                if (parts[0].Equals("*"))
                {
                    if (parts.Length > 1)
                    {
                        start = startNr;
                        end = maxNr;

                        interval = int.Parse(parts[1]);
                    }
                    else
                    {
                        // put a -1 in place
                        start = -1;
                        end = -1;
                        interval = 1;
                    }
                }
                else
                {
                    // format is 0-8/2
                    start = int.Parse(parts[0]);
                    end = parts.Length > 1 ? int.Parse(parts[1]) : int.Parse(parts[0]);
                    interval = parts.Length > 2 ? int.Parse(parts[2]) : 1;
                }

                for (int i = start; i <= end; i += interval)
                {
                    vals.Add(i);
                }
            }
            return vals;
        }
        
    }
}
