#region

using System;
using System.Collections.Generic;
using System.Linq;
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
        public JOBSTATUS JobStatus { get; set; }
        public PrintSystemJobInfo JobInfo { get; set; }
        public string PrinterName { get; set; }

        public PrintJobChangeEventArgs(int intJobId, string strJobName, JOBSTATUS jStatus, PrintSystemJobInfo objJobInfo, string printerName)
        {
            JobId = intJobId;
            JobName = strJobName;
            JobStatus = jStatus;
            JobInfo = objJobInfo;
            PrinterName = printerName;
        }
    }

    public delegate void PrintJobStatusChanged(object sender, PrintJobChangeEventArgs e);

    public class PrintQueueMonitor
    {
        #region DLL Import Functions

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(string pPrinterName,
            out IntPtr phPrinter,
            int pDefault);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter",
            SetLastError = true,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter
            (int hPrinter);

        [DllImport("winspool.drv",
            EntryPoint = "FindFirstPrinterChangeNotification",
            SetLastError = true, CharSet = CharSet.Ansi,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FindFirstPrinterChangeNotification
        ([In] IntPtr hPrinter,
            [In] int fwFlags,
            [In] int fwOptions,
            [In] [MarshalAs(UnmanagedType.LPStruct)]
            PRINTER_NOTIFY_OPTIONS pPrinterNotifyOptions);

        [DllImport("winspool.drv", EntryPoint = "FindNextPrinterChangeNotification",
            SetLastError = true, CharSet = CharSet.Ansi,
            ExactSpelling = false,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool FindNextPrinterChangeNotification
        ([In] IntPtr hChangeObject,
            [Out] out int pdwChange,
            [In] [MarshalAs(UnmanagedType.LPStruct)]
            PRINTER_NOTIFY_OPTIONS pPrinterNotifyOptions,
            [Out] out IntPtr lppPrinterNotifyInfo
        );

        #endregion

        #region Constants

        private const int PRINTER_NOTIFY_OPTIONS_REFRESH = 1;

        #endregion

        #region Events

        public event PrintJobStatusChanged OnJobStatusChange;

        #endregion

        #region private variables

        private IntPtr _printerHandle = IntPtr.Zero;
        private readonly string _spoolerName;
        private readonly ManualResetEvent _mrEvent = new ManualResetEvent(false);
        private RegisteredWaitHandle _waitHandle;
        private IntPtr _changeHandle = IntPtr.Zero;
        private readonly PRINTER_NOTIFY_OPTIONS _notifyOptions = new PRINTER_NOTIFY_OPTIONS();
        private readonly Dictionary<int, string> _objJobDict = new Dictionary<int, string>();
        private PrintQueue _spooler;

        #endregion

        #region constructor

        public PrintQueueMonitor(string strSpoolName)
        {
            // Let us open the printer and get the printer handle.
            _spoolerName = strSpoolName;
            //Start Monitoring
            Start();
        }

        #endregion

        #region destructor

        ~PrintQueueMonitor()
        {
            Stop();
        }

        #endregion

        #region StartMonitoring

        public void Start()
        {
            OpenPrinter(_spoolerName, out _printerHandle, 0);
            if (_printerHandle != IntPtr.Zero)
            {
                //We got a valid Printer handle.  Let us register for change notification....
                //_changeHandle = FindFirstPrinterChangeNotification(_printerHandle, (int) PRINTER_CHANGES.PRINTER_CHANGE_JOB, 0, _notifyOptions);
                _changeHandle = FindFirstPrinterChangeNotification(_printerHandle, (int) PRINTER_CHANGES.PRINTER_CHANGE_ALL, 0, _notifyOptions);
                // We have successfully registered for change notification.  Let us capture the handle...
                _mrEvent.SafeWaitHandle = new SafeWaitHandle(_changeHandle, true);
                //Now, let us wait for change notification from the printer queue.... (-1 is INFINITE wait)
                _waitHandle = ThreadPool.RegisterWaitForSingleObject(_mrEvent, PrinterNotifyWaitCallback, _mrEvent, -1, true);
            }

            _spooler = new PrintQueue(new PrintServer(), _spoolerName);
            foreach (var psi in _spooler.GetPrintJobInfoCollection())
            {
                _objJobDict[psi.JobIdentifier] = psi.Name;
            }
        }

        #endregion

        #region StopMonitoring

        public void Stop()
        {
            if (_printerHandle != IntPtr.Zero)
            {
                ClosePrinter((int) _printerHandle);
                _printerHandle = IntPtr.Zero;
            }
        }

        #endregion


        #region Callback Function

        public void PrinterNotifyWaitCallback(object state, bool timedOut)
        {
            try
            {
                if (_printerHandle == IntPtr.Zero)
                {
                    return;
                }

                #region read notification details

                _notifyOptions.Count = 1;
                var pdwChange = 0;
                var pNotifyInfo = IntPtr.Zero;
                var bResult = FindNextPrinterChangeNotification(_changeHandle, out pdwChange, _notifyOptions, out pNotifyInfo);

                // if a notification overflow occurred, ...
                if ((int) pNotifyInfo == 1)
                {
                    // ...we must refresh to continue
                    _notifyOptions.dwFlags = PRINTER_NOTIFY_OPTIONS_REFRESH;
                    bResult = FindNextPrinterChangeNotification(_changeHandle, out pdwChange, _notifyOptions, out pNotifyInfo);
                }

                //If the Printer Change Notification Call did not give data, exit code
                if ((bResult == false) || (((int) pNotifyInfo) == 0)) return;

                //If the Change Notification was not relgated to job, exit code
                var bJobRelatedChange = ((pdwChange & PRINTER_CHANGES.PRINTER_CHANGE_ADD_JOB) == PRINTER_CHANGES.PRINTER_CHANGE_ADD_JOB) ||
                                        ((pdwChange & PRINTER_CHANGES.PRINTER_CHANGE_SET_JOB) == PRINTER_CHANGES.PRINTER_CHANGE_SET_JOB) ||
                                        ((pdwChange & PRINTER_CHANGES.PRINTER_CHANGE_DELETE_JOB) == PRINTER_CHANGES.PRINTER_CHANGE_DELETE_JOB) ||
                                        ((pdwChange & PRINTER_CHANGES.PRINTER_CHANGE_WRITE_JOB) == PRINTER_CHANGES.PRINTER_CHANGE_WRITE_JOB);

                if (!bJobRelatedChange)
                {
                    return;
                }

                #endregion

                #region populate Notification Information

                //Now, let us initialize and populate the Notify Info data
                var info = (PRINTER_NOTIFY_INFO) Marshal.PtrToStructure(pNotifyInfo, typeof(PRINTER_NOTIFY_INFO));
                var pData = (int) pNotifyInfo + Marshal.SizeOf(typeof(PRINTER_NOTIFY_INFO));
                var data = new PRINTER_NOTIFY_INFO_DATA[info.Count];
                for (uint i = 0; i < info.Count; i++)
                {
                    data[i] = (PRINTER_NOTIFY_INFO_DATA) Marshal.PtrToStructure((IntPtr) pData, typeof(PRINTER_NOTIFY_INFO_DATA));
                    pData += Marshal.SizeOf(typeof(PRINTER_NOTIFY_INFO_DATA));
                }

                #endregion

                #region iterate through all elements in the data array

                for (var i = 0; i < data.Count(); i++)
                {
                    if ((data[i].Field == (ushort) PRINTERJOBNOTIFICATIONTYPES.JOB_NOTIFY_FIELD_STATUS) &&
                        (data[i].Type == (ushort) PRINTERNOTIFICATIONTYPES.JOB_NOTIFY_TYPE)
                    )
                    {
                        var jStatus = (JOBSTATUS) Enum.Parse(typeof(JOBSTATUS), data[i].NotifyData.Data.cbBuf.ToString());
                        var intJobId = (int) data[i].Id;
                        var strJobName = "";
                        PrintSystemJobInfo pji = null;
                        try
                        {
                            _spooler = new PrintQueue(new PrintServer(), _spoolerName);
                            pji = _spooler.GetJob(intJobId);
                            if (!_objJobDict.ContainsKey(intJobId))
                                _objJobDict[intJobId] = pji.Name;
                            strJobName = pji.Name;
                        }
                        catch
                        {
                            pji = null;
                            _objJobDict.TryGetValue(intJobId, out strJobName);
                            if (strJobName == null) strJobName = "";
                        }

                        //Let us raise the event
                        if (OnJobStatusChange != null)
                        {
                            OnJobStatusChange(this, new PrintJobChangeEventArgs(intJobId, strJobName, jStatus, pji, _spoolerName));
                        }
                    }
                }
            }
            catch (Exception)
            {
                //Debug.WriteLine(ex.Message);
            }
            finally
            {
                _mrEvent.Reset();
                _waitHandle = ThreadPool.RegisterWaitForSingleObject(_mrEvent, PrinterNotifyWaitCallback, _mrEvent, -1, true);
            }

            #endregion
        }

        #endregion
    }
}