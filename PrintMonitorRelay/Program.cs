#region

using System.ServiceProcess;

#endregion

namespace PrintMonitorRelay
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new PrintMonitorRelay()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}