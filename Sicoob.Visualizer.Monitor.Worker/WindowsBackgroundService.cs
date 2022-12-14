using Microsoft.Graph;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualBasic;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Data;
using System.Diagnostics;
using static Sicoob.Visualizer.Monitor.Comuns.GraphHelper;
using static Sicoob.Visualizer.Monitor.Comuns.Settings;
using ActivityType = Sicoob.Visualizer.Monitor.Dal.Models.Enums.ActivityType;
using DayOfWeek = System.DayOfWeek;
using TaskStatus = System.Threading.Tasks.TaskStatus;

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

            try
            {
                if (Settings.Notifications)
                    new ToastContentBuilder()
                          .AddArgument("action", "viewConversation")
                          .AddArgument("conversationId", 9813)
                          .AddText("Monitoramento")
                          .AddText("O monitoramenteo do Sharepoint foi iniciado.")
                          .Show();
            }
            catch (Exception ex)
            {
                _logger.Log(logLevel: LogLevel.Error, exception: ex, null);
            }

            while (true)
            {
                var timeTables = GetSchedules();

                try
                {
                    await Helper.GetLoginAsync();

                    _logger.Log(LogLevel.Information, "get authentication success!");

                    await UpdateUsersAsync();

                    _logger.Log(LogLevel.Information, "Update accounts Success!");

                    await UpdateActivitiesAsync();

                    if (!CheckHourly(timeTables, out Hourly? actualHourly))
                        continue;

                    notifyNewRelatorio();

                    LastUpdateHourly = new LastHourly(actualHourly);
                }
                catch (ServiceException ex)
                {
                    _logger.Log(LogLevel.Error, ex, null);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex, null);
                }

                Thread.Sleep(50000);
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
        }

        private async Task UpdateActivitiesAsync()
        {
            try
            {
                var drivers = await Helper.GetDrivesAsync();
                var driver = drivers.First();
                var driverId = new Uri(driver.WebUrl).Host;
                var lists = await Helper.GetListsAsync(driverId);

                foreach (var list in lists)
                {
                    List<Task> tasks = new();
                    var items = await Helper.GetItemsAsync(driverId, list.Id);

                    tasks.Add(new Task((obj) => StartUpdatePage(obj).Wait(), new StartUpdate(items, driver, Settings)));

                    if (tasks[^1].Status == TaskStatus.Created)
                        tasks[^1].Start();

                    var lastItems = items.NextPageRequest;

                    while (lastItems != null)
                    {
                        var localItems = await lastItems.GetAsync();

                        tasks.Add(new Task((obj) => StartUpdatePage(obj).Wait(), new StartUpdate(localItems, driver, Settings)));

                        if (tasks[^1].Status == TaskStatus.Created)
                            tasks[^1].Start();

                        lastItems = localItems.NextPageRequest;

                        _logger.Log(LogLevel.Information, $"New Task created with id: {tasks[^1].Id}");
                    }

                    foreach (var item in tasks)
                    {
                        if (item.Status == TaskStatus.Running)
                        {
                            _logger.Log(LogLevel.Information, $"Task running with id: {item.Id}");
                            item.Wait(TimeSpan.FromMinutes(5));
                            _logger.Log(LogLevel.Information, $"Task finished with id: {item.Id}");
                        }
                    }
                }

                _logger.Log(LogLevel.Information, "Update items access Success!");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, null);
            }
        }
        private class StartUpdate
        {
            public IListItemsCollectionPage Items { get; set; }
            public Drive Driver { get; }
            public GraphHelper Helper { get; set; }
            public StartUpdate(IListItemsCollectionPage items, Drive driver, Settings settings)
            {
                Items = items;
                Driver = driver;
                Helper = new(settings.OAuth, settings.GetContext());
            }
        }

        private async Task StartUpdatePage(object? obj)
        {
            try
            {
                var strOpt = obj as StartUpdate;

                await strOpt.Helper.GetLoginAsync();

                lock (strOpt.Items)
                {
                    foreach (var item in strOpt.Items)
                    {
                        if (item.ContentType.Name == "Folder")
                            continue;

                        try
                        {
                            string itemTag = item.ETag.Split(',').First().Replace("\"", string.Empty);

                            var taskActEdt = strOpt.Helper.GetActivityAsync(strOpt.Driver.Id, itemTag, ActivityType.Edit);
                            var taskActAcc = strOpt.Helper.GetActivityAsync(strOpt.Driver.Id, itemTag, ActivityType.Access);

                            Stopwatch stopwatch = new();
                            stopwatch.Start();
                            taskActAcc.Wait();
                            taskActEdt.Wait();

                            FileActivity[] editActivity = taskActEdt.Result;
                            FileActivity[] accessActivity = taskActAcc.Result;

                            FileActivity[] activities = new FileActivity[editActivity.Length + accessActivity.Length];
                            editActivity.CopyTo(activities, 0);
                            accessActivity.CopyTo(activities, editActivity.Length);

                            stopwatch.Stop();

                            _logger.Log(LogLevel.Debug, $"Get activities for item: {item.Id} in {stopwatch.Elapsed}");
                            var taskItem = strOpt.Helper.GetItemAsync(strOpt.Driver.Id, itemTag);
                            taskItem.Wait();

                            var it = taskItem.Result;
                            strOpt.Helper.UpdateActivitiesAsync(it, activities).Wait();
                            _logger.Log(LogLevel.Debug, $"Inserted activities for item: {item.Id}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(LogLevel.Error, ex, null);
                        }
                        finally
                        {
                            Thread.Sleep(2000);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, null);
            }
        }

        private void notifyNewRelatorio()
        {
            try
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
                             ).Show();
            }
            catch (Exception)
            {

                throw;
            }
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