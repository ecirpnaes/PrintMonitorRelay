#region

using System;
using System.Globalization;
using System.Runtime.InteropServices;

#endregion

namespace PrintMonitorRelay
{
    public enum JobControlEnum
    {
        Pause = 1,
        Resume = 2,
        Cancel = 3,
        Restart = 4,
        Delete = 5,
        SentToPrinter = 6,
        LastPageEjected = 7,
        Retain = 8,
        Release = 9
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;

        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;

        public short dmLogPixels;
        public short dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;

        public override string ToString()
        {
            return $@"dmDeviceName == '{dmDeviceName}',
dmSpecVersion == {dmSpecVersion},
dmDriverVersion == {dmDriverVersion},
dmSize == {dmSize},
dmDriverExtra == {dmDriverExtra},
dmFields == {dmFields},
dmPositionX == {dmPositionX},
dmPositionY == {dmPositionY},
dmDisplayOrientation == {dmDisplayOrientation},
dmDisplayFixedOutput == {dmDisplayFixedOutput},
dmColor == {dmColor},
dmDuplex == {dmDuplex},
dmYResolution == {dmYResolution},
dmTTOption == {dmTTOption},
dmCollate == {dmCollate},
dmFormName == {dmFormName},
dmLogPixels == {dmLogPixels},
dmBitsPerPel == {dmBitsPerPel},
dmPelsWidth == {dmPelsWidth},
dmPelsHeight == {dmPelsHeight},
dmDisplayFlags == {dmDisplayFlags},
dmDisplayFrequency == {dmDisplayFrequency},
dmICMMethod == {dmICMMethod},
dmICMIntent == {dmICMIntent},
dmMediaType == {dmMediaType},
dmPanningWidth == {dmPanningWidth},
dmPanningHeight == {dmPanningHeight}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemTime
    {
        [MarshalAs(UnmanagedType.U2)]
        public short Year;

        [MarshalAs(UnmanagedType.U2)]
        public short Month;

        [MarshalAs(UnmanagedType.U2)]
        public short DayOfWeek;

        [MarshalAs(UnmanagedType.U2)]
        public short Day;

        [MarshalAs(UnmanagedType.U2)]
        public short Hour;

        [MarshalAs(UnmanagedType.U2)]
        public short Minute;

        [MarshalAs(UnmanagedType.U2)]
        public short Second;

        [MarshalAs(UnmanagedType.U2)]
        public short Milliseconds;

        public SystemTime(DateTime dt)
        {
            dt = dt.ToUniversalTime(); // SetSystemTime expects the SYSTEMTIME in UTC
            Year = (short) dt.Year;
            Month = (short) dt.Month;
            DayOfWeek = (short) dt.DayOfWeek;
            Day = (short) dt.Day;
            Hour = (short) dt.Hour;
            Minute = (short) dt.Minute;
            Second = (short) dt.Second;
            Milliseconds = (short) dt.Millisecond;
        }

        public DateTime ToDateTime()
        {
            return new DateTime(Year, Month, Day, Hour, Minute, Second, Milliseconds, CultureInfo.CurrentCulture.Calendar, DateTimeKind.Utc).ToLocalTime();
        }
    }

    [Flags]
    public enum JobStatusEnum
    {
        Paused = 0x00000001,
        Error = 0x00000002,
        Deleting = 0x00000004,
        Spooling = 0x00000008,
        Printing = 0x00000010,
        Offline = 0x00000020,
        PaperOut = 0x00000040,
        Printed = 0x00000080,
        Deleted = 0x00000100,
        Blocked = 0x00000200,
        UserIntervention = 0x00000400,
        Restart = 0x00000800,
        Complete = 0x00001000,
        Retained = 0x00002000,
        RenderingLocally = 0x00004000
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PrinterInfo2
    {
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pServerName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pPrinterName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pShareName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pPortName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDriverName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pComment;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pLocation;

        public IntPtr pDevMode;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pSepFile;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pPrintProcessor;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDatatype;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pParameters;

        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct JobInfo1
    {
        public uint JobId;
        public string pPrinterName;
        public string pMachineName;
        public string pUserName;
        public string pDocument;
        public string pDatatype;
        public string pStatus;
        public uint Status;
        public uint Priority;
        public uint Position;
        public uint TotalPages;
        public uint PagesPrinted;
        public SystemTime Submitted;
    }

    public struct Jobinfo
    {
        public int JobId;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pPrinterName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pMachineName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pUserName;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDocument;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDatatype;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string pStatus;

        public int Status;
        public int Priority;
        public int Position;
        public int TotalPages;
        public int PagesPrinted;
        public SystemTime Submitted;
    }

    [Flags]
    public enum PrinterAttributesEnum
    {
        Queued = 1,
        Direct = 2,
        Default = 4,
        Shared = 8,
        Network = 0x10,
        Hidden = 0x20,
        Local = 0x40,
        EnableDevq = 0x80,
        KeepPrintedJobs = 0x100,
        DoCompleteFirst = 0x200,
        WorkOffline = 0x400,
        EnableBidi = 0x800,
        RawOnly = 0x1000,
        Published = 0x2000
    }

    [Flags]
    public enum PrinterStatusEnum
    {
        Paused = 1,
        Error = 2,
        PendingDeletion = 4,
        PaperJam = 8,
        PaperOut = 0x10,
        ManualFeed = 0x20,
        PaperProblem = 0x40,
        Offline = 0x80,
        IoActive = 0x100,
        Busy = 0x200,
        Printing = 0x400,
        OutputBinFull = 0x800,
        NotAvailable = 0x1000,
        Waiting = 0x2000,
        Processing = 0x4000,
        Initializing = 0x8000,
        WarmingUp = 0x10000,
        TonerLow = 0x20000,
        NoToner = 0x40000,
        PagePunt = 0x80000,
        UserIntervention = 0x100000,
        OutOfMemory = 0x200000,
        DoorOpen = 0x400000,
        ServerUnknown = 0x800000,
        PowerSave = 0x1000000
    }

    [Flags]
    public enum PrinterFlagsEnum
    {
        Default = 0x00000001,
        Local = 0x00000002,
        Connections = 0x00000004,
        Favorite = 0x00000004,
        Name = 0x00000008,
        Remote = 0x00000010,
        Shared = 0x00000020,
        Network = 0x00000040,
        Expand = 0x00004000,
        Container = 0x00008000,
        Iconmask = 0x00ff0000,
        Icon1 = 0x00010000,
        Icon2 = 0x00020000,
        Icon3 = 0x00040000,
        Icon4 = 0x00080000,
        Icon5 = 0x00100000,
        Icon6 = 0x00200000,
        Icon7 = 0x00400000,
        Icon8 = 0x00800000,
        Hide = 0x01000000
    }

    [Flags]
    public enum PrinterChangesEnum : uint
    {
        AddPrinter = 1,
        SetPrinter = 2,
        DeletePrinter = 4,
        FailedToConnect = 8,
        ChangePrinter = 0xFF,
        AddJob = 0x100,
        SetJob = 0x200,
        DeleteJob = 0x400,
        WriteJob = 0x800,
        Job = 0xFF00,
        AddForm = 0x10000,
        SetForm = 0x20000,
        DeleteForm = 0x40000,
        Form = 0x70000,
        AddPort = 0x100000,
        ConfigurePort = 0x200000,
        DeletePort = 0x400000,
        Port = 0x700000,
        AddPrintProcessor = 0x1000000,
        DeletePrintProcessor = 0x4000000,
        PrintProcessor = 0x7000000,
        AddPrinterDriver = 0x10000000,
        SetPrinterDriver = 0x20000000,
        DeletePrinterDriver = 0x40000000,
        PrinterDriver = 0x70000000,
        Timeout = 0x80000000,
        ChangeAll = 0x7777FFFF
    }

    public enum PrinterNotifyTypeEnum
    {
        ServerName = 0,
        PrinterName = 1,
        ShareName = 2,
        PortName = 3,
        DriverName = 4,
        Comment = 5,
        Location = 6,
        DevMode = 7,
        SepFile = 8,
        PrintProcessor = 9,
        Parameters = 10,
        DataType = 11,
        SecurityDescriptor = 12,
        Attributes = 13,
        Priority = 14,
        DefaultPriority = 15,
        StartTime = 16,
        UntilTime = 17,
        Status = 18,
        StatusString = 19,
        CJobs = 20,
        AveragePagesPerMin = 21,
        TotalPages = 22,
        PagesPrinted = 23,
        TotalBytes = 24,
        BytesPrinted = 25
    }

    public enum JobNotifyTypeEnum
    {
        PrinterName = 0,
        MachineName = 1,
        PortName = 2,
        UserName = 3,
        NotifyName = 4,
        DataType = 5,
        PrintProcessor = 6,
        Paramters = 7,
        DriverName = 8,
        DevMode = 9,
        Status = 10,
        StatusString = 11,
        SecurityDescriptor = 12,
        Document = 13,
        Priority = 14,
        Position = 15,
        Submitted = 16,
        StartTime = 17,
        UntilTime = 18,
        Time = 19,
        TotalPages = 20,
        PagesPrinted = 21,
        TotalBytes = 22,
        BytesPrinted = 23
    }

    [StructLayout(LayoutKind.Sequential)]
    public class PrinterNotifyOptions
    {
        public int dwVersion = 2;
        public int dwFlags;
        public int Count = 2;
        public IntPtr lpTypes;

        public PrinterNotifyOptions(bool refresh = false)
        {
            dwFlags = refresh ? 1 : 0;
            const int bytesNeeded = (2 + PrinterNotifyOptionsType.JobFieldsCount + PrinterNotifyOptionsType.PrinterFieldsCount) * 2;
            var pJobTypes = new PrinterNotifyOptionsType();
            lpTypes = Marshal.AllocHGlobal(bytesNeeded);
            Marshal.StructureToPtr(pJobTypes, lpTypes, true);
        }
    }

    public enum NotificationTypeEnum
    {
        Printer = 0,
        Job = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    public class PrinterNotifyOptionsType
    {
        public const int JobFieldsCount = 24;
        public const int PrinterFieldsCount = 23;
        public short wJobType;
        public short wJobReserved0;
        public int dwJobReserved1;
        public int dwJobReserved2;
        public int JobFieldCount;
        public IntPtr pJobFields;
        public short wPrinterType;
        public short wPrinterReserved0;
        public int dwPrinterReserved1;
        public int dwPrinterReserved2;
        public int PrinterFieldCount;
        public IntPtr pPrinterFields;

        public PrinterNotifyOptionsType()
        {
            wJobType = (short) NotificationTypeEnum.Job;
            wPrinterType = (short) NotificationTypeEnum.Printer;
            SetupFields();
        }

        private void SetupFields()
        {
            if (pJobFields.ToInt32() != 0)
            {
                Marshal.FreeHGlobal(pJobFields);
            }

            if (wJobType == (short) NotificationTypeEnum.Job)
            {
                JobFieldCount = JobFieldsCount;
                pJobFields = Marshal.AllocHGlobal((JobFieldsCount * 2) - 1);

                Marshal.WriteInt16(pJobFields, 0, (short) JobNotifyTypeEnum.PrinterName);
                Marshal.WriteInt16(pJobFields, 2, (short) JobNotifyTypeEnum.MachineName);
                Marshal.WriteInt16(pJobFields, 4, (short) JobNotifyTypeEnum.PortName);
                Marshal.WriteInt16(pJobFields, 6, (short) JobNotifyTypeEnum.UserName);
                Marshal.WriteInt16(pJobFields, 8, (short) JobNotifyTypeEnum.NotifyName);
                Marshal.WriteInt16(pJobFields, 10, (short) JobNotifyTypeEnum.DataType);
                Marshal.WriteInt16(pJobFields, 12, (short) JobNotifyTypeEnum.PrintProcessor);
                Marshal.WriteInt16(pJobFields, 14, (short) JobNotifyTypeEnum.Paramters);
                Marshal.WriteInt16(pJobFields, 16, (short) JobNotifyTypeEnum.DriverName);
                Marshal.WriteInt16(pJobFields, 18, (short) JobNotifyTypeEnum.DevMode);
                Marshal.WriteInt16(pJobFields, 20, (short) JobNotifyTypeEnum.Status);
                Marshal.WriteInt16(pJobFields, 22, (short) JobNotifyTypeEnum.StatusString);
                Marshal.WriteInt16(pJobFields, 24, (short) JobNotifyTypeEnum.SecurityDescriptor);
                Marshal.WriteInt16(pJobFields, 26, (short) JobNotifyTypeEnum.Document);
                Marshal.WriteInt16(pJobFields, 28, (short) JobNotifyTypeEnum.Priority);
                Marshal.WriteInt16(pJobFields, 30, (short) JobNotifyTypeEnum.Position);
                Marshal.WriteInt16(pJobFields, 32, (short) JobNotifyTypeEnum.Submitted);
                Marshal.WriteInt16(pJobFields, 34, (short) JobNotifyTypeEnum.StartTime);
                Marshal.WriteInt16(pJobFields, 36, (short) JobNotifyTypeEnum.UntilTime);
                Marshal.WriteInt16(pJobFields, 38, (short) JobNotifyTypeEnum.Time);
                Marshal.WriteInt16(pJobFields, 40, (short) JobNotifyTypeEnum.TotalPages);
                Marshal.WriteInt16(pJobFields, 42, (short) JobNotifyTypeEnum.PagesPrinted);
                Marshal.WriteInt16(pJobFields, 44, (short) JobNotifyTypeEnum.TotalBytes);
                Marshal.WriteInt16(pJobFields, 46, (short) JobNotifyTypeEnum.BytesPrinted);
            }

            if (pPrinterFields.ToInt32() != 0)
            {
                Marshal.FreeHGlobal(pPrinterFields);
            }

            if (wPrinterType == (short) NotificationTypeEnum.Printer)
            {
                PrinterFieldCount = PrinterFieldsCount;
                pPrinterFields = Marshal.AllocHGlobal((PrinterFieldsCount - 1) * 2);

                Marshal.WriteInt16(pPrinterFields, 0, (short) PrinterNotifyTypeEnum.ServerName);
                Marshal.WriteInt16(pPrinterFields, 2, (short) PrinterNotifyTypeEnum.PrinterName);
                Marshal.WriteInt16(pPrinterFields, 4, (short) PrinterNotifyTypeEnum.ShareName);
                Marshal.WriteInt16(pPrinterFields, 6, (short) PrinterNotifyTypeEnum.PortName);
                Marshal.WriteInt16(pPrinterFields, 8, (short) PrinterNotifyTypeEnum.DriverName);
                Marshal.WriteInt16(pPrinterFields, 10, (short) PrinterNotifyTypeEnum.Comment);
                Marshal.WriteInt16(pPrinterFields, 12, (short) PrinterNotifyTypeEnum.Location);
                Marshal.WriteInt16(pPrinterFields, 14, (short) PrinterNotifyTypeEnum.SepFile);
                Marshal.WriteInt16(pPrinterFields, 16, (short) PrinterNotifyTypeEnum.PrintProcessor);
                Marshal.WriteInt16(pPrinterFields, 18, (short) PrinterNotifyTypeEnum.Parameters);
                Marshal.WriteInt16(pPrinterFields, 20, (short) PrinterNotifyTypeEnum.DataType);
                Marshal.WriteInt16(pPrinterFields, 22, (short) PrinterNotifyTypeEnum.Attributes);
                Marshal.WriteInt16(pPrinterFields, 24, (short) PrinterNotifyTypeEnum.Priority);
                Marshal.WriteInt16(pPrinterFields, 26, (short) PrinterNotifyTypeEnum.DefaultPriority);
                Marshal.WriteInt16(pPrinterFields, 28, (short) PrinterNotifyTypeEnum.StartTime);
                Marshal.WriteInt16(pPrinterFields, 30, (short) PrinterNotifyTypeEnum.UntilTime);
                Marshal.WriteInt16(pPrinterFields, 32, (short) PrinterNotifyTypeEnum.StatusString);
                Marshal.WriteInt16(pPrinterFields, 34, (short) PrinterNotifyTypeEnum.CJobs);
                Marshal.WriteInt16(pPrinterFields, 36, (short) PrinterNotifyTypeEnum.AveragePagesPerMin);
                Marshal.WriteInt16(pPrinterFields, 38, (short) PrinterNotifyTypeEnum.TotalPages);
                Marshal.WriteInt16(pPrinterFields, 40, (short) PrinterNotifyTypeEnum.PagesPrinted);
                Marshal.WriteInt16(pPrinterFields, 42, (short) PrinterNotifyTypeEnum.TotalBytes);
                Marshal.WriteInt16(pPrinterFields, 44, (short) PrinterNotifyTypeEnum.BytesPrinted);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PrinterNotifyInfo
    {
        public uint Version;
        public uint Flags;
        public uint Count;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PrinterNotifyInfoData2
    {
        public uint cbBuf;
        public IntPtr pBuf;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PrinterNotifyInfoDataUnion
    {
        [FieldOffset(0)]
        private readonly uint adwData0;

        [FieldOffset(4)]
        private readonly uint adwData1;

        [FieldOffset(0)]
        public PrinterNotifyInfoData2 Data;

        public uint[] adwData
        {
            get { return new[] {adwData0, adwData1}; }
        }
    }

    // Structure borrowed from http://lifeandtimesofadeveloper.blogspot.com/2007/10/unmanaged-structures-padding-and-c-part_18.html.
    [StructLayout(LayoutKind.Sequential)]
    public struct PrinterNotifyInfoData
    {
        public ushort Type;
        public ushort Field;
        public uint Reserved;
        public uint Id;
        public PrinterNotifyInfoDataUnion NotifyData;
    }
}