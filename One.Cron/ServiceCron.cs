using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using log4net;
using One.Cron.Core;
using ServiceDebuggerHelper;
using One.Cron.API;

namespace One.Cron
{
    public partial class ServiceCron : ServiceBase, IDebuggableService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ServiceCron));

        private const int MAX_RELOAD_COUNT = 720;
        private const int RELOAD_DELAY = 100000;
        List<BOCron> cronEntries = null;
        private Thread queueThread, controlThread;
        private bool serviceStarted, servicePaused;
        Dictionary<string, IJob> threads = new Dictionary<string, IJob>();

        private int loadCrontabErrors;

        private DateTime controlLoopLastRun = DateTime.Now;
        private DateTime reportLastRun = DateTime.Now;

        private List<string> AdminEmailList;

        public ServiceCron()
        {
            try
            {
                InitializeComponent();
                log4net.Config.XmlConfigurator.Configure();
                log.Info("One.Cron service v1.02");

                AdminEmailList = Tool.SplitString(ConfigurationManager.AppSettings["AdminEmailList"]);
            }
            catch (Exception ex)
            {
                log.Fatal("ServiceCron", ex);
                ErrorReport("OnStart", "One.Cron failed to be created!");
                throw ex;
            }
        }

        private void ErrorReport(string title, string msg)
        {
            var  message = new MailMessage();
            message.IsBodyHtml = false;

            foreach (var email in AdminEmailList)
            {
                message.To.Add(new MailAddress(email));    
            }
            message.Subject = "One.Cron - ERROR - " + title;
            message.Priority = MailPriority.High;
            message.Body = msg;

            if (message.To.Count > 0)
            {
                try
                {
                    var client = new SmtpClient();
                    client.Send(message);
                    log.Info("---------------- report sent ----------------");
                }
                catch (Exception ex)
                {
                    log.Fatal("**************** Sending report failed **************** ", ex);
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            serviceStarted = true;
            try
            {
                InitCron();

                controlThread = new Thread(ControlLoop);
                controlThread.Start();

                queueThread = new Thread(QueueWorkLoop);
                queueThread.Start();
                log.Info("OnStart: Initilized crontab, started ControlLoop and QueueWorkLoop");
            }
            catch (Exception ex)
            {
                log.Fatal("OnStart-InitCron", ex);
                ErrorReport("OnStart", "One.Cron is can't start!");
                throw ex;
            }
        }

        protected override void OnStop()
        {
            log.Info("OnStop");
            ErrorReport("OnStop", "One.Cron is stopping!");
            serviceStarted = false;
        }

        protected override void OnPause()
        {
            log.Info("OnPause");
            servicePaused = true;
            //base.OnPause();
        }

        protected override void OnContinue()
        {
            log.Info("OnContinue");
            servicePaused = false;
 	        //base.OnContinue();
        }


        private void InitCron()
        {
            servicePaused = true;
            log.Debug("Clearing existing cron entries and threads");
            cronEntries = null;
            var safeForReload = threads.Values.Count == 0;
            var waitingForReloadCount = 0;
            while (!safeForReload)
            {
                waitingForReloadCount++;
                safeForReload = true;
                foreach (var t in threads.Values)
                {
                    if (t.IsRunning)
                        safeForReload = false;
                }
                if (!safeForReload)
                {
                    if (waitingForReloadCount < MAX_RELOAD_COUNT)
                    {
                        log.Info("Waiting 100 seconds for threads to exit in order to reload crontab.");
                        Thread.Sleep(RELOAD_DELAY);
                    }
                    else
                    {
                        log.Fatal("Too many retries while waiting for threads to exit (" + ((MAX_RELOAD_COUNT * RELOAD_DELAY) / 3600000) + "hrs). Stoping service.");
                        Stop();
                        return;
                    }
                }
            }
            threads = null;

            log.Info("Now starting load of new cron entries");

            try
            {
                cronEntries = DbCron.List();
            }
            catch (Exception ex)
            {
                log.Error("InitCron()", ex);
                loadCrontabErrors++;
            }

            if (cronEntries != null && loadCrontabErrors > 0)
            {
                log.Info("Crontab error count cleared.");
                loadCrontabErrors = 0;
            }

            if (cronEntries != null)
            {
                threads = new Dictionary<string, IJob>();
                foreach (var cron in cronEntries)
                {
                    if (!threads.ContainsKey(cron.ClassName))
                    {
                        var job = JobFactory.CreateJob(cron.ClassName);
                        if (job != null)
                        {
                            threads[cron.ClassName] = job;
                        }
                        else
                        {
                            log.Error("Class " + cron.ClassName + " not loaded.");
                        }
                    }
                }
                log.Info("Found " + threads.Count + " cron plugins");
            }
            servicePaused = false;
        }

        private void ControlLoop()
        {
            while (serviceStarted)
            {
                if (DateTime.Now.Subtract(controlLoopLastRun) > new TimeSpan(3, 0, 0))
                {
                    controlLoopLastRun = DateTime.Now;
                    if (!servicePaused)
                        InitCron();
                }

                if (DateTime.Now.Subtract(reportLastRun) > new TimeSpan(24, 0, 0) ||
                    (DateTime.Now.Hour == 1 && DateTime.Now.Minute == 0 && DateTime.Now.Second == 0))
                {
                    //if (!servicePaused)
                    reportLastRun = DateTime.Now;
                    //    InitCron();
                }
                Thread.Sleep(1000);
            }
        }

        private void QueueWorkLoop()
        {
            int errorCount = 0;
            while (serviceStarted)
            {
                try
                {
                    if (!servicePaused)
                    {
                        foreach (var cron in cronEntries)
                        {
                            if (cron.IsPending && threads.ContainsKey(cron.ClassName))
                            {
                                var tInfo = new ThreadExecuteInfo { ClassName = cron.ClassName, Description = cron.Description , JobId = cron.JobId };
                                ThreadPool.QueueUserWorkItem(Execute, tInfo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (++errorCount < 5)
                    {
                        log.Error("QueueUserWorkItem exception; waiting 1 minute until retry (5 max)", ex);
                        Thread.Sleep(50000);
                    }
                    else
                    {
                        log.Fatal("QueueUserWorkItem exception; will not retry", ex);
                        ErrorReport("QueueWorkLoop", "QueueUserWorkItem exception; will not retry!");
                        serviceStarted = false;   
                    }
                }
                Thread.Sleep(10000);
            }
            Stop();
        }

        private void Execute(object a)
        {
            if (servicePaused || !serviceStarted)
                return;

            // Constrain the number of worker threads (Omitted here.)
            var threadInfo = a as ThreadExecuteInfo;

            if (threadInfo == null)
                return;

            if (!threads[threadInfo.ClassName].IsRunning)
            {
                threads[threadInfo.ClassName].IsRunning = true;
                try
                {
                    log.Error("Thread start " + threadInfo.Description + " " + threadInfo.JobId);
                    threads[threadInfo.ClassName].Description = threadInfo.Description;
                    threads[threadInfo.ClassName].Execute();
                    DbCron.TouchLastFinished(threadInfo.JobId);
                }
                catch (Exception ex)
                {
                    log.Error("Job threw an exception", ex);
                    DbCron.TouchLastErrorMessage(threadInfo.JobId, ex.Message);
                }
                finally
                {
                    threads[threadInfo.ClassName].IsRunning = false;
                }
                
            }
            else
            {
                log.Info("Skipped " + threadInfo.JobId + " becuase job with engine " + threadInfo.ClassName + " is already running.");
            }
        }

        #region IDebuggableService implementation

        public void Continue()
        {
            OnContinue();
        }

        public void Pause()
        {
            OnPause();
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }

        public void StopService()
        {
            OnStop();
        }

        #endregion
    }
}

