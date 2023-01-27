using System.ComponentModel;

namespace Sicoob.Visualizer.Monitor.Dal.Models;
public class Site
{
    [Key, Required]
    [StringLength(449)]
    public string Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    [DefaultValue("https://y2c0h.sharepoint.com/")]
    public string WebUrl { get; set; }
}