using ExporterCommon.Application;
using ExporterCommon.Infra;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Security.Cryptography;

namespace ExporterSystem.Infra
{
    public class KeyManager : IKeyManagerPort
    {
        private const int KeySize = 2048;
        private readonly ISystemConfigPort systemConfig;
        private readonly IFileSystemServicePort fileSystemService;
        private readonly IAppLoggerPort appLogger;

        public KeyManager(
            ISystemConfigPort systemConfig,
            IFileSystemServicePort fileSystemService,
            IAppLoggerPort appLogger
        )
        {
            this.systemConfig = systemConfig;
            this.fileSystemService = fileSystemService;
            this.appLogger = appLogger;
        }

        private string GetKeyFilePath()
        {
            return fileSystemService.PathCombine(systemConfig.SecurityDirPath, "keys.xml");
        }

        private RSACryptoServiceProvider GetKey()
        {
            string keyFilePath = GetKeyFilePath();
            var xml = fileSystemService.FileReadAllText(keyFilePath);
            var rsa = new RSACryptoServiceProvider(KeySize);
            rsa.FromXmlString(xml);
            return rsa;
        }

        public string GetPublicKeyAsPem()
        {
            using (var rsa = GetKey())
            {
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

        public byte[] Sign(byte[] data)
        {
            using (var rsa = GetKey())
            {
                byte[] signature = rsa.SignData(
                    data,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );
                return signature;
            }
        }

        public void WriteAsymmetricKeyPair()
        {
            string keyFilePath = GetKeyFilePath();
            using (var rsa = new RSACryptoServiceProvider(KeySize))
            {
                var xml = rsa.ToXmlString(true);
                fileSystemService.FileWriteAllText(keyFilePath, xml);
                appLogger.Info($"New assymetric key pair written to {keyFilePath}");
            }
        }

        public bool KeyExistsAndIsValid()
        {
            string keyFilePath = GetKeyFilePath();

            if (!fileSystemService.FileExists(keyFilePath))
                return false;

            try
            {
                using (var rsa = GetKey())
                {
                    var parameters = rsa.ExportParameters(false);
                    var valid = parameters.Modulus != null && parameters.Modulus.Length > 0;
                    if (valid)
                    {
                        appLogger.Info("Assymetric key pair is valid");
                        return true;
                    }
                    appLogger.Warn("Invalid assymetric key pair detected");
                    return false;
                }
            }
            catch (Exception ex)
            {
                appLogger.Error("Error while verything assymetric keys validity", ex);
                return false;
            }
        }

    }
}
