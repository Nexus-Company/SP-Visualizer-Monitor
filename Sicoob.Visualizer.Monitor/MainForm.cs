using Sicoob.Visualizer.Monitor.Comuns;
using System;
using System.Windows.Forms;

namespace Sicoob.Visualizer.Monitor
{
    public partial class MainForm : Form
    {
        public Settings AppSettings { get; set; }
        public MainForm()
        {
            InitializeComponent();
            AppSettings = Settings.LoadSettings(); 
            GraphHelper.InitializeGraphForUserAuthAsync(AppSettings);
        }

        private async void btnNext_Click(object sender, EventArgs e)
        {
            await GraphHelper.SaveLoginAsync();
        }
    }
}
