using MahApps.Metro.IconPacks;
using Microsoft.EntityFrameworkCore;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Comuns.Database;
using Sicoob.Visualizer.Monitor.Comuns.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sicoob.Visualizer.Monitor.Wpf
{
    /// <summary>
    /// Lógica interna para MainScreen.xaml
    /// </summary>
    public partial class MainScreen : Window
    {
        public Settings AppSettings { get; set; }
        public MonitorContext MonitorContext { get; set; }
        public MainScreen()
        {
            InitializeComponent();
            AppSettings = Settings.LoadSettings();
            MonitorContext = AppSettings.GetContext();
            UpdateBell();
        }

        private async void Logout_Click(object sender, RoutedEventArgs args)
        {
            _ = await MonitorContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [GraphAuthentications]");
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

        private void UpdateBell()
        {
            if (AppSettings.Notifications)
                BellNotifications.Kind = PackIconMaterialKind.Bell;
            else
                BellNotifications.Kind = PackIconMaterialKind.BellOff;
        }
    }
}
