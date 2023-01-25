namespace Sicoob.Visualizer.Monitor.Dal.Models;

public class Item
{
    [Key]
    [StringLength(320)]
    public string Id { get; set; }
    [Required]
    [StringLength(int.MaxValue)]
    public string WebUrl { get; set; }
    [Required]
    [StringLength(int.MaxValue)]
    public string Name { get; set; }

    [Required]
    [StringLength(int.MaxValue)]
    public string Directory
    {
        get => directory;
        set => directory = value.EndsWith("/") ? value.Remove(value.Length - 1, 1) : value;
    }

    private string directory;
}