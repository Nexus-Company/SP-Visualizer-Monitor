using MahApps.Metro.IconPacks;
using Microsoft.EntityFrameworkCore;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Dal;
using Sicoob.Visualizer.Monitor.Dal.Models;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using Activity = Sicoob.Visualizer.Monitor.Dal.Models.Activity;

namespace Sicoob.Visualizer.Monitor.Wpf
{
    /// <summary>
    /// Lógica interna para MainScreen.xaml
    /// </summary>
    public partial class MainScreen : Window
    {
        public Settings AppSettings { get; set; }
        public MonitorContext MonitorContext { get; set; }
        public Account Account { get; set; }
        public MainScreen(Account account)
        {
            InitializeComponent();
            setAccount(account);
            AppSettings = Settings.LoadSettings();
            MonitorContext = AppSettings.GetContext();
            UpdateBell();
            GetRecents();
        }

        private async void GetRecents()
        {
            var activities = await (from act in MonitorContext.Activities
                                    orderby act.Date descending
                                    select act)
                                    .Include(act => act.Account)
                                    .Include(act => act.Item)
                                    .Take(50)
                                    .ToArrayAsync();

            foreach (var item in activities)
                AddActivityRow(item);
        }

        private async void Logout_Click(object sender, RoutedEventArgs args)
        {
            ServiceController? service = ServiceController
               .GetServices()
               .FirstOrDefault(service => service.DisplayName == SplashScreen.monitorServiceName);

            if (service?.Status == ServiceControllerStatus.Running)
                service?.Stop();

            _ = await MonitorContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [Authentications]");

            SplashScreen splash = new();
            Close();
            splash.Show();
        }
        private void Notifications_Change(object sender, EventArgs args)
        {
            AppSettings.Notifications = !AppSettings.Notifications;

            UpdateBell();
        }
        private void Exit_Click(object sender, RoutedEventArgs args)
        {
            MonitorContext.Dispose();
            Close();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void setAccount(Account account)
        {
            Account = account;
            string[] names = account.Name.Trim().Split(' ');

            userEmail.Text = account.Email;
            userName.Text = account.Name;
            userInitial.Text = names[0][0].ToString().ToUpperInvariant();
            userColor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(account.Color));
        }
        private void AddActivityRow(Activity act)
        {
            string[] names = act.Account.Name.Split(' ');
            viwers.Items.Add(new
            {
                BgColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(act.Account.Color)),
                Character = $"{names[0][0]}{names[1][0]}".ToUpperInvariant(),
                IconKind = KindByActivityType(act.Type),
                DisplayName = act.Account.Name,
                ItemName = act.Item.Name,
                ItemWebUrl = act.Item.WebUrl,
                act.Date,
                act.Account.Email
            });
        }

        private PackIconMaterialKind KindByActivityType(ActivityType type)
            => type switch
            {
                ActivityType.Access => PackIconMaterialKind.Eye,
                ActivityType.Edit => PackIconMaterialKind.Pencil,
                _ => PackIconMaterialKind.None
            };
        private void UpdateBell()
        {
            if (AppSettings.Notifications)
                BellNotifications.Kind = PackIconMaterialKind.Bell;
            else
                BellNotifications.Kind = PackIconMaterialKind.BellOff;
        }
    }
}
