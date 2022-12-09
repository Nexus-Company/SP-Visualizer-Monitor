﻿global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
using Sicoob.Visualizer.Monitor.Dal.Models;

namespace Sicoob.Visualizer.Monitor.Dal;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Type_or_Member'
public partial class MonitorContext : DbContext
{
    #region DBSets
    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<GraphAuthentication> Authentications { get; set; }
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
            "Server=PC;Database=Nexus OAuth (Development);Trusted_Connection=true;";
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
            optionsBuilder.UseSqlServer(ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }

    public override void Dispose()
    {
        base.Dispose();

        GC.SuppressFinalize(this);
    }
}