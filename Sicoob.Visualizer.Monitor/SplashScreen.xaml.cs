using MaterialDesignThemes.Wpf;
using Microsoft.Data.SqlClient;
using Microsoft.Toolkit.Uwp.Notifications;
using Sicoob.Visualizer.Monitor.Comuns;
using Sicoob.Visualizer.Monitor.Comuns.Helpers;
using Sicoob.Visualizer.Monitor.Wpf.Views;
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
        public const string version = "0.1.3";
        public const string monitorServiceName =
#if DEBUG
     "SP Visualizer Service (Debug)";
#else 
            "SP Visualizer Service";
#endif
        private readonly PaletteHelper paletteHelper = new();
        private readonly Task thAwaitLogin;
        public Settings AppSettings { get; set; }
        public GraphHelper Helper { get; set; }
        public SplashScreen()
        {
            InitializeComponent();

            txtVersion.Text = $"Versão: {version}";
            try
            {
                AppSettings = Settings.LoadSettings();
                thAwaitLogin = new(Login);
                Helper = new GraphHelper(AppSettings.OAuth, AppSettings.GetContext(), true);
            }
            catch (SqlException ex)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(
#if DEBUG
                        @"..\..\..\..\Sicoob.Visualizer.Conector\bin\Debug\net7.0-windows\Visualizer Conector.exe"
#else
                        @"Conector\Visualizer Conector.exe"
#endif
),
                    Verb = "runas"
                });

                Close();
            }
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

            try
            {
                ServiceController? service = ServiceController
                                                    .GetServices()
                                                    .FirstOrDefault(service => service.DisplayName == monitorServiceName);

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
            }
            catch (Exception)
            {
                new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 1234)
                        .AddText("Serviço Monitoramento")
                        .AddText("O Serviço de Monitoramenteo do Sharepoint foi não iniciado por causa de um erro.");
            }

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
            Helper?.Dispose();
        }
    }
}