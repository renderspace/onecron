using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Collections;
using System.Configuration;
using System.Reflection;
using System.Data.SqlClient;
using log4net;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
using One.Cron.Core;
using System.Data;


namespace One.Cron
{
    [RunInstaller(true)]
    public partial class InstallerCron : Installer
    {
        public InstallerCron()
        {
            InitializeComponent();

            var serviceProcessInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //# Service Account Information

            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            // Service Information

            serviceInstaller.DisplayName = "One.Cron.4";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.DelayedAutoStart = true;

            // we should read this: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server
            // serviceInstaller.ServicesDependedOn = new[] { "MSSQLSERVER" }; 

            // This must be identical to the WindowsService.ServiceBase name
            // set in the constructor of WindowsService.cs

            serviceInstaller.ServiceName = "One.Cron.4";

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }

        // source: http://www.codeproject.com/Tips/446121/Adding-connection-string-during-installation
        protected override void OnAfterInstall(IDictionary savedState)
        {
            // Debugger.Launch();
            base.OnAfterInstall(savedState);

            string dataSource = Context.Parameters["DataSource"];
            dataSource = dataSource.Replace('/', ';');
            // MessageBox.Show("instance=" + dataSource);
            dataSource = dataSource.Replace("$", @"\");
            dataSource = dataSource.Replace("UserID", @"User ID");
            // During Connection String Formation an Extra forward slash is found 
            dataSource = dataSource.Replace(@"\\", @"\");
            dataSource = "Data source = " + dataSource.Replace("InitialCatalog", @"Initial Catalog");
            string pwd = Context.Parameters["Password"];

            var configFileName = Assembly.GetExecutingAssembly().Location + ".config";

            // MessageBox.Show( + dataSource);

            var doc2 = XDocument.Load(configFileName);
            var list5 = from appNode in doc2.Descendants("connectionStrings").Elements()
                        where appNode.Attribute("name").Value == "cron"
                        select appNode;
            var element5 = list5.FirstOrDefault();
            element5.Attribute("connectionString").Value = dataSource;
            doc2.Save(configFileName);

            //var con = new SqlConnection(dataSource);
            /*try
            {
                using (SqlDataReader reader = SqlHelper.ExecuteReader(dataSource, CommandType.Text,
                    "SELECT  * FROM [dbo].[cron]"))
                    while (reader.Read()) { }
                MessageBox.Show("connection works");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            } */


        }

    }
}
