using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class RegisterExtensionCommand : BaseCommand
    {
        public string ExtensionId { get; set; }
        public string PublicKey { get; set; }
        public string Hostname { get; set; }
        public string Os { get; set; }
        public string ExtensionVersion { get; set; }

        public RegisterExtensionCommand() {}
    }
}
