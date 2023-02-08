using SP.Visualizer.Monitor.Comuns;
using SP.Visualizer.Monitor.Comuns.Helpers;
using SP.Visualizer.Monitor.Wpf.ViewModels;
using System.Diagnostics;
using System.Windows;

namespace SP.Visualizer.Monitor.Wpf.Views;

/// <summary>
/// Lógica interna para ExportScreen.xaml
/// </summary>
public partial class ExportScreen : Window
{
    public Settings AppSettings { get; set; }
    public GraphHelper Helper { get; set; }
    public string? Search { get; set; }
    public ExportHelper Export { get; set; }
    public ExportScreen(string? search)
    {
        InitializeComponent();
        AppSettings = Settings.LoadSettings();
        Helper = new GraphHelper(AppSettings.OAuth, AppSettings.GetContext());
        Export = new ExportHelper(Helper);
        Search = search;
    }

    private async void Export_Click(object sender, EventArgs args)
    {
        //if (dtStart.SelectedDate == null && dtEnd.SelectedDate == null)
        //{
        //    ToolTip toolTip = (ToolTip)dtStart.ToolTip;
        //    toolTip.IsOpen = true;
        //    toolTip.Content = "O campo não pode ser nulo";
        //    return;
        //}

        try
        {
            btnExport.Click -= Export_Click;
            string export = await Export.ExportActivitiesAsync(Search,
            dtStart.SelectedDate ?? null,
            dtEnd.SelectedDate ?? DateTime.Now,
            rdOrdAscending.IsChecked ?? false,
            ((ExportTypeViewModel)gdTypes.DataContext).Checked);

            Process.Start(new ProcessStartInfo(export)
            {
                UseShellExecute = true,
            });

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ocorreu um error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnExport.Click += Export_Click;
        }
    }
}