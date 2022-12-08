using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sicoob.Visualizer.Monitor.Comuns.Database.Models
{
    public class GraphAuthentication
    {
        [StringLength(100)]
        public string TokenType { get; set; }
        [Key]
        [StringLength(2500)]
        public string AccessToken { get; set; }
        [StringLength(2500)]
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }
}
