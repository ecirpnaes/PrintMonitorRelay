#region

using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Printing;
using System.ServiceProcess;
using System.Timers;
using PrintMonitorRelay.Settings;

#endregion

namespace PrintMonitorRelay
{
    public partial class PrintMonitorRelay : ServiceBase
    {
        private static Timer _timer;

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

        private static void InitWatcher()
        {
            var query = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 1), "TargetInstance isa \"Win32_Process\"");
            var watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += Watcher_EventArrived;
            watcher.Start();
        }

        private static void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var instanceName = ((ManagementBaseObject) e.NewEvent["TargetInstance"])["Name"].ToString();
                if (string.IsNullOrEmpty(instanceName)) return;
                if (instanceName.ToLower() == "printisolationhost.exe")
                {
                    CheckPrinterName();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void CheckPrinterName()
        {
            try
            {
                var printQueues = new PrintServer().GetPrintQueues();
                foreach (var printerSettings in AppSettings.PrinterSettings)
                {
                    var printQueue = printQueues.FirstOrDefault(pq => pq.Name == printerSettings.PrinterName);
                    if (printQueue == null) continue;
                    if (!printQueue.IsProcessing && !printQueue.IsPrinting && printQueue.NumberOfJobs <= 0) continue;

                    PingRelay(printerSettings.IpOn);

                    _timer = new Timer {Interval = TimeSpan.FromSeconds(printerSettings.Duration).TotalMilliseconds, AutoReset = false};
                    _timer.Elapsed += (sender, e) => TimerElasped(sender, e, printerSettings);
                    _timer.Start();
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static void TimerElasped(object sender, ElapsedEventArgs e, AppSettings.PrinterSetting printerSettings)
        {
            PingRelay(printerSettings.IpOff);
        }

        private static async void PingRelay(string ip)
        {
            await new HttpClient().GetAsync(ip);
        }
    }
}