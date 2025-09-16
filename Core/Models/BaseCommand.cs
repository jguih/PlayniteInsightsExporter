using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class BaseCommand
    {
        public DateTime ClientUtcNow { get; set; } = DateTime.UtcNow;

        public BaseCommand() { }
    }
}
