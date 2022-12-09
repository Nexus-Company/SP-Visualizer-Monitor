using Microsoft.Toolkit.Uwp.Notifications;
using Sicoob.Visualizer.Monitor.Comuns;
using System.Data;
using static Sicoob.Visualizer.Monitor.Comuns.Settings;

namespace Sicoob.Visualizer.Monitor.Worker
{
    public class WindowsBackgroundService : BackgroundService
    {
        private readonly ILogger<WindowsBackgroundService> _logger;
        public Settings Settings { get; set; }
        public LastHourly LastUpdateHourly { get; set; }
        public GraphHelper Helper { get; set; }

        public WindowsBackgroundService(ILogger<WindowsBackgroundService> logger)
        {
            _logger = logger;
            LastUpdateHourly = new LastHourly();
            Settings = LoadSettings();
            Helper = new(Settings.OAuth, Settings.GetContext());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Log(LogLevel.Information, "Service start Success!");

            if (Settings.Notifications)
                new ToastContentBuilder()
                      .AddArgument("action", "viewConversation")
                      .AddArgument("conversationId", 9813)
                      .AddText("Monitoramento")
                      .AddText("O monitoramenteo do Sharepoint foi iniciado.");

            while (true)
            {
                var timeTables = GetSchedules();

                if (!CheckHourly(timeTables, out Hourly? actualHourly))
                    continue;

                try
                {

                    //var drive = (await Helper.GetDrivesAsync()).First();
                    //var strReader = new StreamReader(await GraphHelper.GetReportsAsync());
                    //var text = strReader.ReadToEnd();
                    notifyNewRelatorio();
                }
                catch (Microsoft.Graph.ServiceException ex)
                {

                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex.Message);
                }

                LastUpdateHourly = new LastHourly(actualHourly);
            }
        }

        private void notifyNewRelatorio()
        {
            var imageUri = Path.GetFullPath(@"Assets\Image\relatorio.png");

            if (Settings.Notifications)
                new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 9813)
                        .AddText("Novo relatório disponível")
                        .AddText("Foi gerado um novo relatório de atividade dos arquivos no Sharepoint, clique no botão abaixo para acessa-lo.")
                        //.AddInlineImage(new Uri(imageUri))
                        .AddButton(new ToastButton()
                            .SetContent("Abrir relatório")
                            .SetProtocolActivation(new Uri("https://localhost:80"))
                         );


        }

        private bool CheckHourly(Hourly[] timeTables, out Hourly? actual)
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

            public LastHourly(Hourly? hourly)
            {
                if (hourly == null)
                {
                    Time = new TimeSpan(0, 0, 0);
                    DayOfWeek = DayOfWeek.Monday;

                    return;
                }

                Time = hourly.Time;
                DayOfWeek = hourly.DaysOfWeek.First(fs => fs >= DateTime.Now.DayOfWeek);
            }
        }
    }
}