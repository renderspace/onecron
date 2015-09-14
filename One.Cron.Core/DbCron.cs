using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace One.Cron.Core
{
    public class DbCron
    {
        public static List<BOCron> List()
        {
            var crons = new List<BOCron>();
            using (SqlDataReader reader = SqlHelper.ExecuteReader(DbHelper.ConnectionString, CommandType.Text,
                "SELECT  id, admin_email_list, crontab_entry, last_finished, fully_qualified_class_name FROM [dbo].[cron_jobs]"))
                while (reader.Read())
                    crons.Add(PopulateCron(reader));
            return crons;
        }

        public static BOCron Get(int id)
        {
            BOCron cron = null;

            using (SqlDataReader reader = SqlHelper.ExecuteReader(DbHelper.ConnectionString, CommandType.Text,
                @"SELECT  id, admin_email_list, crontab_entry, last_finished, fully_qualified_class_name
                    FROM [dbo].[cron_jobs] WHERE id=@id", new SqlParameter("@id", id)))
            {
                if (reader.Read())
                {
                    cron = PopulateCron(reader);
                }
            }

            return cron;
        }

        private static BOCron PopulateCron(IDataRecord reader)
        {
            var cron = new BOCron
                           {
                               JobId = (int) reader["id"],
                               AdminEmailList = (string) reader["admin_email_list"],
                               CrontabEntry = (string) reader["crontab_entry"],
                               LastFinished = (reader["last_finished"] == DBNull.Value ? null : (DateTime?) reader["last_finished"]),
                               ClassName = (string)reader["fully_qualified_class_name"]
                           };
            return cron;
        }

        public static void CleanCronData()
        {
            SqlHelper.ExecuteNonQuery(DbHelper.ConnectionString, CommandType.Text,
                "UPDATE [dbo].[cron_jobs] SET queue_idx = NULL, started = NULL");
        }

        public static void Delete(int id)
        {
            SqlHelper.ExecuteNonQuery(DbHelper.ConnectionString, CommandType.Text,
                "DELETE FROM [dbo].[cron_jobs] WHERE id=@id", new SqlParameter("@id", id));
        }

        public static void Change(BOCron cron)
        {
            var paramsToPass = new SqlParameter[5];
            string sql;
            paramsToPass[1] = new SqlParameter("@AdminEmailList", cron.AdminEmailList);
            paramsToPass[2] = new SqlParameter("@CrontabEntry", cron.CrontabEntry);
            paramsToPass[3] = SqlHelper.GetNullable("@LastFinished", cron.LastFinished);
            paramsToPass[4] = new SqlParameter("@ClassName", cron.ClassName);

            if (cron.JobId > 0)
            {
                paramsToPass[0] = new SqlParameter("@JobId", cron.JobId);
                sql =
                    @"UPDATE [dbo].[cron_jobs] SET admin_email_list = @AdminEmailList, crontab_entry = @CrontabEntry, 
                            last_finished = @LastFinished, fully_qualified_class_name = @ClassName
                        WHERE id = @JobId";
            }
            else
            {
                paramsToPass[0] = new SqlParameter("@Id", DBNull.Value);
                paramsToPass[0].Direction = ParameterDirection.InputOutput;
                paramsToPass[0].DbType = DbType.Int32;
                sql =
                    @"INSERT [dbo].[cron] (admin_email_list, crontab_entry, last_finished, settings, fully_qualified_class_name) VALUES 
                            (@AdminEmailList, @CrontabEntry, @LastFinished, @ClassName)
                        WHERE id = @JobId; SET @JobId = SCOPE_IDENTITY();";
            }

            SqlHelper.ExecuteNonQuery(DbHelper.ConnectionString, CommandType.Text, sql, paramsToPass);

            if (cron.JobId == 0)
                cron.JobId = Int32.Parse(paramsToPass[0].Value.ToString());
        }

        public static void TouchLastFinished(int jobId)
        {
            var sql =
                    @"UPDATE [dbo].[cron_jobs] SET last_finished = getdate()
                        WHERE id = @JobId";
            SqlHelper.ExecuteNonQuery(DbHelper.ConnectionString, CommandType.Text, sql, new SqlParameter("@JobId", jobId));
        }

        public static void TouchLastErrorMessage(int jobId, string message)
        {
            var sql =
                    @"UPDATE [dbo].[cron_jobs] SET last_error_message = @Message, last_error_date = getdate()
                        WHERE id = @JobId";
            SqlHelper.ExecuteNonQuery(DbHelper.ConnectionString, CommandType.Text, sql, new SqlParameter("@JobId", jobId), new SqlParameter("@Message", message));
        }
    }
}
