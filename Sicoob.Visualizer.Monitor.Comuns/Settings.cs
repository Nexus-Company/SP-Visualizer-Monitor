using Microsoft.Extensions.Configuration;
using Sicoob.Visualizer.Monitor.Comuns.Database;
using System;
using System.Diagnostics;

namespace Sicoob.Visualizer.Monitor.Comuns
{
    public class Settings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string AuthTenant { get; set; }
        public string RedirectUrl { get; set; }
        public string[] GraphUserScopes { get; set; }

        private string _conn;
        public MonitorContext GetContext()
            => new MonitorContext(_conn);

        public static Settings LoadSettings()
        {
            // Load settings
            IConfiguration config = new ConfigurationBuilder()
                // appsettings.json is required
                .AddJsonFile("appsettings.json", optional: false)
                // appsettings.Development.json" is optional, values override appsettings.json
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();

            var settings = config.GetRequiredSection("Settings").Get<Settings>();

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
    }
}
