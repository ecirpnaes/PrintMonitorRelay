#region

using System;
using System.Collections.Generic;
using System.Printing;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

#endregion

namespace PrintMonitorRelay
{
    public class PrintJobChangeEventArgs : EventArgs
    {
        //https://www.codeproject.com/Articles/51085/Monitor-jobs-in-a-printer-queue-NET
        public int JobId { get; set; }
        public string JobName { get; set; }
        public JobStatusEnum JobStatus { get; set; }
        public PrintSystemJobInfo JobInfo { get; set; }
        public string PrinterName { get; set; }

        public PrintJobChangeEventArgs(int jobId, string jobName, JobStatusEnum jobStatusEnum, PrintSystemJobInfo printSystemJobInfo, string printerName)
        {
            JobId = jobId;
            JobName = jobName;
            JobStatus = jobStatusEnum;
            JobInfo = printSystemJobInfo;
            PrinterName = printerName;
        }
    }

    public delegate void PrintJobStatusChanged(object sender, PrintJobChangeEventArgs e);

    public class PrintQueueMonitor
    {
        #region DLL Import Functions

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, int pDefault);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(int hPrinter);

        [DllImport("winspool.drv", EntryPoint = "FindFirstPrinterChangeNotification", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FindFirstPrinterChangeNotification([In] IntPtr hPrinter, [In] int fwFlags, [In] int fwOptions, [In] [MarshalAs(UnmanagedType.LPStruct)]
            PrinterNotifyOptions pPrinterNotifyOptions);

        [DllImport("winspool.drv", EntryPoint = "FindNextPrinterChangeNotification", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool FindNextPrinterChangeNotification([In] IntPtr hChangeObject, [Out] out int pdwChange, [In] [MarshalAs(UnmanagedType.LPStruct)]
            PrinterNotifyOptions pPrinterNotifyOptions, [Out] out IntPtr lppPrinterNotifyInfo);

        [DllImport("winspool.drv", EntryPoint = "FreePrinterNotifyInfo", SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern int FreePrinterNotifyInfo(IntPtr pPrinterNotifyInfo);

        #endregion

        #region Events

        public event PrintJobStatusChanged OnJobStatusChange;

        #endregion

        #region private variables

        private IntPtr _printerHandle = IntPtr.Zero;
        private readonly string _spoolerName;
        private ManualResetEvent _manualResetEvent;
        private RegisteredWaitHandle _registeredWaitHandle;
        private IntPtr _changeHandle = IntPtr.Zero;
        private PrinterNotifyOptions _printerNotifyOptions;

        private readonly Dictionary<int, string> dictionary = new Dictionary<int, string>();
        //private PrintQueue _spooler;

        #endregion

        public PrintQueueMonitor(string printerName)
        {
            _spoolerName = printerName;
            Start();
        }

        ~PrintQueueMonitor()
        {
            Stop();
        }

        public void Start()
        {
            OpenPrinter(_spoolerName, out _printerHandle, 0);

            // if we don't have a valid handle, don't even try
            if (_printerHandle != IntPtr.Zero)
            {
                _printerNotifyOptions = new PrinterNotifyOptions();
                _manualResetEvent = new ManualResetEvent(false);

                // Get a handle to a waiting object, providing the fields we are interested in
                _changeHandle = FindFirstPrinterChangeNotification(_printerHandle, (int) PrinterChangesEnum.ChangeAll, 0, _printerNotifyOptions);

                // assign the handle to the ManualResetEvent
                _manualResetEvent.SafeWaitHandle = new SafeWaitHandle(_changeHandle, true);

                // Wait for change notification from the printer queue.... (-1 = INFINITE wait)
                _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_manualResetEvent, PrinterNotifyWaitCallback, _manualResetEvent, -1, true);
            }

            //_spooler = new PrintQueue(new PrintServer(), _spoolerName);
            //foreach (var psi in _spooler.GetPrintJobInfoCollection())
            //{
            //    dictionary[psi.JobIdentifier] = psi.Name;
            //}
        }

        public void Stop()
        {
            if (_printerHandle == IntPtr.Zero) return;
            ClosePrinter((int) _printerHandle);
            _printerHandle = IntPtr.Zero;
        }

        public void PrinterNotifyWaitCallback(object state, bool timedOut)
        {
            try
            {
                if (_printerHandle == IntPtr.Zero)
                {
                    return;
                }

                _printerNotifyOptions.Count = 1;
                var result = FindNextPrinterChangeNotification(_changeHandle, out var changeType, _printerNotifyOptions, out var printerNotifyInfo);

                // if a notification overflow occurred, ...
                if (printerNotifyInfo.ToInt32() == 1)
                {
                    // ...we must refresh to continue                   
                    result = FindNextPrinterChangeNotification(_changeHandle, out changeType, new PrinterNotifyOptions(true), out printerNotifyInfo);
                }

                //If the Printer Change Notification Call did not give data, exit 
                if (result == false || printerNotifyInfo == IntPtr.Zero) return;

                //If the Change Notification was not relegated to job, exit 
                if (!ChangeTypeIsJob(changeType)) return;

                //Now, let us initialize and populate the Notify Info data
                var printerInfo = (PrinterNotifyInfo) Marshal.PtrToStructure(printerNotifyInfo, typeof(PrinterNotifyInfo));
                var infoSize = (int) printerNotifyInfo + Marshal.SizeOf(typeof(PrinterNotifyInfo));
                var printerNotifyInfoData = new PrinterNotifyInfoData[printerInfo.Count];
                for (uint i = 0; i < printerInfo.Count; i++)
                {
                    printerNotifyInfoData[i] = (PrinterNotifyInfoData) Marshal.PtrToStructure((IntPtr) infoSize, typeof(PrinterNotifyInfoData));
                    infoSize += Marshal.SizeOf(typeof(PrinterNotifyInfoData));
                }

                for (var i = 0; i < printerNotifyInfoData.Length; i++)
                {
                    if (printerNotifyInfoData[i].Field != (ushort) JobNotifyTypeEnum.Status || (printerNotifyInfoData[i].Type != (ushort) NotificationTypeEnum.Job)) continue;

                    var jobStatusEnum = (JobStatusEnum) Enum.Parse(typeof(JobStatusEnum), printerNotifyInfoData[i].NotifyData.Data.cbBuf.ToString());
                    var jobId = (int) printerNotifyInfoData[i].Id;
                    //string jobName;
                    //PrintSystemJobInfo printSystemJobInfo = null;
                    //try
                    //{
                    //    _spooler = new PrintQueue(new PrintServer(), _spoolerName);
                    //    printSystemJobInfo = _spooler.GetJob(jobId);
                    //    if (!objJobDict.ContainsKey(jobId))
                    //        objJobDict[jobId] = printSystemJobInfo.Name;
                    //    jobName = printSystemJobInfo.Name;
                    //}
                    //catch
                    //{
                    //    printSystemJobInfo = null;
                    //    objJobDict.TryGetValue(jobId, out jobName);
                    //    if (jobName == null) jobName = "";
                    //}

                    //Let us raise the event
                    if (OnJobStatusChange != null)
                    {
                        OnJobStatusChange(this, new PrintJobChangeEventArgs(jobId, string.Empty, jobStatusEnum, null, _spoolerName));
                    }
                }

                FreePrinterNotifyInfo(printerNotifyInfo);
            }
            catch (Exception)
            {
                //Debug.WriteLine(ex.Message);
            }
            finally
            {
                _manualResetEvent.Reset();
                _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_manualResetEvent, PrinterNotifyWaitCallback, _manualResetEvent, -1, true);
            }
        }

        private static bool ChangeTypeIsJob(int changeType)
        {
            return GetChangeType(changeType, PrinterChangesEnum.AddJob) ||
                   GetChangeType(changeType, PrinterChangesEnum.SetJob) ||
                   GetChangeType(changeType, PrinterChangesEnum.DeleteJob) ||
                   GetChangeType(changeType, PrinterChangesEnum.WriteJob);
        }

        private static bool GetChangeType(int changeType, PrinterChangesEnum printerChangesEnum)
        {
            return (changeType & (int) printerChangesEnum) == (int) printerChangesEnum;
        }
    }
}