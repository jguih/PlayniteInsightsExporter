using Core;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class KeyManager : IKeyManager
    {
        private const int KeySize = 2048;
        private readonly string keyFilePath;

        public KeyManager(IPlayAtlasExporterContext plugin)
        {
            var securityDir = plugin.GetSecurityDirectoryPath();

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

        public string GetPublicKeyAsPem()
        {
            var rsa = GetOrCreateKeyPair();
            var rsaParams = rsa.ExportParameters(false);

            RsaKeyParameters bcKey = new RsaKeyParameters(
                false, // public key only
                new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Modulus),
                new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Exponent)
            );

            using (var sw = new StringWriter())
            {
                var pemWriter = new PemWriter(sw);
                pemWriter.WriteObject(bcKey);
                pemWriter.Writer.Flush();
                return sw.ToString();
            }
        }
    }
}
