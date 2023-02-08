namespace SP.Visualizer.Monitor.Dal.Models;

[Index(nameof(Id), nameof(Name), nameof(FolderId), nameof(ListId))]
public class Item
{
    [Key]
    [StringLength(320)]
    public string Id { get; set; }

    [Required]
    [StringLength(int.MaxValue)]
    public string WebUrl { get; set; }

    [Required]
    [StringLength(449)]
    public string Etag { get; set; }

    [Required]
    [StringLength(int.MaxValue)]
    public string MimeType { get; set; }

    [Required]
    [StringLength(449)]
    public string Name { get; set; }

    [StringLength(450)]
    public string? FolderId { get; set; }

    [StringLength(449)]
    public string? ListId { get; set; }

    [ForeignKey(nameof(FolderId))]
    public Folder? Folder { get; set; }

    [ForeignKey(nameof(ListId))]
    public List? List { get; set; }
}