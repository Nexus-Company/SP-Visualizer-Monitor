using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Sicoob.Visualizer.Monitor.Service
{
    public partial class UpdateViwersService : ServiceBase
    {
        public Settings Settings { get; set; }
        public UpdateViwersService(Settings settings)
        {
            Settings = settings;
            InitializeComponent();
            InitializeGraph();
        }

        protected override void OnStart(string[] args)
        {
            while (true)
            {

            }
        }

        protected override void OnStop()
        {
        }

        void InitializeGraph()
        {
            GraphHelper.InitializeGraphForUserAuth(Settings, (info, cancel) =>
            {
                // Display the device code message to
                // the user. This tells them
                // where to go to sign in and provides the
                // code to use.
                Console.WriteLine(info.Message);
                return Task.FromResult(0);
            });
        }
    }
}
