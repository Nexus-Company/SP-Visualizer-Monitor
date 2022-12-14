using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sicoob.Visualizer.Monitor.Dal;
using System;
using System.Diagnostics;
using System.IO;

namespace Sicoob.Visualizer.Monitor.Comuns
{
    public class Settings
    {
        private static string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        private static string settingsDevelopPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.Development.json");
        public OAuthSettings OAuth { get; set; }

        public bool Notifications
        {
            get => (bool)JObject.Parse(File.ReadAllText(settingsPath))["Notifications"];
            set
            {
                JObject obj = JObject.Parse(File.ReadAllText(settingsPath));

                if ((bool)obj["Notifications"] != value)
                {
                    obj.Remove("Notifications");
                    obj["Notifications"] = value;

                    File.WriteAllText(settingsPath, obj.ToString());
                }
            }
        }
        private string _conn;
        public MonitorContext GetContext()
        {
            var ctx = new MonitorContext(_conn);

            if (ctx.Database.GetPendingMigrations().Count() > 1)
                ctx.Database.Migrate();

            return ctx;
        }

        public static Settings LoadSettings()
        {
            // Load settings
            IConfiguration config = new ConfigurationBuilder()
                // appsettings.json is required
                .AddJsonStream(new MemoryStream(File.ReadAllBytes(settingsPath)))
                .AddJsonFile(settingsDevelopPath, true)
                .Build();

            var settings = config.Get<Settings>();

            settings._conn = config.GetConnectionString("SqlServer");
            return settings;
        }

        public static Hourly[] GetSchedules()
        {
            // Load settings
            IConfiguration config = new ConfigurationBuilder()
                // appsettings.json is required
                .AddJsonFile("appsettings.json", optional: false)
                // appsettings.Development.json" is optional, values override appsettings.json
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();

            Hourly[] hours = config.GetRequiredSection("Schedules").Get<Hourly[]>();

            if (Debugger.IsAttached)
            {
                var temp = new Hourly[hours.Length + 1];

                for (int i = 0; i < hours.Length; i++)
                {
                    temp[i] = hours[i];
                }

                DateTime now = DateTime.Now;
                temp[temp.Length - 1] = new Hourly(new TimeSpan(now.Hour, now.Minute, now.Second), new DayOfWeek[] { now.DayOfWeek });

                return temp;
            }

            return hours;
        }

        public class Hourly
        {
            public TimeSpan Time { get; set; }
            public DayOfWeek[] DaysOfWeek { get; set; }

            public Hourly() { }

            public Hourly(TimeSpan time, DayOfWeek[] daysOfWeek)
            {
                Time = time;
                DaysOfWeek = daysOfWeek ?? throw new ArgumentNullException(nameof(daysOfWeek));
            }
        }

        public class OAuthSettings
        {
            public string ClientId { get; set; }
            public string RedirectUrl { get; set; }
            public string[] GraphUserScopes { get; set; }
            public string[] SharepointUserScopes { get; set; }
        }
    }
}
