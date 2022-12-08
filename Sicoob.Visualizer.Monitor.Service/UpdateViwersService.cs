using Microsoft.Toolkit.Uwp.Notifications;
using Sicoob.Visualizer.Monitor.Comuns;
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
using static Sicoob.Visualizer.Monitor.Comuns.Settings;

namespace Sicoob.Visualizer.Monitor.Service
{
    public partial class UpdateViwersService : ServiceBase
    {
        public Settings Settings { get; set; }
        public LastHourly LastUpdateHourly { get; set; }
        private Thread ThreadService { get; set; }
        public UpdateViwersService(Settings settings)
        {
            InitializeComponent();
            Settings = settings;
            LastUpdateHourly = new LastHourly();
            ThreadService = new Thread(() => ServiceAsync().Wait());
        }

        public void Start()
        {
            GraphHelper.InitializeGraphForUserAuthAsync(Settings);
            GraphHelper.GetLoginAsync()
                .Wait();
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
                var timeTables = GetSchedules();

                if (!CheckHourly(timeTables, out Hourly actualHourly))
                    continue;

                try
                {
                    eventLog.WriteEntry("Get Drive");
                    var drive = (await GraphHelper.GetDrivesAsync()).First();
                    //var strReader = new StreamReader(await GraphHelper.GetReportsAsync());
                    //var text = strReader.ReadToEnd();
                    notifyNewRelatorio();
                    eventLog.WriteEntry("Started ok");
                }
                catch (Exception ex)
                {
                    eventLog.WriteEntry(ex.Message);
                }

                LastUpdateHourly = new LastHourly(actualHourly);
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

        private bool CheckHourly(Hourly[] timeTables, out Hourly actual)
        {
            var last = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            foreach (var item in timeTables)
            {
                if (item.Time > LastUpdateHourly.Time &&
                    last > item.Time &&
                    item.DaysOfWeek.Contains(DateTime.Now.DayOfWeek) &&
                    item.DaysOfWeek.Select(dayOfWeek => dayOfWeek >= LastUpdateHourly.DayOfWeek).Contains(true))
                {
                    actual = item;
                    return true;
                }
            }

            actual = null;
            return false;
        }

        public class LastHourly
        {
            public TimeSpan Time { get; set; }
            public DayOfWeek DayOfWeek { get; set; }

            public LastHourly()
            {
                Time = new TimeSpan(0, 0, 0);
                DayOfWeek = DayOfWeek.Monday;
            }

            public LastHourly(Hourly hourly)
            {
                Time = hourly.Time;
                DayOfWeek = hourly.DaysOfWeek.First(fs => fs >= DateTime.Now.DayOfWeek);
            }
        }
    }
}
