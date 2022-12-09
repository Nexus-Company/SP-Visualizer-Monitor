using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sicoob.Visualizer.Monitor.Dal.Models;
public class GraphAuthentication
{
    [Key]
    public int Id { get; set; }
    [StringLength(100)]
    public string TokenType { get; set; }
    [Required]
    [StringLength(2500)]
    public string AccessToken { get; set; }
    [StringLength(2500)]
    public string RefreshToken { get; set; }
    public string Account { get; set; }
    public int ExpiresIn { get; set; }

    [ForeignKey(nameof(Account))]
    public Account AccountNavigation { get; set; }
}
