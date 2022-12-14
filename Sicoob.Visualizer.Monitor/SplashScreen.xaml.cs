using MaterialDesignThemes.Wpf;
using Sicoob.Visualizer.Monitor.Comuns;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace Sicoob.Visualizer.Monitor.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        public const string monitorServiceName = "SP Visualizer Worker";
        private readonly PaletteHelper paletteHelper = new();
        private readonly Task thAwaitLogin;
        public Settings AppSettings { get; set; }
        public GraphHelper Helper { get; set; }
        public SplashScreen()
        {
            InitializeComponent();
            AppSettings = Settings.LoadSettings();
            thAwaitLogin = new(Login);
            Helper = new GraphHelper(AppSettings.OAuth, AppSettings.GetContext(), true);

            ITheme theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(Theme.Dark);
        }
        private async void Login()
        {
            bool logued =
#if DEBUG
                false;
#else 
                false;
#endif

            try
            {
                await Helper.GetLoginAsync();
                logued = true;
            }
            catch (Exception)
            {

            }

            if (!logued)
            {
                await ChangeStatusAsync("Esperando autorização...");

                await Helper.SaveLoginAsync();

                await Application.Current.Dispatcher
                    .BeginInvoke(DispatcherPriority.Background, () =>
                    {
                        txtHpReOpen.Visibility = Visibility.Visible;
                    });
            }

            await Application.Current.Dispatcher
               .BeginInvoke(DispatcherPriority.Background, () =>
               {
                   txtHpReOpen.Visibility = Visibility.Hidden;
               });

            await ChangeStatusAsync("Obtendo informações...");

            var account = await Helper.GetAuthenticatedAccountAsync();

            var drive = (await Helper.GetDrivesAsync()).First();

            //if (!logued)
            //{
            //    await ChangeStatusAsync("Esperando autorização do Sharepoint...");

            //    await Helper.SaveLoginAsync(new Uri(drive.WebUrl).Host);
            //}

            await ChangeStatusAsync("Inciando o serviço...");

            ServiceController? service = ServiceController
               .GetServices()
               .FirstOrDefault(service => service.DisplayName == monitorServiceName);

            if (service == null)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = $"create \"{monitorServiceName}\" binPath= \"{Path.GetFullPath(@"Sicoob.Visualizer.Monitor.Worker.exe")}\" start=auto"
                };

                Process.Start(psi);

                Thread.Sleep(100);

                service = ServiceController
                    .GetServices()
                    .FirstOrDefault(service => service.DisplayName == monitorServiceName);
            }

            if (service?.Status == ServiceControllerStatus.StopPending)
            {
                await ChangeStatusAsync("Esperando resposta do serviço...");
                service?.WaitForStatus(ServiceControllerStatus.Stopped);
                await ChangeStatusAsync("Inciando o serviço...");
            }

            if (service?.Status != ServiceControllerStatus.Running)
            {
                service?.Start();
                service?.WaitForStatus(ServiceControllerStatus.Running);
            }

            service?.Dispose();

            await Application.Current.Dispatcher
                .BeginInvoke(DispatcherPriority.Background, () =>
                {
                    MainScreen ms = new(account);
                    Close();
                    ms.Show();
                });
        }

        private async Task ChangeStatusAsync(string text)
            => await Application.Current.Dispatcher
                .BeginInvoke(DispatcherPriority.Background, () =>
                {
                    txtStatus.Text = text;
                });
        private void Window_ContentRendered(object sender, EventArgs e)
           => thAwaitLogin.Start();
        private void hpReOpen_RequestNavigate(object sender, RequestNavigateEventArgs e)
           => Helper.RequestLogin();
        protected override void OnClosed(EventArgs e)
        {
            Helper.Dispose();
        }
    }
}