namespace Sicoob.Visualizer.Monitor.Dal.Models;

[Index(nameof(Name), nameof(ListId))]
public class Folder
{
    [Key, Required]
    public string Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    [StringLength(449)]
    public string ListId { get; set; }

    [Required]
    [StringLength(int.MaxValue)]
    public string Directory
    {
        get => directory;
        set => directory = RemoveSeparetors(value);
    }

    private string directory;

    public string? FatherId { get; set; }

    [ForeignKey(nameof(ListId))]
    public List List { get; set; }

    [ForeignKey(nameof(FatherId))]
    public Folder Father { get; set; }


    public static string RemoveSeparetors(string directory)
    {
        directory = Uri.UnescapeDataString(directory);
        directory = directory.Replace("/", @"\");
        directory = directory.EndsWith(@"\") ? directory.Remove(directory.Length - 1, 1) : directory;
        return directory.StartsWith(@"\") ? directory.Remove(0, 1) : directory;
    }
}