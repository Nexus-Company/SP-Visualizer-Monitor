using Sicoob.Visualizer.Monitor.Comuns.Database.Models;
using System.Data.Entity;

namespace Sicoob.Visualizer.Monitor.Comuns.Database
{
    public class MonitorContext : DbContext
    {
        // Your context has been configured to use a 'MonitorContext' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'Sicoob.Visualizer.Monitor.Dal.MonitorContext' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'MonitorContext' 
        // connection string in the application configuration file.
        public MonitorContext()
        {
        }

        public MonitorContext(string conn)
            : base(conn)
        {
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        public virtual DbSet<GraphAuthentication> Authentications { get; set; }
    }
}