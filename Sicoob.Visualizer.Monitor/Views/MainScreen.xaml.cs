using MahApps.Metro.IconPacks;
using Microsoft.EntityFrameworkCore;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Comuns.Helpers;
using Sicoob.Visualizer.Monitor.Dal.Models;
using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System.Diagnostics;
using System.Media;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Activity = Sicoob.Visualizer.Monitor.Dal.Models.Activity;
using Timer = System.Timers.Timer;

namespace Sicoob.Visualizer.Monitor.Wpf.Views;
/// <summary>
/// Lógica interna para MainScreen.xaml
/// </summary>
public partial class MainScreen : Window
{
    private Timer _refreshTime;
    private Timer _updateLastCheck;
    public Settings AppSettings { get; set; }
    public Account Account { get; set; }
    public bool? DateAscending { get; set; } = null;
    public int TotalPages { get; set; }
    public bool Active { get; set; } = true;
    public DateTime LastCheck { get => Account.LastCheck; }
    public int Page { get => _page; set => _page = value; }

    private int _page;
    public string Search => Application.Current.Dispatcher.Invoke(() => txtSearch.Text);
    public GraphHelper Helper { get; set; }
    public MainScreen(Account account)
    {
        InitializeComponent();
        SetAccount(account);
        AppSettings = Settings.LoadSettings();
        Page = 1;
        txtVersion.Text = $"Versão: {SplashScreen.version}";

        Helper = new GraphHelper(AppSettings.OAuth, AppSettings.GetContext());
        #region Timers
        _refreshTime = new()
        {
            AutoReset = true,
            Interval = 500,
            Enabled = true
        };
        _refreshTime.Elapsed += Refresh;
        _refreshTime.Start();

        _updateLastCheck = new()
        {
            AutoReset = true,
            Interval = 30000,
            Enabled = true
        };
        _updateLastCheck.Elapsed += UpdateLastCheck;
        _updateLastCheck.Start();
        #endregion
        UpdateBell();
    }

    #region Events
    private void columnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is DataGridColumnHeader)
        {
            var header = e.OriginalSource as DataGridColumnHeader;
            if ((header.Content as string).Equals("Data", StringComparison.InvariantCultureIgnoreCase))
            {
                DateAscending = !(DateAscending ?? true);
            }
        }
    }
    private void Logout_Click(object sender, RoutedEventArgs args)
    {
        Helper.ClearLogin();
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
        Close();
    }
    private void Export_Click(object sender, EventArgs args)
    {
        ExportScreen export = new(Search);
        export.ShowDialog();
    }
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void Conffett_Click(object sender, EventArgs args)
    {
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void btnMaximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            btnMaximize.Kind = PackIconMaterialKind.Fullscreen;
        }
        else
        {
            WindowState = WindowState.Maximized;
            btnMaximize.Kind = PackIconMaterialKind.FullscreenExit;
        }
    }
    #endregion

    #region Pagination       
    private void PageNext_Click(object sender, EventArgs args)
    {
        if ((Page + 1) <= TotalPages)
        {
            Page++;

            GetRecents(ref _page, Search);
        }
        else
            SystemSounds.Exclamation.Play();
    }
    private void PagePrevious_Click(object sender, EventArgs args)
    {
        if ((Page - 1) >= 1)
        {
            Page--;
            GetRecents(ref _page, Search);
        }
        else
            SystemSounds.Exclamation.Play();
    }
    private void Page_Click(object sender, EventArgs args)
    {
        Button btn = sender as Button;

        int page;
        if (btn.Content is int)
            page = (int)btn.Content;
        else if (btn.Content is string)
            page = int.Parse(btn.Content as string);
        else
            page = int.Parse(btn.Content.ToString());

        if (page > TotalPages)
        {
            SystemSounds.Exclamation.Play();
            return;
        }

        Page = page;
        GetRecents(ref _page, Search);
    }
    #endregion

    #region Recents
    private void GetRecents(ref int page, string? search = null)
    {
        DateTime?
            start = null,
            end = null;

        string? userName, userEmail, fileName;

        (userName, userEmail, fileName) = search.GetSearch();

        var activities = Helper.GetActivities(ref page, out int pages, AppSettings.PerPage, DateAscending, fileName, userName, userEmail, start, end);

        if (activities.Where(wh => wh.Date > LastCheck).Count() > 0)
        {

        }

        AddActivityRow(activities);

        TotalPages = pages;

        Application.Current.Dispatcher.Invoke(()
            => HiddenByPageNumber());
    }
    private void Refresh(object sender, EventArgs args)
    {
        GetRecents(ref _page, Search);
        ServiceController? service = ServiceController
                                           .GetServices()
                                           .FirstOrDefault(service => service.DisplayName == SplashScreen.monitorServiceName);

        Application.Current.Dispatcher.BeginInvoke(() =>
            txtServiceStatus.Text = $"Service: {Enum.GetName(service?.Status ?? ServiceControllerStatus.Stopped)}");
    }

    private async void UpdateLastCheck(object sender, EventArgs args)
    {
        if (Active)
        {
            Account.LastCheck = DateTime.Now;
            var settings = Settings.LoadSettings();

            using (var ctx = settings.GetContext())
            {
                ctx.Entry(Account).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
            }
        }
    }

    private void AddActivityRow(Activity[] acts)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (viwers.Items.Count > acts.Length)
                viwers.Items.Clear();

            for (int i = 0; i < acts.Length; i++)
            {
                Activity act = acts[i];
                string[] names = act.Account.Name.Split(' ');
                string Directory = act.Item.Folder?.Directory ?? act.Item.List?.Directory;
                Directory ??= string.Empty;
                object obj = new
                {
                    BgColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(act.Account.Color)),
                    Character = $"{names[0][0]}{names[1][0]}".ToUpperInvariant(),
                    IconKind = KindByActivityType(act.Type),
                    DisplayName = act.Account.Name,
                    ItemName = act.Item.Name,
                    ItemWebUrl = act.Item.WebUrl,
                    Directory,
                    act.Date,
                    act.Account.Email,
                    IconNew = (act.Inserted > LastCheck) ? Visibility.Visible : Visibility.Hidden
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
    #endregion

    private void HiddenByPageNumber()
    {
        Brush colorFgFocus = new SolidColorBrush(Colors.White);
        Brush colorFgUnFocus = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c7682"));
        Brush colorBgFocus = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00723a"));
        Brush colorBgUnFocus = new SolidColorBrush(Colors.Transparent);

        try
        {
            pagingOne.Click -= Page_Click;
            pagingTwo.Click -= Page_Click;
            pagingThree.Click -= Page_Click;
        }
        catch (Exception)
        {
        }

        pagingThree.Cursor = Cursors.Hand;
        pagingThree.Click += Page_Click;
        pagingTwo.Cursor = Cursors.Hand;
        pagingTwo.Click += Page_Click;
        pagingOne.Cursor = Cursors.Hand;
        pagingOne.Click += Page_Click;

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

            if (TotalPages < 3)
            {
                pagingThree.Cursor = Cursors.No;
                pagingThree.Click -= Page_Click;
            }

            if (TotalPages < 2)
            {
                pagingTwo.Cursor = Cursors.No;
                pagingTwo.Click -= Page_Click;
            }

            if (TotalPages < 1)
            {
                pagingOne.Cursor = Cursors.No;
                pagingOne.Click -= Page_Click;
            }
        }
        else if (TotalPages > Page + 12)
        {
            pagingOne.Content = Page;
            pagingTwo.Content = Page + 1;
            pagingThree.Content = Page + 2;
            pagingFor.Content = Page + 10;
            pagingFive.Content = Page + 11;
            pagingSix.Content = Page + 12;
        }
        else
        {

        }

        string complement = TotalPages != 1 ? "Páginas" : "Página";
        txtPages.Text = $"{TotalPages} {complement}.";
    }
    private void SetAccount(Account account)
    {
        Account = account;
        string[] names = account.Name.Trim().Split(' ');

        userEmail.Text = account.Email;
        userName.Text = account.Name;
        userInitial.Text = names[0][0].ToString().ToUpperInvariant();
        userColor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(account.Color));
    }
    private static PackIconMaterialKind KindByActivityType(ActivityType type)
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