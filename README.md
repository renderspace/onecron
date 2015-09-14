#One.Cron Windows service

Windows service which can load any assembly and periodically call a method in that assembly.

## Installation

1. Create directory "c:\Program files\One.Cron.4"
2. Copy contents of zip file into directory created above.
3. Launch command prompt.
4. cd into directory created above.
5. Run "c:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe One.Cron.4.exe"
6. Make sure that the installation was successful. Check that the following message is output in command prompt:
The Install phase completed successfully, and the Commit phase is beginning.

7. Create a new database and a new user of the database.
8. In the new database run the following script to create the table "cron_jobs":

```sql
/****** Object: Table dbo.cron_jobs Script Date: 01/22/2013 13:49:07 /
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE dbo.cron_jobs(
id int IDENTITY(1,1) NOT NULL,
admin_email_list varchar(1000) NOT NULL,
crontab_entry varchar(255) NOT NULL,
last_finished datetime NULL,
fully_qualified_class_name varchar(255) NOT NULL,
last_error_message varchar(1000) NULL,
last_error_date datetime NULL,
CONSTRAINT PK_cron_jobs PRIMARY KEY CLUSTERED
(
id ASC
)WITH (PADINDEX = OFF, STATISTICSNORECOMPUTE = OFF, IGNOREDUPKEY = OFF, ALLOWROWLOCKS = ON, ALLOWPAGELOCKS = ON) ON PRIMARY
) ON PRIMARY

GO

SET ANSI_PADDING OFF
GO
```

9. In file One.Cron.4.exe.config modify connection string "cron" so that it can access the database created above.
10. In file One.Cron.4.exe.config modify <param name="File" value="c:\\logs\\cron2.log" />
11) In Microsoft Management Console for services start One.Cron.4 service.
12) Check in log file configured in point 10 whether the service started ok.
13) Stop the service.
14) Copy all dll files from the existing one.net bin folder into the One.Cron.4 directory created above.
15) In the above created database table insert row with following values:
```
adminemaillist = your email
crontab_entry = * * * * *
last_finished = leave empty (NULL)
fullyqualifiedclass_name = One.Net.BLL.Publisher
```

16) In One.Cron.4.exe.config modify the connection string "MsSqlConnectionString" so that it matches the One.NET preview connection string (in web.config)
17) Start service.
18) Check in log file configured in point 10 whether the service started ok.
