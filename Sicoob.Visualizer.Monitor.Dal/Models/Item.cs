using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}