using Sicoob.Visualizer.Monitor.Dal.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sicoob.Visualizer.Monitor.Dal.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        [Required]
        public string User { get; set; }
        [Required]
        public string Target { get; set; }
        [Required]
        public ActivityType Type { get; set; }

        [ForeignKey(nameof(User))]
        public Account Account { get; set; }

        [ForeignKey(nameof(Target))]
        public Item Item { get; set; }
    }
}
