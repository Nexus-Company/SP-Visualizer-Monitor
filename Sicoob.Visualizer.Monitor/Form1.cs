using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using System;
using Form = System.Windows.Forms.Form;

namespace Sicoob.Visualizer.Monitor
{
    public partial class Form1 : Form
    {
        public ClientContext ClientContext { get; set; }
        public Form1()
        {
            InitializeComponent();
            var authManager = new AuthenticationManager();
            ClientContext = authManager.GetWebLoginClientContext("https://edgeufalbr.sharepoint.com/sites/TodoMundo");
            var site = ClientContext.Web;
            var file = ClientContext.Web.GetFileById(Guid.Parse("38e62963-94ba-4bb3-8a6a-6704a8cd8e64"));
            ClientContext.Load(file); 
            ClientContext.Load(site);
            ClientContext.Load(file.CheckedOutByUser);
            ClientContext.Load(file.Author);
            ClientContext.Load(file.ModifiedBy);
            ClientContext.Load(file.Properties);
            ClientContext.Load(file.ListItemAllFields);
            ClientContext.Load(file.LockedByUser);
            ClientContext.Load(file.VersionEvents);
            ClientContext.ExecuteQuery();
        }
    }
}
