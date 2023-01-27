using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Toolkit.Uwp.Notifications;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Comuns.Helpers;
using Sicoob.Visualizer.Monitor.Dal;
using Sicoob.Visualizer.Monitor.Dal.Models;
using System.Data;
using System.Diagnostics;
using System.Security.Policy;
using static Sicoob.Visualizer.Monitor.Comuns.Settings;
using Activity = Sicoob.Visualizer.Monitor.Dal.Models.Activity;
using ActivityType = Sicoob.Visualizer.Monitor.Dal.Models.Enums.ActivityType;
using DayOfWeek = System.DayOfWeek;

namespace Sicoob.Visualizer.Monitor.Worker;
public class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger<WindowsBackgroundService> _logger;
    public LastHourly LastUpdateHourly { get; set; }
    public bool Stopped { get; private set; }

    public WindowsBackgroundService(ILogger<WindowsBackgroundService> logger)
    {
        _logger = logger;
        LastUpdateHourly = new LastHourly();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Log(LogLevel.Information, "Service start Success!");

        Thread thComponents = new(() => UpdateComponentsAsync().Wait());
        Thread thAcitvities = new(() => UpdateActivitiesAsync().Wait());

        thComponents.Start();
        thAcitvities.Start();

        while (!Stopped)
        {

        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Stopped = false;
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Stopped = true;
        return base.StopAsync(cancellationToken);
    }

    private async Task UpdateComponentsAsync()
    {
        while (!Stopped)
        {
            var timeTables = GetSchedules();
            Settings settings = LoadSettings();
            GraphHelper helper = new(settings.OAuth, settings.GetContext());

            try
            {
                await helper.GetLoginAsync();

                using (Updater updater = new(helper, settings.GetContext()))
                {
                    await updater.UpdateAccountsAsync();
                    _logger.Log(LogLevel.Information, "Update accounts Success!");

                    await updater.UpdateSitesAsync();
                    _logger.Log(LogLevel.Information, "Update Sites Success!");

                    await updater.UpdateListsAsync();
                    _logger.Log(LogLevel.Information, "Update Lists Success!");

                    await updater.UpdateItemsAsync();
                    _logger.Log(LogLevel.Information, "Update Items Success!");
                }

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

#if !DEBUG
                Thread.Sleep(150000);
#endif
        }
    }
    private async Task UpdateActivitiesAsync()
    {
        while (!Stopped)
        {
            Settings settings = LoadSettings();
            GraphHelper helper = new(settings.OAuth, settings.GetContext());
            MonitorContext ctx = settings.GetContext();
            var lists = await ctx.Lists.ToArrayAsync();

            foreach (var list in lists)
            {
                if (Stopped)
                    break;

                var items = await (from it in ctx.Items
                                   where it.ListId == list.Id || it.Folder.ListId == list.Id
                                   select it).ToArrayAsync();

                await helper.GetLoginAsync();

                foreach (var item in items)
                {
                    //if (item.ContentType.Name == "Folder")
                    //    continue;

                    try
                    {
                        if (item.MimeType.Contains("image/"))
                            continue;

                        string eTag = item.Etag.Split(",")[0];
                        Stopwatch stopwatch = new();

                        FileActivity[] editActivity = await helper.GetActivityAsync(list.DriveId, eTag, ActivityType.Edit);
                        FileActivity[] accessActivity = await helper.GetActivityAsync(list.DriveId, eTag, ActivityType.Access);

                        FileActivity[] activities = new FileActivity[editActivity.Length + accessActivity.Length];
                        editActivity.CopyTo(activities, 0);
                        accessActivity.CopyTo(activities, editActivity.Length);

                        stopwatch.Stop();

                        foreach (var activity in activities)
                        {
                            Activity? act = await (from actv in ctx.Activities
                                                   where actv.Target == item.Id &&
                                                         actv.Type == activity.Type &&
                                                         actv.Date == activity.ActivityDateTime
                                                   select actv).FirstOrDefaultAsync();
                            if (act == null)
                            {
                                act = new()
                                {
                                    Date = activity.ActivityDateTime,
                                    User = activity.Actor.User.Id,
                                    Type = activity.Type,
                                    Target = item.Id,
                                    Inserted = DateTime.Now
                                };

                                await ctx.Activities.AddAsync(act);
                            }
                        }

                        await ctx.SaveChangesAsync();

                        _logger.Log(LogLevel.Debug, $"Get activities for item: {item.Id} in {stopwatch.Elapsed}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, ex, null);
                    }
                }
            }
        }
    }


    private void notifyNewRelatorio()
    {
        //    try
        //    {
        //        var imageUri = Path.GetFullPath(@"Assets\Image\relatorio.png");

        //        if (Settings.Notifications)
        //            new ToastContentBuilder()
        //                    .AddArgument("action", "viewConversation")
        //                    .AddArgument("conversationId", 9813)
        //                    .AddText("Novo relatório disponível")
        //                    .AddText("Foi gerado um novo relatório de atividade dos arquivos no Sharepoint, clique no botão abaixo para acessa-lo.")
        //                    //.AddInlineImage(new Uri(imageUri))
        //                    .AddButton(new ToastButton()
        //                        .SetContent("Abrir relatório")
        //                        .SetProtocolActivation(new Uri("https://localhost:80"))
        //                     );
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
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