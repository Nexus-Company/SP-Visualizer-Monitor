using Microsoft.EntityFrameworkCore;

namespace SP.Visualizer.Monitor.Dal.Models;

[Index(nameof(Id))]
public class List
{
    [Key, Required]
    [StringLength(449)]
    public string Id { get; set; }

    [Required]
    [StringLength(449)]
    public string SiteId { get; set; }

    [Required]
    public string Name { get; set; }

    public string? DriveId { get; set; }

    public string WebUrl { get; set; }

    public string Directory { get; set; }

    [ForeignKey(nameof(SiteId))]
    public Site Site { get; set; }
}