using System.ComponentModel.DataAnnotations;

namespace Sicoob.Visualizer.Monitor.Comuns.Database.Models
{
    public class GraphAuthentication
    {
        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string TokenType { get; set; }
        [StringLength(2500)]
        public string AccessToken { get; set; }
        [StringLength(2500)]
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }
}
