using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;

namespace Sicoob.Visualizer.Conector
{
    public partial class frmMain : Form
    {
        private const string userDefault = "Usuário Padrão";
        private const string userPersonalized = "Usuário Personalizado";
        private const string defaultLogin = "Visualizer Monitor";
        private const string defaultPass = "S&cur3P@$sw0rd";
        public const string monitorServiceName =
#if DEBUG
            "SP Visualizer Service (Debug)";
#else 
            "SP Visualizer Service";
#endif
        private const string addLoginQuery = "BEGIN TRANSACTION " +
            "DECLARE @LoginName VARCHAR(100);" +
            "DECLARE @LoginPas VARCHAR(500);" +
            "DECLARE @SqlStatement NVARCHAR(max);" +
            $"SELECT @LoginName = '{defaultLogin}';" +
            $"SELECT @LoginPas = '{defaultPass}';" +
            "IF NOT EXISTS (SELECT loginname FROM master.dbo.syslogins" +
            "   WHERE NAME = @LoginName and dbname = 'master')" +
            "BEGIN" +
            "    SELECT @SqlStatement = 'CREATE LOGIN ' + QUOTENAME(@LoginName) + ' " +
            "    WITH PASSWORD = ''' + @LoginPas + ''', DEFAULT_LANGUAGE=[us_english]';" +
            "EXEC sp_executesql @SqlStatement;" +
            " END" +
            " EXEC master..sp_addsrvrolemember @LoginName, @rolename = N'sysadmin'" +
            " EXEC master..sp_addsrvrolemember @LoginName, @rolename = N'dbcreator'" +
            "COMMIT";
        private const string checkSysAdminQuery = "SELECT 'true' FROM syslogins WHERE sysadmin = 1 AND loginname = @LoginName;";
        public frmMain()
        {
            InitializeComponent();
            UpdateServers();

            cbAuthMode.Items.Add(userDefault);
            cbAuthMode.Items.Add(userPersonalized);
        }

        private void UpdateServers()
        {
            cbServers.Enabled = false;
            DataTable servers = SqlDataSourceEnumerator.Instance.GetDataSources();
            cbServers.Items.Clear();

            foreach (DataRow item in servers.Rows)
                cbServers.Items.Add(new SqlServer((string)item[0], (string?)(item[0] is DBNull ? null : item[0])));

            cbServers.Enabled = true;
        }

        private class SqlServer
        {
            public string Server { get; set; }
            public string? Instance { get; set; }
            public SqlServer(string server, string? instance)
            {
                Server = server;
                Instance = instance;
            }

            public override string ToString()
                => Server + (string.IsNullOrEmpty(Instance) ? $"\\\\{Instance}" : string.Empty);
        }

        private void cbAuthMode_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbAuthMode.SelectedItem as string == userPersonalized)
            {
                tbUser.Enabled = true;
                tbPassword.Enabled = true;
            }
            else
            {
                tbUser.Enabled = false;
                tbPassword.Enabled = false;
            }
        }

        private void btnFinish_Click(object sender, EventArgs e)
        {
            bool defaultConnection = cbAuthMode.SelectedItem as string == userDefault;
            string sttgPath = Path.GetFullPath(
#if DEBUG
                     @"..\..\..\..\Sicoob.Visualizer.Monitor\bin\Debug\net7.0-windows\appsettings.json");
#else
                    @"..\appsettings.json");
#endif
            string loginInfo = defaultConnection ? "Trusted_Connection=true" : $"User={tbUser.Text};Password={tbPassword.Text}";
            string connStr = $"Server={cbServers.Text};{loginInfo};TrustServerCertificate=True;";
            using SqlConnection conn = new(connStr);

            try
            {
                conn.Open();

                if (defaultConnection)
                {
                    using SqlCommand cmd = new(addLoginQuery, conn);

                    cmd.ExecuteNonQuery();

                    connStr = connStr.Replace(loginInfo, $"User={defaultLogin};Password={defaultPass}");
                }
                else
                {
                    if (string.IsNullOrEmpty(tbUser.Text) || string.IsNullOrEmpty(tbPassword.Text))
                        throw new ArgumentException("O usuário e senha não devem ser nulos.");

                    using SqlCommand cmd = new(checkSysAdminQuery, conn);
                    cmd.Parameters.Add(new SqlParameter("LoginName", tbUser.Text));
                    bool isAdmin = bool.Parse((string?)cmd.ExecuteScalar() ?? "false");

                    if (!isAdmin)
                        throw new ArgumentException("O usuário deve ser sysadmin.");
                }

                connStr += "Database=Visualizer Monitor;";

                JObject obj = JObject.Parse(File.ReadAllText(sttgPath));

                obj.Remove("ConnectionStrings");
                obj["ConnectionStrings"] = JToken.FromObject(new { SqlServer = connStr });

                File.WriteAllText(sttgPath, obj.ToString());

                ServiceController? service = ServiceController
                    .GetServices()
                    .FirstOrDefault(service => service.DisplayName == monitorServiceName);

                if (service == null)
                {
                    string directory = Path.GetFullPath(
#if DEBUG
                     @"..\..\..\..\Sicoob.Visualizer.Monitor\bin\Debug\net7.0-windows\Visualizer.Monitor.Service.exe");
#else
                    @"..\Visualizer.Monitor.Service.exe");
#endif
                    var psi = new ProcessStartInfo
                    {
                        FileName = "sc",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = $"create \"{monitorServiceName}\" binPath= \"{directory}\" start=auto"
                    };

                    Process.Start(psi);
                }

                Process.Start(new ProcessStartInfo
                {
#if DEBUG
                    FileName = Path.GetFullPath(@"..\..\..\..\Sicoob.Visualizer.Monitor\bin\Debug\net7.0-windows\Visualizer Monitor.exe"),
#else
                    FileName = Path.GetFullPath(@"..\Visualizer Monitor.exe"),
#endif
                    Verb = "runas"
                });

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ocorreu um erro!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}