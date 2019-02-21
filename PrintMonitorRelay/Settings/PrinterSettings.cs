#region

using System.Collections.Generic;
using JsonConfig;

#endregion

namespace PrintMonitorRelay.Settings
{
    public static class AppSettings
    {
        public static List<PrinterSetting> PrinterSettings { get; set; }
        private const string PulseStandard = "state.xml?relayState=2";
        private const string PulseOveride = "state.xml?relayState=2&pulseTime=";

        public static void LoadSettings()
        {
            PrinterSettings = new List<PrinterSetting>();
            foreach (var appSetting in Config.Global.PrinterSettings)
            {
                PrinterSettings.Add(new PrinterSetting {PrinterName = appSetting.PrinterName, Duration = appSetting.Duration, Ip = AddPulse(appSetting.Ip, appSetting.Duration)});
            }
        }

        private static string AddPulse(string ip, int duration)
        {
            var pulse = (duration == 0) ? PulseStandard : PulseOveride + duration;
            return AddSlash(ip) + pulse;
        }

        private static string AddSlash(string ip)
        {
            if (!ip.EndsWith("/"))
                ip += "/";
            return ip;
        }
    }

    public class PrinterSetting
    {
        public string PrinterName { get; set; }
        public int Duration { get; set; }
        public string Ip { get; set; }
    }

    //public class JsonModel
    //{
    //    public List<JsonPrinterSetting> PrinterSettings { get; set; } = new List<JsonPrinterSetting>();
    //}

    //public class JsonPrinterSetting
    //{
    //    public string PrinterName { get; set; }
    //    public int Duration { get; set; }
    //    public string Ip { get; set; }
    //}
}