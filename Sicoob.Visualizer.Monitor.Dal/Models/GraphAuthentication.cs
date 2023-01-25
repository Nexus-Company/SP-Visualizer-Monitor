using Sicoob.Visualizer.Monitor.Dal.Models.Enums;

namespace Sicoob.Visualizer.Monitor.Dal.Models;
public class GraphAuthentication
{
    [Key]
    public int Id { get; set; }
    [StringLength(100)]
    public string TokenType { get; set; }
    [Required]
    [StringLength(int.MaxValue)]
    public string AccessToken { get; set; }
    [StringLength(int.MaxValue)]
    public string RefreshToken { get; set; }
    public string Account { get; set; }
    public DateTime RefreshIn { get; set; }
    public AuthenticationType Type { get; set; }

    [ForeignKey(nameof(Account))]
    public Account AccountNavigation { get; set; }
}
