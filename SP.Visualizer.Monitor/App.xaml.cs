using System.ServiceProcess;
using System.Windows;

namespace SP.Visualizer.Monitor.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
#if DEBUG
            using ServiceController? service = ServiceController
             .GetServices()
             .FirstOrDefault(service => service.DisplayName == SplashScreen.monitorServiceName);

            if (service?.Status == ServiceControllerStatus.Running)
            {
                service?.Stop();
            }
#endif
        }
    }
}
