#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using PrintMonitorRelay.Settings;

#endregion

namespace PrintMonitorRelay
{
    public partial class PrintMonitorRelay : ServiceBase
    {
        private List<PrintQueueMonitor> _printQueues;
        private List<int> _jobsAlreadyCompleted;

        public PrintMonitorRelay()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            AppSettings.LoadSettings();
            InitWatcher();
        }

        protected override void OnStop()
        {
        }

        private void InitWatcher()
        {
            _jobsAlreadyCompleted = new List<int>();
            _printQueues = new List<PrintQueueMonitor>();
            foreach (var printerSetting in AppSettings.PrinterSettings)
            {
                _printQueues.Add(PrintQueueMonitorFactory(printerSetting.PrinterName));
            }
        }

        private PrintQueueMonitor PrintQueueMonitorFactory(string printerName)
        {
            // Factory method to create a thread for each printer we want to monitor
            var printQueueMonitor = new PrintQueueMonitor(printerName);
            printQueueMonitor.OnJobStatusChange += OnJobStatusChange;
            return printQueueMonitor;
        }

        private void OnJobStatusChange(object sender, PrintJobChangeEventArgs e)
        {
            if (AlreadyNotifiedJob(e)) return;

            var appSetting = AppSettings.PrinterSettings.FirstOrDefault(pq => pq.PrinterName == e.PrinterName);
            if (appSetting == null) return;

            // If we found it, ping the relay (turn it on) 
            PingRelay(appSetting);
        }

        private bool AlreadyNotifiedJob(PrintJobChangeEventArgs e)
        {
            try
            {
                if (e.PrinterName.IsEmpty() || e.JobStatus.ToString().IsEmpty() || _jobsAlreadyCompleted.Contains(e.JobId)) return true;
                _jobsAlreadyCompleted.Add(e.JobId);
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static async void PingRelay(PrinterSetting printerSetting)
        {
            //var eventLoge = new EventLog {Source = "Print Monitor Relay", Log = "Application"};
            //eventLoge.WriteEntry(printerSetting.PrinterName, EventLogEntryType.Information);
            //eventLoge.Close();
            try
            {
                await new HttpClient().GetAsync(printerSetting.Ip);
            }
            catch(Exception ex)
            {
                var eventLog = new EventLog { Source = "Print Monitor Relay", Log = "Application" };
                eventLog.WriteEntry(ex.Message, EventLogEntryType.Information);
                eventLog.Close();
            }
        }
    }
}