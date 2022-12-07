using Microsoft.Toolkit.Uwp.Notifications;
using Sicoob.Visualizer.Monitor.Dal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Sicoob.Visualizer.Monitor.Service
{
    public partial class UpdateViwersService : ServiceBase
    {
        public Settings Settings { get; set; }
        private Thread ThreadService { get; set; }
        private MonitorContext ctx;
        public UpdateViwersService(Settings settings)
        {
            InitializeComponent();
            Settings = settings;
            ctx = new MonitorContext();
            ThreadService = new Thread(()
                => ServiceAsync().Wait());
        }

        public void Start()
        {
            InitializeGraph();
            ThreadService.Start();
        }
        protected override void OnStart(string[] args)
            => Start();

        protected override void OnStop()
        {
            //threadAtiva = false;
            //    threadControle.Join();
        }

        async Task ServiceAsync()
        {
            while (true)
            {
                try
                {
                    var drive = (await GraphHelper.GetDrivesAsync()).First();
                    //var strReader = new StreamReader(await GraphHelper.GetReportsAsync());
                    //var text = strReader.ReadToEnd();

                    notifyNewRelatorio();
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void notifyNewRelatorio()
        {
            var imageUri = Path.GetFullPath(@"Resources\relatorio.png");

            new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 9813)
                        .AddText("Novo relatório disponível")
                        .AddText("Foi gerado um novo relatório de atividade dos arquivos no Sharepoint, clique no botão abaixo para acessa-lo.")
                        //.AddInlineImage(new Uri(imageUri))
                        .AddButton(new ToastButton()
                            .SetContent("Abrir relatório")
                            .SetProtocolActivation(new Uri("https://localhost:80"))
                         )
                        .Show();
        }
        void InitializeGraph()
        {
            GraphHelper.InitializeGraphForUserAuthAsync(Settings, (info, cancel) =>
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
