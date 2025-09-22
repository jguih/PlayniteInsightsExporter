using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class KeyManager
    {
        private const int KeySize = 2048;
        private readonly string keyFilePath;

        public KeyManager(IPlayniteInsightsExporterContext plugin)
        {
            var securityDir = Path.Combine(plugin.GetExtensionDataFolderPath(), "security");

            if (!Directory.Exists(securityDir))
            {
                Directory.CreateDirectory(securityDir);
            }

            keyFilePath = Path.Combine(securityDir, "keys.xml");
        }

        public RSACryptoServiceProvider GetOrCreateKeyPair()
        {
            if (File.Exists(keyFilePath))
            {
                var xml = File.ReadAllText(keyFilePath);
                var rsa = new RSACryptoServiceProvider(KeySize);
                rsa.FromXmlString(xml);
                return rsa;
            }
            else
            {
                var rsa = new RSACryptoServiceProvider(KeySize);
                var xml = rsa.ToXmlString(true);
                File.WriteAllText(keyFilePath, xml);
                return rsa;
            }
        }
    }
}
