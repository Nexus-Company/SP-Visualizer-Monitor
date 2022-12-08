using MaterialDesignThemes.Wpf;
using Sicoob.Visualizer.Monitor.Comuns;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace Sicoob.Visualizer.Monitor.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string monitorServiceName = "SP Visualizer Monitor";
        private readonly PaletteHelper paletteHelper = new();
        private readonly Thread thAwaitLogin;


        public Settings AppSettings { get; set; }



        public MainWindow()
        {
            InitializeComponent();
            AppSettings = Settings.LoadSettings();
            thAwaitLogin = new(Login);
            GraphHelper.InitializeGraphForUserAuthAsync(AppSettings);

            ITheme theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(Theme.Dark);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
            => thAwaitLogin.Start();

        private async void Login()
        {
            //Declarando a classe ServiceController e preenchendo um array com todos os serviços
            //do windows usando o método GetServices()
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
                    Arguments = $"create \"{monitorServiceName}\" binPath= \"{Path.GetFullPath(@"Visualizer Monitor.exe")}\" start=auto"
                };

                Process.Start(psi);

                service = ServiceController
                    .GetServices()
                    .FirstOrDefault(service => service.DisplayName == monitorServiceName);
            }

            await GraphHelper.SaveLoginAsync();

            await Application.Current.Dispatcher
                .BeginInvoke(DispatcherPriority.Background, () => Close());
        }
    }


}

