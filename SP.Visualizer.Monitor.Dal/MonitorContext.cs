global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
using SP.Visualizer.Monitor.Dal.Models;

namespace SP.Visualizer.Monitor.Dal;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Type_or_Member'
public partial class MonitorContext : DbContext
{
    #region DBSets
    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<GraphAuthentication> Authentications { get; set; }
    public virtual DbSet<Activity> Activities { get; set; }
    public virtual DbSet<Item> Items { get; set; }
    public virtual DbSet<Site> Sites { get; set; }
    public virtual DbSet<List> Lists { get; set; }
    public virtual DbSet<Folder> Folders { get; set; }
    #endregion

    /// <summary>
    /// Database SqlServer Connection String
    /// </summary>
    private string ConnectionString { get; set; }
    private static string? _lastConnection;

    public MonitorContext()
    {
        ConnectionString = _lastConnection ??
#if DEBUG
            "Server=PC;User=MWS;Password=dev;TrustServerCertificate=True;Database=Visualizer Monitor;";
#else   
            "Server=PC;Database=Nexus OAuth;User Id=MWS;Password=dev;";
#endif
    }
    public MonitorContext(string conn)
    {
        ConnectionString = conn;
        _lastConnection = conn;
    }
    public MonitorContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(ConnectionString, (opt) => { opt.EnableRetryOnFailure(); });
    }

    public override void Dispose()
    {
        base.Dispose();

        GC.SuppressFinalize(this);
    }
}