using MahApps.Metro.IconPacks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.ExternalConnectors;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Dal;
using Sicoob.Visualizer.Monitor.Dal.Models;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Diagnostics;
using System.Media;
using System.ServiceProcess;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Activity = Sicoob.Visualizer.Monitor.Dal.Models.Activity;
using Timer = System.Timers.Timer;

namespace Sicoob.Visualizer.Monitor.Wpf
{
    /// <summary>
    /// Lógica interna para MainScreen.xaml
    /// </summary>
    public partial class MainScreen : Window
    {
        private Timer _refreshTime;
        public Settings AppSettings { get; set; }
        public MonitorContext MonitorContext { get; set; }
        public Account Account { get; set; }
        public bool DateDescending { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; private set; }
        public MainScreen(Account account)
        {
            InitializeComponent();
            setAccount(account);
            AppSettings = Settings.LoadSettings();
            MonitorContext = AppSettings.GetContext();
            Page = 1;
            DateDescending = true;
            _refreshTime = new()
            {
                AutoReset = true,
                Interval = 1000,
                Enabled = true
            };
            _refreshTime.Elapsed += Refresh;
            _refreshTime.Start();
            UpdateBell();
        }

        private async void GetRecents(int page)
        {
            Page = page;

            var activitiesQuery = (from act in MonitorContext.Activities
                                   orderby act.Date
                                   select act)
                                    .Include(act => act.Account)
                                    .Include(act => act.Item)
                                    .OrderBy(act => act.Date);

            if (DateDescending)
                activitiesQuery = activitiesQuery.OrderByDescending(act => act.Date);

            var activities = await activitiesQuery
                                    .Skip((int)((page - 1) * ((double)AppSettings.PerPage)))
                                    .Take(AppSettings.PerPage)
                                    .ToArrayAsync();

            AddActivityRow(activities);

            int count = await MonitorContext.Activities.CountAsync();
            double pages = count / ((double)AppSettings.PerPage);

            TotalPages = pages >= ((int)pages) ? (int)pages + 1 : (int)pages;

            Application.Current.Dispatcher.Invoke(()
                => hiddenByPageNumber());
        }

        private void hiddenByPageNumber()
        {
            Brush colorFgFocus = new SolidColorBrush(Colors.White);
            Brush colorFgUnFocus = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c7682"));
            Brush colorBgFocus = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00723a"));
            Brush colorBgUnFocus = new SolidColorBrush(Colors.Transparent);

            if (TotalPages <= 6)
            {
                pagingSeparetor.Visibility = Visibility.Collapsed;
                pagingOne.Foreground = colorFgUnFocus;
                pagingOne.Background = colorBgUnFocus;
                pagingTwo.Foreground = colorFgUnFocus;
                pagingTwo.Background = colorBgUnFocus;
                pagingThree.Foreground = colorFgUnFocus;
                pagingThree.Background = colorBgUnFocus;
                pagingFor.Foreground = colorFgUnFocus;
                pagingFor.Background = colorBgUnFocus;
                pagingFive.Foreground = colorFgUnFocus;
                pagingFive.Background = colorBgUnFocus;
                pagingSix.Foreground = colorFgUnFocus;
                pagingSix.Background = colorBgUnFocus;

                switch (Page)
                {
                    case 1:
                        pagingOne.Foreground = colorFgFocus;
                        pagingOne.Background = colorBgFocus;
                        break;
                    case 2:
                        pagingTwo.Foreground = colorFgFocus;
                        pagingTwo.Background = colorBgFocus;
                        break;
                    case 3:
                        pagingThree.Foreground = colorFgFocus;
                        pagingThree.Background = colorBgFocus;
                        break;
                    case 4:
                        pagingFor.Foreground = colorFgFocus;
                        pagingFor.Background = colorBgFocus;
                        break;
                    case 5:
                        pagingFive.Foreground = colorFgFocus;
                        pagingFive.Background = colorBgFocus;
                        break;
                    case 6:
                        pagingSix.Foreground = colorFgFocus;
                        pagingSix.Background = colorBgFocus;
                        break;
                    default:
                        break;
                }

                if (TotalPages == 4)
                {
                    pagingSeparetor.Visibility = Visibility.Collapsed;
                    pagingFive.Visibility = Visibility.Collapsed;
                    pagingSix.Visibility = Visibility.Collapsed;
                }
                else if (TotalPages == 5)
                {
                    pagingSeparetor.Visibility = Visibility.Collapsed;
                    pagingSix.Visibility = Visibility.Collapsed;
                }
            }

            if (TotalPages <= 3)
            {
                pagingSeparetor.Visibility = Visibility.Collapsed;
                pagingFor.Visibility = Visibility.Collapsed;
                pagingFive.Visibility = Visibility.Collapsed;
                pagingSix.Visibility = Visibility.Collapsed;
            }
            else if (TotalPages >= 6 && TotalPages < Page + 6)
            {
                pagingSeparetor.Visibility = Visibility.Visible;
                pagingOne.Content = colorFgUnFocus;
                pagingTwo.Content = colorBgUnFocus;
                pagingFor.Content = colorFgUnFocus;
                pagingFive.Content = colorFgUnFocus;
                pagingSix.Content = colorBgUnFocus;
            }

            string complement = TotalPages > 1 ? "Páginas" : "Página";
            txtPages.Text = $"{TotalPages} {complement}.";
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
        private void Refresh(object sender, EventArgs args)
        {
            GetRecents(Page);
        }

        #region Pagination       
        private void PageNext_Click(object sender, EventArgs args)
        {
            if ((Page + 1) <= TotalPages)
                GetRecents(Page + 1);
            else
                SystemSounds.Exclamation.Play();
        }
        private void PagePrevious_Click(object sender, EventArgs args)
        {
            if ((Page - 1) >= 1)
                GetRecents(Page - 1);
            else
                SystemSounds.Exclamation.Play();
        }
        private void Page_Click(object sender, EventArgs args)
        {
            Button btn = sender as Button;

            int page = int.Parse(btn.Content as string);

            if (page > TotalPages)
            {
                SystemSounds.Exclamation.Play();
                return;
            }

            GetRecents(page);
        }
        #endregion
        private void UpdateRecents_Click(object sender, RoutedEventArgs args)
                => GetRecents(Page);

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

        private void AddActivityRow(Activity[] acts)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                for (int i = 0; i < acts.Length; i++)
                {
                    Activity act = acts[i];
                    string[] names = act.Account.Name.Split(' ');

                    object obj = new
                    {
                        BgColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(act.Account.Color)),
                        Character = $"{names[0][0]}{names[1][0]}".ToUpperInvariant(),
                        IconKind = KindByActivityType(act.Type),
                        DisplayName = act.Account.Name,
                        ItemName = act.Item.Name,
                        ItemWebUrl = act.Item.WebUrl,
                        act.Item.Directory,
                        act.Date,
                        act.Account.Email
                    };

                    if (viwers.Items.Count < i + 1)
                    {
                        viwers.Items.Add(obj);
                    }
                    else
                    {
                        viwers.Items[i] = obj;
                    }
                }
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

        protected override void OnClosed(EventArgs e)
        {
            _refreshTime.Stop(); 
            base.OnClosed(e);
        }
    }
}
