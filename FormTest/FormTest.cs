#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PrintMonitorRelay;
using PrintMonitorRelay.Settings;

#endregion

namespace FormTest
{
    public partial class FormTest : Form
    {
        private List<PrintQueueMonitor> _printQueues;
        private List<int> _jobsAlreadyCompleted;
        private Dictionary<string, List<string>> _jobs;

        public FormTest()
        {
            InitializeComponent();
        }

        private void FormTest_Load(object sender, EventArgs e)
        {
            AppSettings.LoadSettings();
            _jobsAlreadyCompleted = new List<int>();
            _printQueues = new List<PrintQueueMonitor>();
            _jobs = new Dictionary<string, List<string>>();

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
            if (e.PrinterName.IsEmpty() || e.JobStatus.ToString().IsEmpty()) return;
            var jobId = GetJobId(e.JobId);
            var key = e.PrinterName + " : " + jobId;

            var list = _jobs.ContainsKey(key) ? _jobs[key] : new List<string>();
            list.Add(e.JobStatus.ToString());
            _jobs[key] = list;
            WriteText();

            //var appSetting = AppSettings.PrinterSettings.FirstOrDefault(pq => pq.PrinterName == e.PrinterName);
            //if (appSetting == null) return;
        }

        private void WriteText()
        {
            var stringBuilder = new StringBuilder();
            foreach (var keyValuePair in _jobs)
            {
                stringBuilder.AppendLine(keyValuePair.Key);
                foreach (var status in keyValuePair.Value)
                {
                    stringBuilder.AppendLine("      " + status);
                }

                stringBuilder.AppendLine("");
            }
            textBox1.Invoke((Action)delegate { textBox1.Text = stringBuilder.ToString(); });
        }

        //private bool AlreadyNotifiedJob(PrintJobChangeEventArgs e)
        //{
        //    try
        //    {
        //        var jobId = GetJobId(e.JobId);
        //        if (e.PrinterName.IsEmpty() || e.JobStatus.ToString().IsEmpty() || _jobsAlreadyCompleted.Contains(jobId)) return true;
        //        _jobsAlreadyCompleted.Add(jobId);
        //        return false;
        //    }
        //    catch (Exception)
        //    {
        //        return true;
        //    }
        //}

        private static int GetJobId(int jobId)
        {
            // creates a unique job id by appending minute of the day
            var now = DateTime.UtcNow;
            return (now.Hour * 60) + now.Minute + jobId;
        }
    }
}