using Sicoob.Visualizer.Monitor.Comuns;
using System.Diagnostics;
using System.ServiceProcess;

namespace Sicoob.Visualizer.Monitor.Service
{
    internal static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        static void Main()
        {
            UpdateViwersService service = new UpdateViwersService(Settings.LoadSettings());

            if (Debugger.IsAttached)
            {
                service.Start();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[]
                {
                    service
                });
            }
        }
    }
}
