using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using One.Cron.API;
using System.Configuration;
using System.Data.SqlClient;

namespace One.Cron.Core
{
    public class SqlJob : IJob
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["SqlJob"].ConnectionString; }
        }

        public string Description { get; set; }

        public bool IsRunning { get; set; }

        public int Id { get; set; }

        public string Parameters { get; set; }

        public void Execute()
        {
            if (!string.IsNullOrWhiteSpace(Parameters))
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    var command = connection.CreateCommand();
                    command.CommandTimeout = 7200;
                    // SqlHelper.ExecuteNonQuery(connection,  System.Data.CommandType.Text, Parameters);
                }
            }
        }
    }
}
