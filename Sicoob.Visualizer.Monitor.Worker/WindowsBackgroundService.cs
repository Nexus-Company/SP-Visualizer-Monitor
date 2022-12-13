using Microsoft.Toolkit.Uwp.Notifications;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Data;
using static Sicoob.Visualizer.Monitor.Comuns.GraphHelper;
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
                    await Helper.GetLoginAsync();

                    await UpdateUsersAsync();

                    await UpdateActivitiesAsync();

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

        private async Task UpdateUsersAsync()
        {
            var users = await Helper.GetUsersAsync();

            while (users.Count > 1)
            {
                foreach (var user in users)
                    await Helper.UpdateOrAppendUserAsync(user);

                users.Clear();

                if (users.NextPageRequest != null)
                    users = await users.NextPageRequest.GetAsync();
            }

            _logger.Log(LogLevel.Information, "Update accounts Success!");
        }

        private async Task UpdateActivitiesAsync()
        {
            var driver = (await Helper.GetDrivesAsync()).First();
            var driverId = new Uri(driver.WebUrl).Host;

            var lists = await Helper.GetListsAsync(driverId);

            foreach (var list in lists)
            {
                var items = await Helper.GetItemsAsync(driverId, list.Id);
                bool @continue = false;

                do
                {
                    foreach (var item in items)
                    {
                        if (item.ContentType.Name == "Folder")
                            continue;

                        try
                        {
                            string itemTag = item.ETag.Split(',').First().Replace("\"", string.Empty);
                            FileActivity[] editActivity = await Helper.GetActivityAsync(driver.Id, itemTag, ActivityType.Edit);
                            FileActivity[] accessActivity = await Helper.GetActivityAsync(driver.Id, itemTag, ActivityType.Access);

                            FileActivity[] activities = new FileActivity[editActivity.Length + accessActivity.Length];
                            editActivity.CopyTo(activities, 0);
                            accessActivity.CopyTo(activities, editActivity.Length);

                            var it = await Helper.GetItemAsync(driver.Id, itemTag);
                            await Helper.UpdateActivitiesAsync(it, activities);
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    if (items.NextPageRequest != null)
                    {
                        items = await items.NextPageRequest.GetAsync();
                        @continue = true;
                    }
                } while (@continue);
            }

            _logger.Log(LogLevel.Information, "Update items access Success!");
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