namespace Sicoob.Visualizer.Monitor.Dal.Models;
public class Account
{
    [Key]
    [StringLength(320)]
    public string Email { get; set; }
    [StringLength(320)]
    public string Name { get; set; }

    [StringLength(12)]
    public string Color { get; set; }
}
