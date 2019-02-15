#region

using System.Collections.Generic;
using JsonConfig;

#endregion

namespace PrintMonitorRelay.Settings
{
    public static class AppSettings
    {
        public static List<PrinterSetting> PrinterSettings { get; set; }

        private enum RelayStateEnum
        {
            Off = 0,
            On = 1,
            Toggle = 2
        }

        private const string Relay = "state.xml?relayState=";

        public static void LoadSettings()
        {
            PrinterSettings = new List<PrinterSetting>();
            foreach (var appSetting in Config.Global.PrinterSettings)
            {
                PrinterSettings.Add(new PrinterSetting {PrinterName = appSetting.PrinterName, Duration = appSetting.Duration, IpOn = AddOn(appSetting.Ip), IpOff = AddOff(appSetting.Ip)});
            }
        }

        private static string AddOn(string ip)
        {
            return AddRelay(ip) + RelayStateEnum.On;
        }

        private static string AddOff(string ip)
        {
            return AddRelay(ip) + RelayStateEnum.Off;
        }

        private static string AddRelay(string ip)
        {
            if (!ip.EndsWith("/"))
            {
                ip += "/";
            }

            return ip + Relay;
        }

        public class PrinterSetting
        {
            public string PrinterName { get; set; }
            public int Duration { get; set; }
            public string IpOn { get; set; }
            public string IpOff { get; set; }
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
}