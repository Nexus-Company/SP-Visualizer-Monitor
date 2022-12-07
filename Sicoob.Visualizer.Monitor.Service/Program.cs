using System;
using System.Collections.Generic;
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
            ServiceBase.Run(new ServiceBase[]
            {
                new UpdateViwersService(Settings.LoadSettings())
            });
        }
    }
}
