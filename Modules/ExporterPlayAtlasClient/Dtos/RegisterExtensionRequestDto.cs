using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Dtos
{
    public class RegisterExtensionRequestDto : BaseRequestDto
    {
        public static readonly string ENDPOINT = "/api/extension/register";
        public string ExtensionId { get; }
        public string PublicKey { get; }
        public string Hostname { get; }
        public string Os { get; }
        public string ExtensionVersion { get; }

        public RegisterExtensionRequestDto(
            string extensionId, 
            string publicKey, 
            string hostname, 
            string os, 
            string extensionVersion
        )
        {
            ExtensionId = extensionId;
            PublicKey = publicKey;
            Hostname = hostname;
            Os = os;
            ExtensionVersion = extensionVersion;
        }
    }
}
