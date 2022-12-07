using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

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
